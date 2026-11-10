using System.Text.Json;
using ImportOps.Application.Common;
using ImportOps.Application.Finance;
using ImportOps.Application.Operations;
using ImportOps.Domain.Entities;
using ImportOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportOps.Infrastructure.Services;

public sealed class FinanceService(
    ImportOpsDbContext db,
    IImportOperationService operationService,
    ICurrentCompany currentCompany,
    ICurrentUser currentUser) : IFinanceService
{
    public async Task<FinancialSummaryDto> GetSummaryAsync(long operationId,CancellationToken ct)
    {
        var op=await GetOperationAsync(operationId,ct);
        var funds=await db.ClientFunds.AsNoTracking().Where(x=>x.OperationId==operationId&&!x.IsDeleted&&x.FundStatus!="CANCELLED").SumAsync(x=>(decimal?)x.BaseAmount,ct)??0;
        var expenses=await db.OperationExpenses.AsNoTracking().Where(x=>x.OperationId==operationId&&!x.IsDeleted&&x.ExpenseStatus!="CANCELLED").SumAsync(x=>(decimal?)x.BaseAmount,ct)??0;
        var payments=await (from p in db.ExpensePayments.AsNoTracking() join e in db.OperationExpenses.AsNoTracking() on p.OperationExpenseId equals e.OperationExpenseId where e.OperationId==operationId&&!p.IsVoid select p.BaseAmount).SumAsync(ct);
        var services=await db.OperationServices.AsNoTracking().Where(x=>x.OperationId==operationId&&x.ServiceStatus!="CANCELLED").SumAsync(x=>(decimal?)x.BaseAmount,ct)??0;
        var collections=await db.ClientCollections.AsNoTracking().Where(x=>x.OperationId==operationId&&!x.IsVoid&&x.CollectionStatus!="VOID").SumAsync(x=>(decimal?)x.BaseAmount,ct)??0;
        var refunds=await db.ClientRefunds.AsNoTracking().Where(x=>x.OperationId==operationId&&x.RefundStatus=="COMPLETED").SumAsync(x=>(decimal?)x.BaseAmount,ct)??0;
        var adjustments=await db.OperationAdjustments.AsNoTracking().Where(x=>x.OperationId==operationId&&!x.IsVoid).SumAsync(x=>(decimal?)(x.AdjustmentType=="CREDIT"?-x.BaseAmount:x.BaseAmount),ct)??0;
        var serviceCollected=await (from a in db.ServiceCollectionAllocations.AsNoTracking() join s in db.OperationServices.AsNoTracking() on a.OperationServiceId equals s.OperationServiceId where s.OperationId==operationId select a.AllocatedAmount).SumAsync(ct);
        var customsApplied=await db.CustomsTaxFinancePayments.AsNoTracking().Where(x=>x.CompanyId==currentCompany.CompanyId&&x.OperationId==operationId&&!x.IsVoid&&x.FundingSource=="CLIENT_ADVANCE").SumAsync(x=>(decimal?)x.AmountPen,ct)??0;
        var managedFundBalance=funds-payments-refunds-customsApplied;
        var pendingService=Math.Max(0,services-serviceCollected);
        var clientBalance=funds+collections-expenses-services-refunds+adjustments;
        return new(operationId,op.FinancialStatus,funds,expenses,payments,services,collections,refunds,managedFundBalance,pendingService,clientBalance);
    }

    public async Task<BudgetDto?> GetBudgetAsync(long operationId,CancellationToken ct)
    {
        await GetOperationAsync(operationId,ct);
        var item=await db.OperationBudgets.AsNoTracking()
            .Where(x=>x.OperationId==operationId&&x.BudgetStatus!="CANCELLED")
            .OrderByDescending(x=>x.VersionNo)
            .FirstOrDefaultAsync(ct);
        return item is null?null:MapBudget(item);
    }

    public async Task<SettlementDto?> GetSettlementAsync(long operationId,CancellationToken ct)
    {
        await GetOperationAsync(operationId,ct);
        var item=await db.OperationSettlements.AsNoTracking()
            .SingleOrDefaultAsync(x=>x.OperationId==operationId&&x.CompanyId==currentCompany.CompanyId,ct);
        return item is null?null:MapSettlement(item);
    }

    public async Task<BudgetDto> EnsureBudgetFromQuotationAsync(long operationId,CancellationToken ct)
    {
        var op=await GetOperationAsync(operationId,ct);
        var existing=await db.OperationBudgets.SingleOrDefaultAsync(x=>x.OperationId==operationId&&x.BudgetStatus!="CANCELLED",ct);
        if(existing is not null)return MapBudget(existing);

        var quotation=await db.Quotations.AsNoTracking().SingleAsync(x=>x.QuotationId==op.QuotationId,ct);
        var items=await db.QuotationItems.AsNoTracking().Where(x=>x.QuotationId==quotation.QuotationId).OrderBy(x=>x.LineNo).ToListAsync(ct);

        await using var tx=await db.Database.BeginTransactionAsync(ct);
        var budget=new OperationBudget{CompanyId=currentCompany.CompanyId,OperationId=operationId,VersionNo=1,BudgetStatus="APPROVED",BaseCurrencyCode=quotation.CurrencyCode,
            CreatedAt=DateTime.UtcNow,CreatedBy=currentUser.UserId,ApprovedAt=DateTime.UtcNow,ApprovedByUserId=currentUser.UserId,Notes=$"Generado desde {quotation.QuotationNo} V{quotation.VersionNo}."};
        db.OperationBudgets.Add(budget);await db.SaveChangesAsync(ct);

        foreach(var q in items){
            var type=q.ItemType=="SERVICE"?"SERVICE":"COST";
            db.OperationBudgetItems.Add(new OperationBudgetItem{OperationBudgetId=budget.OperationBudgetId,ItemType=type,ExpenseCategoryId=q.ExpenseCategoryId,
                ServiceCategoryId=q.ServiceCategoryId,Description=q.Description,CurrencyCode=quotation.CurrencyCode,EstimatedAmount=q.LineTotal,EstimatedBaseAmount=q.LineTotal,IsRebillable=q.IsRebillable,Notes=q.Notes});
        }
        budget.TotalEstimatedCost=items.Where(x=>x.ItemType!="SERVICE").Sum(x=>x.LineTotal);
        budget.TotalEstimatedService=items.Where(x=>x.ItemType=="SERVICE").Sum(x=>x.LineTotal);
        budget.TotalEstimatedAmount=budget.TotalEstimatedCost+budget.TotalEstimatedService;
        AddEvent(operationId,"FINANCE_BUDGET_CREATED","Presupuesto inicial creado",$"Generado desde cotización aprobada por {budget.TotalEstimatedAmount:N2} {budget.BaseCurrencyCode}.");
        await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
        await RefreshStatusAsync(operationId,ct);
        return MapBudget(budget);
    }

    public async Task<IReadOnlyCollection<FundDto>> GetFundsAsync(long operationId,CancellationToken ct){await GetOperationAsync(operationId,ct);var x=await db.ClientFunds.AsNoTracking().Where(f=>f.OperationId==operationId&&!f.IsDeleted).OrderByDescending(f=>f.CreatedAt).ToListAsync(ct);return x.Select(MapFund).ToList();}
    public async Task<FundDto> RegisterFundAsync(long operationId,RegisterFundRequest r,CancellationToken ct)
    {
        var op=await GetOperationAsync(operationId,ct);ValidateMoney(r.ReceivedAmount,r.ExchangeRate);await ValidateCurrencyAsync(r.CurrencyCode,ct);
        var baseAmount=Base(r.ReceivedAmount,r.ExchangeRate);
        var e=new ClientFund{CompanyId=currentCompany.CompanyId,OperationId=operationId,ClientId=op.ClientId,FundStatus="RECEIVED",FundNature="CLIENT_ADVANCE",
            RequestedAmount=r.RequestedAmount,ReceivedAmount=r.ReceivedAmount,CurrencyCode=r.CurrencyCode.Trim().ToUpperInvariant(),ExchangeRate=r.ExchangeRate,BaseAmount=baseAmount,
            RequestedAt=r.RequestedAmount.HasValue?DateTime.UtcNow:null,ReceivedAt=DateTime.UtcNow,PaymentMethodId=r.PaymentMethodId,ReferenceNo=Clean(r.ReferenceNo),Notes=Clean(r.Notes),
            CreatedAt=DateTime.UtcNow,CreatedBy=currentUser.UserId};
        db.ClientFunds.Add(e);AddEvent(operationId,"CLIENT_FUND_RECEIVED","Fondos del cliente recibidos",$"{r.ReceivedAmount:N2} {e.CurrencyCode}.");await db.SaveChangesAsync(ct);await RefreshStatusAsync(operationId,ct);return MapFund(e);
    }

    public async Task<IReadOnlyCollection<ExpenseDto>> GetExpensesAsync(long operationId,CancellationToken ct)
    {
        await GetOperationAsync(operationId,ct);var list=await db.OperationExpenses.AsNoTracking().Where(x=>x.OperationId==operationId&&!x.IsDeleted).OrderByDescending(x=>x.ExpenseDate).ToListAsync(ct);
        var result=new List<ExpenseDto>();foreach(var e in list){var paid=await db.ExpensePayments.AsNoTracking().Where(p=>p.OperationExpenseId==e.OperationExpenseId&&!p.IsVoid).SumAsync(p=>(decimal?)p.BaseAmount,ct)??0;result.Add(MapExpense(e,e.ExpenseStatus=="PAID"&&paid==0?e.BaseAmount:paid));}return result;
    }

    public async Task<ExpenseDto> RegisterExpenseAsync(long operationId,RegisterExpenseRequest r,CancellationToken ct)
    {
        await EnsureFinanceEditableAsync(operationId,ct);if(string.IsNullOrWhiteSpace(r.Description))throw new BusinessException(ErrorCodes.Validation,"Descripción obligatoria.",400);
        ValidateMoney(r.Amount,r.ExchangeRate);await ValidateCurrencyAsync(r.CurrencyCode,ct);
        if(r.PayerType is not ("COMPANY" or "CLIENT"))throw new BusinessException(ErrorCodes.Validation,"Responsable del desembolso inválido.",400);
        if(!await db.ExpenseCategories.AsNoTracking().AnyAsync(x=>x.ExpenseCategoryId==r.ExpenseCategoryId&&x.CompanyId==currentCompany.CompanyId&&x.IsActive,ct))throw new BusinessException(ErrorCodes.Validation,"Categoría de gasto inválida.",400);
        var e=new OperationExpense{CompanyId=currentCompany.CompanyId,OperationId=operationId,ExpenseCategoryId=r.ExpenseCategoryId,SupplierId=r.SupplierId,ExpenseStatus="PAID",
            Description=r.Description.Trim(),DocumentReference=Clean(r.DocumentReference),ExpenseDate=r.ExpenseDate.Date,DueDate=r.DueDate?.Date,CurrencyCode=r.CurrencyCode.Trim().ToUpperInvariant(),
            Amount=r.Amount,ExchangeRate=r.ExchangeRate,BaseAmount=Base(r.Amount,r.ExchangeRate),PaidBy=r.PayerType,IsRebillable=r.IsRebillable,RebillAmount=r.RebillAmount,Notes=Clean(r.Notes),CreatedAt=DateTime.UtcNow,CreatedBy=currentUser.UserId};
        db.OperationExpenses.Add(e);AddEvent(operationId,"EXPENSE_REGISTERED","Gasto registrado",$"{e.Description}: {e.Amount:N2} {e.CurrencyCode}.");await db.SaveChangesAsync(ct);await RefreshStatusAsync(operationId,ct);return MapExpense(e,e.BaseAmount);
    }

    public async Task<ExpenseDto> PayExpenseAsync(long operationId,long expenseId,PayExpenseRequest r,CancellationToken ct)
    {
        await GetOperationAsync(operationId,ct);
        var e=await db.OperationExpenses.SingleOrDefaultAsync(x=>x.OperationExpenseId==expenseId&&x.OperationId==operationId&&x.CompanyId==currentCompany.CompanyId,ct)??throw new BusinessException(ErrorCodes.NotFound,"Gasto no encontrado.",404);
        if(e.ExpenseStatus=="PAID" && !await db.ExpensePayments.AnyAsync(p=>p.OperationExpenseId==expenseId&&!p.IsVoid,ct)) throw new BusinessException("EXPENSE_ALREADY_PAID","El gasto ya se registró como pagado. No corresponde un segundo pago.",409);
        ValidateMoney(r.Amount,r.ExchangeRate);await ValidateCurrencyAsync(r.CurrencyCode,ct);if(r.PayerType is not("COMPANY" or "CLIENT" or "THIRD_PARTY"))throw new BusinessException(ErrorCodes.Validation,"PayerType inválido.",400);
        var paid=await db.ExpensePayments.AsNoTracking().Where(x=>x.OperationExpenseId==expenseId&&!x.IsVoid).SumAsync(x=>(decimal?)x.BaseAmount,ct)??0;
        var paymentBase=Base(r.Amount,r.ExchangeRate);if(paid+paymentBase>e.BaseAmount+0.01m)throw new BusinessException("EXPENSE_OVERPAYMENT","El pago supera el saldo del gasto.",409);
        var p=new ExpensePayment{CompanyId=currentCompany.CompanyId,OperationExpenseId=expenseId,PaymentDate=r.PaymentDate,Amount=r.Amount,CurrencyCode=r.CurrencyCode.Trim().ToUpperInvariant(),
            ExchangeRate=r.ExchangeRate,BaseAmount=paymentBase,PayerType=r.PayerType,PaymentMethodId=r.PaymentMethodId,ReferenceNo=Clean(r.ReferenceNo),Notes=Clean(r.Notes),CreatedAt=DateTime.UtcNow,CreatedBy=currentUser.UserId};
        db.ExpensePayments.Add(p);paid+=paymentBase;e.ExpenseStatus=paid>=e.BaseAmount-0.01m?"PAID":"PARTIALLY_PAID";e.PaidBy=r.PayerType;
        AddEvent(operationId,"EXPENSE_PAID","Pago de gasto registrado",$"{e.Description}: {r.Amount:N2} {p.CurrencyCode} por {r.PayerType}.");await db.SaveChangesAsync(ct);await RefreshStatusAsync(operationId,ct);return MapExpense(e,paid);
    }

    public async Task<IReadOnlyCollection<ExpensePaymentDto>> GetExpensePaymentsAsync(long operationId,long expenseId,CancellationToken ct)
    {
        await GetOperationAsync(operationId,ct);
        var exists=await db.OperationExpenses.AsNoTracking().AnyAsync(e=>e.OperationExpenseId==expenseId&&e.OperationId==operationId&&e.CompanyId==currentCompany.CompanyId&&!e.IsDeleted,ct);
        if(!exists)throw new BusinessException(ErrorCodes.NotFound,"Gasto no encontrado.",404);
        var payments=await db.ExpensePayments.AsNoTracking()
            .Where(p=>p.OperationExpenseId==expenseId&&p.CompanyId==currentCompany.CompanyId)
            .OrderByDescending(p=>p.PaymentDate).ThenByDescending(p=>p.ExpensePaymentId).ToListAsync(ct);
        return payments.Select(p=>new ExpensePaymentDto(p.ExpensePaymentId,p.OperationExpenseId,p.PaymentDate,p.CurrencyCode,p.Amount,p.BaseAmount,p.PayerType,p.PaymentMethodId,p.ReferenceNo,p.Notes,p.IsVoid,ConcurrencyToken.Encode(p.RowVer))).ToList();
    }

    public async Task<ExpenseDto> VoidExpensePaymentAsync(long operationId,long expenseId,long paymentId,VoidExpensePaymentRequest request,CancellationToken ct)
    {
        await EnsureFinanceEditableAsync(operationId,ct);
        RequireReason(request.Reason);
        var e=await db.OperationExpenses.SingleOrDefaultAsync(x=>x.OperationExpenseId==expenseId&&x.OperationId==operationId&&x.CompanyId==currentCompany.CompanyId&&!x.IsDeleted,ct)
            ??throw new BusinessException(ErrorCodes.NotFound,"Gasto no encontrado.",404);
        var p=await db.ExpensePayments.SingleOrDefaultAsync(x=>x.ExpensePaymentId==paymentId&&x.OperationExpenseId==expenseId&&x.CompanyId==currentCompany.CompanyId&&!x.IsVoid,ct)
            ??throw new BusinessException(ErrorCodes.NotFound,"Pago no encontrado o ya anulado.",404);
        db.Entry(p).Property(x=>x.RowVer).OriginalValue=ConcurrencyToken.Decode(request.RowVersion);
        p.IsVoid=true;p.VoidedAt=DateTime.UtcNow;p.VoidedBy=currentUser.UserId;p.VoidReason=request.Reason.Trim();
        await db.SaveChangesAsync(ct);
        var remaining=await db.ExpensePayments.AsNoTracking().Where(x=>x.OperationExpenseId==expenseId&&!x.IsVoid).SumAsync(x=>(decimal?)x.BaseAmount,ct)??0;
        e.ExpenseStatus=remaining<=0?"PENDING":remaining>=e.BaseAmount-0.01m?"PAID":"PARTIALLY_PAID";
        e.PaidBy=remaining<=0?null:await db.ExpensePayments.AsNoTracking().Where(x=>x.OperationExpenseId==expenseId&&!x.IsVoid).OrderByDescending(x=>x.PaymentDate).Select(x=>x.PayerType).FirstOrDefaultAsync(ct);
        e.ModifiedAt=DateTime.UtcNow;e.ModifiedBy=currentUser.UserId;
        AddEvent(operationId,"EXPENSE_PAYMENT_VOIDED","Pago a tercero anulado",$"Pago {paymentId} del gasto {expenseId}. Motivo: {request.Reason.Trim()}");
        await db.SaveChangesAsync(ct);
        await RefreshStatusAsync(operationId,ct);
        return MapExpense(e,remaining);
    }

    public async Task<IReadOnlyCollection<ServiceDto>> GetServicesAsync(long operationId,CancellationToken ct)
    {
        await GetOperationAsync(operationId,ct);var list=await db.OperationServices.AsNoTracking().Where(x=>x.OperationId==operationId).ToListAsync(ct);var result=new List<ServiceDto>();
        foreach(var s in list){var col=await (from a in db.ServiceCollectionAllocations.AsNoTracking() where a.OperationServiceId==s.OperationServiceId select a.AllocatedAmount).SumAsync(ct);result.Add(MapService(s,col));}return result;
    }

    public async Task<IReadOnlyCollection<CollectionDto>> GetCollectionsAsync(long operationId,CancellationToken ct){await GetOperationAsync(operationId,ct);var x=await db.ClientCollections.AsNoTracking().Where(c=>c.OperationId==operationId).OrderByDescending(c=>c.CollectionDate).ToListAsync(ct);return x.Select(MapCollection).ToList();}
    public async Task<CollectionDto> RegisterCollectionAsync(long operationId,RegisterCollectionRequest r,CancellationToken ct)
    {
        await using var collectionTx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable,ct);
        var op=await GetOperationAsync(operationId,ct);ValidateMoney(r.Amount,r.ExchangeRate);await ValidateCurrencyAsync(r.CurrencyCode,ct);if(r.CollectionType is not("SERVICE" or "FUND" or "OTHER" or "LIQUIDATION"))throw new BusinessException(ErrorCodes.Validation,"CollectionType inválido.",400);
        if(r.CollectionType=="LIQUIDATION") { var balances=await LiquidationBalancesAsync(operationId,ct);var code=r.CurrencyCode.Trim().ToUpperInvariant();if(!balances.TryGetValue(code,out var balance)||r.Amount>balance+0.005m)throw new BusinessException(ErrorCodes.Validation,"El cobro excede el saldo pendiente de la liquidación en "+code+".",400); }
        var e=new ClientCollection{CompanyId=currentCompany.CompanyId,OperationId=operationId,ClientId=op.ClientId,CollectionType=r.CollectionType,CollectionStatus="RECEIVED",CollectionDate=r.CollectionDate,
            CurrencyCode=r.CurrencyCode.Trim().ToUpperInvariant(),Amount=r.Amount,ExchangeRate=r.ExchangeRate,BaseAmount=Base(r.Amount,r.ExchangeRate),PaymentMethodId=r.PaymentMethodId,ReferenceNo=Clean(r.ReferenceNo),Notes=Clean(r.Notes),CreatedAt=DateTime.UtcNow,CreatedBy=currentUser.UserId};
        db.ClientCollections.Add(e);AddEvent(operationId,"CLIENT_COLLECTION_RECEIVED","Cobro recibido",$"{r.Amount:N2} {e.CurrencyCode} ({e.CollectionType}).");await db.SaveChangesAsync(ct);await RefreshStatusAsync(operationId,ct);await collectionTx.CommitAsync(ct);return MapCollection(e);
    }

    public async Task AllocateCollectionAsync(long operationId,long collectionId,AllocateCollectionRequest r,CancellationToken ct)
    {
        await GetOperationAsync(operationId,ct);
        if(r.Amount<=0)throw new BusinessException(ErrorCodes.Validation,"Monto de aplicación debe ser > 0.",400);
        var c=await db.ClientCollections.SingleOrDefaultAsync(x=>x.ClientCollectionId==collectionId&&x.OperationId==operationId&&!x.IsVoid,ct)??throw new BusinessException(ErrorCodes.NotFound,"Cobro no encontrado.",404);
        var s=await db.OperationServices.SingleOrDefaultAsync(x=>x.OperationServiceId==r.OperationServiceId&&x.OperationId==operationId,ct)??throw new BusinessException(ErrorCodes.NotFound,"Servicio no encontrado.",404);
        var allocatedToCollection=await db.ServiceCollectionAllocations.AsNoTracking().Where(x=>x.ClientCollectionId==collectionId).SumAsync(x=>(decimal?)x.AllocatedAmount,ct)??0;
        var allocatedToService=await db.ServiceCollectionAllocations.AsNoTracking().Where(x=>x.OperationServiceId==s.OperationServiceId).SumAsync(x=>(decimal?)x.AllocatedAmount,ct)??0;
        if(allocatedToCollection+r.Amount>c.BaseAmount+0.01m)throw new BusinessException("COLLECTION_OVERALLOCATION","La aplicación supera el monto del cobro.",409);
        if(allocatedToService+r.Amount>s.BaseAmount+0.01m)throw new BusinessException("SERVICE_OVERALLOCATION","La aplicación supera el saldo del servicio.",409);
        db.ServiceCollectionAllocations.Add(new(){OperationServiceId=s.OperationServiceId,ClientCollectionId=c.ClientCollectionId,AllocatedAmount=r.Amount,AllocatedAt=DateTime.UtcNow,CreatedBy=currentUser.UserId});
        allocatedToService+=r.Amount;s.ServiceStatus=allocatedToService>=s.BaseAmount-0.01m?"COLLECTED":"PARTIALLY_COLLECTED";c.CollectionStatus="APPLIED";await db.SaveChangesAsync(ct);await RefreshStatusAsync(operationId,ct);
    }

    public async Task RegisterAdjustmentAsync(long operationId,RegisterAdjustmentRequest r,CancellationToken ct)
    {
        await GetOperationAsync(operationId,ct);ValidateMoney(r.Amount,r.ExchangeRate);await ValidateCurrencyAsync(r.CurrencyCode,ct);if(r.AdjustmentType is not("DEBIT" or "CREDIT" or "CORRECTION"))throw new BusinessException(ErrorCodes.Validation,"AdjustmentType inválido.",400);
        if(string.IsNullOrWhiteSpace(r.Reason))throw new BusinessException(ErrorCodes.Validation,"Razón obligatoria.",400);
        db.OperationAdjustments.Add(new(){CompanyId=currentCompany.CompanyId,OperationId=operationId,AdjustmentType=r.AdjustmentType,Reason=r.Reason.Trim(),CurrencyCode=r.CurrencyCode.Trim().ToUpperInvariant(),Amount=r.Amount,ExchangeRate=r.ExchangeRate,BaseAmount=Base(r.Amount,r.ExchangeRate),AdjustmentDate=r.AdjustmentDate.Date,Notes=Clean(r.Notes),CreatedAt=DateTime.UtcNow,CreatedBy=currentUser.UserId});
        AddEvent(operationId,"FINANCIAL_ADJUSTMENT","Ajuste financiero",$"{r.AdjustmentType}: {r.Amount:N2} {r.CurrencyCode}.");await db.SaveChangesAsync(ct);await RefreshStatusAsync(operationId,ct);
    }

    public async Task<IReadOnlyCollection<RefundDto>> GetRefundsAsync(long operationId,CancellationToken ct){await GetOperationAsync(operationId,ct);var x=await db.ClientRefunds.AsNoTracking().Where(r=>r.OperationId==operationId).ToListAsync(ct);return x.Select(MapRefund).ToList();}
    public async Task<RefundDto> RegisterRefundAsync(long operationId,RegisterRefundRequest r,CancellationToken ct)
    {
        var op=await GetOperationAsync(operationId,ct);ValidateMoney(r.Amount,r.ExchangeRate);await ValidateCurrencyAsync(r.CurrencyCode,ct);if(string.IsNullOrWhiteSpace(r.Reason))throw new BusinessException(ErrorCodes.Validation,"Razón obligatoria.",400);
        if(r.CurrencyCode.Equals("PEN",StringComparison.OrdinalIgnoreCase)){
            var advancesPen=await db.ClientFunds.AsNoTracking().Where(x=>x.CompanyId==currentCompany.CompanyId&&x.OperationId==operationId&&!x.IsDeleted&&x.FundStatus!="CANCELLED"&&x.CurrencyCode=="PEN").SumAsync(x=>(decimal?)x.ReceivedAmount,ct)??0;
            var refundedPen=await db.ClientRefunds.AsNoTracking().Where(x=>x.CompanyId==currentCompany.CompanyId&&x.OperationId==operationId&&x.RefundStatus=="COMPLETED"&&x.CurrencyCode=="PEN").SumAsync(x=>(decimal?)x.Amount,ct)??0;
            var customsPen=await db.CustomsTaxFinancePayments.AsNoTracking().Where(x=>x.CompanyId==currentCompany.CompanyId&&x.OperationId==operationId&&!x.IsVoid&&x.FundingSource=="CLIENT_ADVANCE").SumAsync(x=>(decimal?)x.AmountPen,ct)??0;
            if(r.Amount>advancesPen-refundedPen-customsPen+0.01m)throw new BusinessException("REFUND_EXCEEDS_PEN_ADVANCE","La devolución PEN excede los anticipos no aplicados a aduanas.",409);
        }
        var summary=await GetSummaryAsync(operationId,ct);if(Base(r.Amount,r.ExchangeRate)>summary.ManagedFundBalance+0.01m)throw new BusinessException("REFUND_EXCEEDS_BALANCE","La devolución supera el saldo de fondos administrados.",409);
        var e=new ClientRefund{CompanyId=currentCompany.CompanyId,OperationId=operationId,ClientId=op.ClientId,RefundStatus="COMPLETED",RefundDate=DateTime.UtcNow,CurrencyCode=r.CurrencyCode.Trim().ToUpperInvariant(),Amount=r.Amount,ExchangeRate=r.ExchangeRate,BaseAmount=Base(r.Amount,r.ExchangeRate),PaymentMethodId=r.PaymentMethodId,ReferenceNo=Clean(r.ReferenceNo),Reason=r.Reason.Trim(),Notes=Clean(r.Notes),CreatedAt=DateTime.UtcNow,CreatedBy=currentUser.UserId};
        db.ClientRefunds.Add(e);AddEvent(operationId,"CLIENT_REFUND","Devolución al cliente",$"{r.Amount:N2} {e.CurrencyCode}.");await db.SaveChangesAsync(ct);await RefreshStatusAsync(operationId,ct);return MapRefund(e);
    }

    public async Task<SettlementDto> PrepareSettlementAsync(long operationId,CancellationToken ct)
    {
        var op=await GetOperationAsync(operationId,ct);var stage=await db.WorkflowStages.AsNoTracking().SingleAsync(x=>x.WorkflowStageId==op.CurrentStageId,ct);
        if(stage.Code!="SETTLEMENT")throw new BusinessException("SETTLEMENT_STAGE_REQUIRED","La operación debe estar en etapa SETTLEMENT.",409);
        var summary=await GetSummaryAsync(operationId,ct);var existing=await db.OperationSettlements.SingleOrDefaultAsync(x=>x.OperationId==operationId,ct);
        var s=existing??new OperationSettlement{CompanyId=currentCompany.CompanyId,OperationId=operationId,SettlementStatus="DRAFT",BaseCurrencyCode=op.QuotationCurrencySnapshot??"PEN",CreatedAt=DateTime.UtcNow,CreatedBy=currentUser.UserId};
        s.TotalClientFunds=summary.TotalClientFunds;s.TotalExpenses=summary.TotalExpenses;s.TotalCompanyServices=summary.TotalServices;s.TotalCollections=summary.TotalCollections;s.TotalRefunds=summary.TotalRefunds;s.ClientBalance=summary.ClientBalance;s.SettlementDate=DateTime.UtcNow;s.ModifiedAt=DateTime.UtcNow;s.ModifiedBy=currentUser.UserId;
        if(existing is null)db.OperationSettlements.Add(s);await db.SaveChangesAsync(ct);await operationService.SetFinancialStatusAsync(operationId,FinancialStatuses.PendingSettlement,"Rendición preparada.",ct);return MapSettlement(s);
    }

    public async Task<SettlementDto> ApproveSettlementAsync(long operationId,ApproveSettlementRequest r,CancellationToken ct)
    {
        var s=await db.OperationSettlements.SingleOrDefaultAsync(x=>x.OperationId==operationId&&x.CompanyId==currentCompany.CompanyId,ct)??throw new BusinessException(ErrorCodes.NotFound,"Rendición no preparada.",404);
        db.Entry(s).Property(x=>x.RowVer).OriginalValue=ConcurrencyToken.Decode(r.RowVersion);var summary=await GetSummaryAsync(operationId,ct);
        // El registro financiero representa desembolsos ya realizados. No exigir un pago
        // adicional ni la aplicación previa de anticipos para confirmar la validación.
        // Solo impedir una clasificación ambigua que pudiera generar un cobro indebido.
        var expensesToReview=await db.OperationExpenses.AsNoTracking().CountAsync(x=>
            x.CompanyId==currentCompany.CompanyId && x.OperationId==operationId && !x.IsDeleted &&
            x.ExpenseStatus!="CANCELLED" && x.IsRebillable &&
            (x.PaidBy==null || (x.PaidBy!="COMPANY" && x.PaidBy!="CLIENT")),ct);
        var taxesToReview=await db.CustomsTaxFinanceItems.AsNoTracking().CountAsync(x=>
            x.CompanyId==currentCompany.CompanyId && x.OperationId==operationId && !x.IsVoid &&
            x.PayerType!="COMPANY" && x.PayerType!="CLIENT",ct);
        if(expensesToReview+taxesToReview>0)
            throw new BusinessException("DISBURSEMENT_REVIEW_REQUIRED","Hay gastos o tributos sin responsable de desembolso confirmado.",409);
        s.SettlementStatus="APPROVED";s.ApprovedAt=DateTime.UtcNow;s.ApprovedByUserId=currentUser.UserId;s.Notes=Clean(r.Notes);await db.SaveChangesAsync(ct);
        await operationService.SetFinancialStatusAsync(operationId,summary.PendingServiceCollection>0.01m?FinancialStatuses.PendingCollection:FinancialStatuses.Closed,"Validación financiera confirmada; cobranza independiente.",ct);AddEvent(operationId,"SETTLEMENT_APPROVED","Validación financiera confirmada","Responsables revisados; anticipos y cobranza independientes.");await db.SaveChangesAsync(ct);return MapSettlement(s);
    }



    private async Task ValidateServiceAsync(SaveServiceRequest r,CancellationToken ct)
    {
        if(string.IsNullOrWhiteSpace(r.Description))throw new BusinessException(ErrorCodes.Validation,"Descripción obligatoria.",400);
        ValidateMoney(r.Amount,r.ExchangeRate);await ValidateCurrencyAsync(r.CurrencyCode,ct);
        if(!await db.ServiceCategories.AsNoTracking().AnyAsync(x=>x.ServiceCategoryId==r.ServiceCategoryId&&x.CompanyId==currentCompany.CompanyId&&x.IsActive,ct))
            throw new BusinessException(ErrorCodes.Validation,"Categoría de servicio inválida.",400);
    }
    public async Task<ServiceDto> AddServiceAsync(long operationId,SaveServiceRequest r,CancellationToken ct)
    {
        await EnsureFinanceEditableAsync(operationId,ct);await ValidateServiceAsync(r,ct);
        var x=new OperationServiceCharge{CompanyId=currentCompany.CompanyId,OperationId=operationId,ServiceCategoryId=r.ServiceCategoryId,
            ServiceStatus="PENDING",Description=r.Description.Trim(),CurrencyCode=r.CurrencyCode.Trim().ToUpperInvariant(),Amount=r.Amount,
            ExchangeRate=r.ExchangeRate,BaseAmount=Base(r.Amount,r.ExchangeRate),Notes=Clean(r.Notes),CreatedAt=DateTime.UtcNow,CreatedBy=currentUser.UserId};
        db.OperationServices.Add(x);AddEvent(operationId,"SERVICE_CREATED","Servicio R&B registrado",x.Description);
        await db.SaveChangesAsync(ct);await RefreshStatusAsync(operationId,ct);return MapService(x,0);
    }
    public async Task<ServiceDto> UpdateServiceAsync(long operationId,long id,EditServiceRequest request,CancellationToken ct)
    {
        await EnsureFinanceEditableAsync(operationId,ct);
        var x=await db.OperationServices.SingleOrDefaultAsync(v=>v.OperationServiceId==id&&v.OperationId==operationId&&v.CompanyId==currentCompany.CompanyId&&!v.IsDeleted,ct)
            ??throw new BusinessException(ErrorCodes.NotFound,"Servicio no encontrado.",404);
        if(await db.ServiceCollectionAllocations.AnyAsync(v=>v.OperationServiceId==id,ct))throw new BusinessException("SERVICE_HAS_COLLECTIONS","El servicio tiene cobros aplicados y no se puede editar.",409);
        db.Entry(x).Property(v=>v.RowVer).OriginalValue=ConcurrencyToken.Decode(request.RowVersion);
        var r=request.Data;await ValidateServiceAsync(r,ct);
        x.ServiceCategoryId=r.ServiceCategoryId;x.Description=r.Description.Trim();x.CurrencyCode=r.CurrencyCode.Trim().ToUpperInvariant();x.Amount=r.Amount;
        x.ExchangeRate=r.ExchangeRate;x.BaseAmount=Base(r.Amount,r.ExchangeRate);x.Notes=Clean(r.Notes);x.ModifiedAt=DateTime.UtcNow;x.ModifiedBy=currentUser.UserId;
        AddEvent(operationId,"SERVICE_UPDATED","Servicio R&B modificado",x.Description);
        await db.SaveChangesAsync(ct);await RefreshStatusAsync(operationId,ct);return MapService(x,0);
    }
    public async Task VoidServiceAsync(long operationId,long id,VoidFinanceRequest request,CancellationToken ct)
    {
        await EnsureFinanceEditableAsync(operationId,ct);RequireReason(request.Reason);
        var x=await db.OperationServices.SingleOrDefaultAsync(v=>v.OperationServiceId==id&&v.OperationId==operationId&&v.CompanyId==currentCompany.CompanyId&&!v.IsDeleted,ct)
            ??throw new BusinessException(ErrorCodes.NotFound,"Servicio no encontrado.",404);
        if(await db.ServiceCollectionAllocations.AnyAsync(v=>v.OperationServiceId==id,ct))throw new BusinessException("SERVICE_HAS_COLLECTIONS","El servicio tiene cobros aplicados y no se puede anular.",409);
        db.Entry(x).Property(v=>v.RowVer).OriginalValue=ConcurrencyToken.Decode(request.RowVersion);
        x.IsDeleted=true;x.ServiceStatus="CANCELLED";x.ModifiedAt=DateTime.UtcNow;x.ModifiedBy=currentUser.UserId;
        AddEvent(operationId,"SERVICE_VOIDED","Servicio R&B anulado",$"Servicio {id}. Motivo: {request.Reason.Trim()}");
        await db.SaveChangesAsync(ct);await RefreshStatusAsync(operationId,ct);
    }

    private async Task EnsureFinanceEditableAsync(long operationId,CancellationToken ct)
    {
        await GetOperationAsync(operationId,ct);
        if(await db.IssuedLiquidations.AsNoTracking().AnyAsync(x=>x.OperationId==operationId&&x.CompanyId==currentCompany.CompanyId,ct))
            throw new BusinessException("LIQUIDATION_ALREADY_ISSUED","La liquidación ya fue emitida. Se requiere rectificación formal.",409);
        if(await db.OperationSettlements.AsNoTracking().AnyAsync(x=>x.OperationId==operationId&&x.CompanyId==currentCompany.CompanyId&&x.SettlementStatus=="APPROVED",ct))
            throw new BusinessException("SETTLEMENT_ALREADY_APPROVED","La rendición está aprobada. Se requiere reapertura autorizada.",409);
    }

    private static void RequireReason(string reason)
    {
        if(string.IsNullOrWhiteSpace(reason))throw new BusinessException(ErrorCodes.Validation,"Indica el motivo de anulación.",400);
    }

    public async Task<FundDto> UpdateFundAsync(long operationId,long id,EditFundRequest request,CancellationToken ct)
    {
        await EnsureFinanceEditableAsync(operationId,ct);
        var x=await db.ClientFunds.SingleOrDefaultAsync(v=>v.ClientFundId==id&&v.OperationId==operationId&&v.CompanyId==currentCompany.CompanyId&&!v.IsDeleted,ct)
            ??throw new BusinessException(ErrorCodes.NotFound,"Anticipo no encontrado.",404);
        db.Entry(x).Property(v=>v.RowVer).OriginalValue=ConcurrencyToken.Decode(request.RowVersion);
        var r=request.Data;ValidateMoney(r.ReceivedAmount,r.ExchangeRate);await ValidateCurrencyAsync(r.CurrencyCode,ct);
        x.RequestedAmount=r.RequestedAmount;x.ReceivedAmount=r.ReceivedAmount;x.CurrencyCode=r.CurrencyCode.Trim().ToUpperInvariant();x.ExchangeRate=r.ExchangeRate;x.BaseAmount=Base(r.ReceivedAmount,r.ExchangeRate);
        var current=await GetSummaryAsync(operationId,ct);
        if(current.ManagedFundBalance-(x.BaseAmount??0)+Base(r.ReceivedAmount,r.ExchangeRate)<-0.01m)
            throw new BusinessException("FUND_ALREADY_USED","No se puede reducir el anticipo por debajo de los desembolsos y devoluciones registrados.",409);
        x.PaymentMethodId=r.PaymentMethodId;x.ReferenceNo=Clean(r.ReferenceNo);x.Notes=Clean(r.Notes);x.ModifiedAt=DateTime.UtcNow;x.ModifiedBy=currentUser.UserId;
        AddEvent(operationId,"FUND_UPDATED","Anticipo corregido",$"Anticipo {id} actualizado por {currentUser.UserId}.");
        await db.SaveChangesAsync(ct);await RefreshStatusAsync(operationId,ct);return MapFund(x);
    }
    public async Task VoidFundAsync(long operationId,long id,VoidFinanceRequest request,CancellationToken ct)
    {
        await EnsureFinanceEditableAsync(operationId,ct);RequireReason(request.Reason);
        var x=await db.ClientFunds.SingleOrDefaultAsync(v=>v.ClientFundId==id&&v.OperationId==operationId&&v.CompanyId==currentCompany.CompanyId&&!v.IsDeleted,ct)
            ??throw new BusinessException(ErrorCodes.NotFound,"Anticipo no encontrado.",404);
        db.Entry(x).Property(v=>v.RowVer).OriginalValue=ConcurrencyToken.Decode(request.RowVersion);
        var current=await GetSummaryAsync(operationId,ct);
        if(current.ManagedFundBalance-(x.BaseAmount??0)<-0.01m)
            throw new BusinessException("FUND_ALREADY_USED","El anticipo financia pagos o devoluciones y no puede anularse directamente.",409);
        x.IsDeleted=true;x.FundStatus="CANCELLED";x.ModifiedAt=DateTime.UtcNow;x.ModifiedBy=currentUser.UserId;
        AddEvent(operationId,"FUND_VOIDED","Anticipo anulado",$"Anticipo {id}. Motivo: {request.Reason.Trim()}");await db.SaveChangesAsync(ct);await RefreshStatusAsync(operationId,ct);
    }
    public async Task<ExpenseDto> UpdateExpenseAsync(long operationId,long id,EditExpenseRequest request,CancellationToken ct)
    {
        await EnsureFinanceEditableAsync(operationId,ct);
        var x=await db.OperationExpenses.SingleOrDefaultAsync(v=>v.OperationExpenseId==id&&v.OperationId==operationId&&v.CompanyId==currentCompany.CompanyId&&!v.IsDeleted,ct)
            ??throw new BusinessException(ErrorCodes.NotFound,"Gasto no encontrado.",404);
        if(await db.ExpensePayments.AnyAsync(p=>p.OperationExpenseId==id&&!p.IsVoid,ct))throw new BusinessException("EXPENSE_HAS_PAYMENTS","No se puede editar un gasto con pagos registrados.",409);
        db.Entry(x).Property(v=>v.RowVer).OriginalValue=ConcurrencyToken.Decode(request.RowVersion);
        var r=request.Data;if(string.IsNullOrWhiteSpace(r.Description))throw new BusinessException(ErrorCodes.Validation,"Descripción obligatoria.",400);
        ValidateMoney(r.Amount,r.ExchangeRate);await ValidateCurrencyAsync(r.CurrencyCode,ct);
        var documented=await db.ExpensePaymentReceipts.AsNoTracking()
            .Where(v=>v.CompanyId==currentCompany.CompanyId&&v.OperationExpenseId==id&&!v.IsVoid)
            .SumAsync(v=>(decimal?)v.Amount,ct)??0m;
        if(documented>0&&(r.Amount<documented||!string.Equals(r.CurrencyCode,x.CurrencyCode,StringComparison.OrdinalIgnoreCase)))
            throw new BusinessException("EXPENSE_HAS_RECEIPTS","El gasto tiene constancias: no se puede reducir por debajo de lo documentado ni cambiar la moneda. Anula las constancias antes.",409);
        if(!await db.ExpenseCategories.AsNoTracking().AnyAsync(v=>v.ExpenseCategoryId==r.ExpenseCategoryId&&v.CompanyId==currentCompany.CompanyId&&v.IsActive,ct))throw new BusinessException(ErrorCodes.Validation,"Categoría inválida.",400);
        x.ExpenseCategoryId=r.ExpenseCategoryId;x.SupplierId=r.SupplierId;x.Description=r.Description.Trim();x.DocumentReference=Clean(r.DocumentReference);
        x.ExpenseDate=r.ExpenseDate.Date;x.DueDate=r.DueDate?.Date;x.CurrencyCode=r.CurrencyCode.Trim().ToUpperInvariant();x.Amount=r.Amount;x.ExchangeRate=r.ExchangeRate;x.BaseAmount=Base(r.Amount,r.ExchangeRate);
        if(r.PayerType is not ("COMPANY" or "CLIENT"))throw new BusinessException(ErrorCodes.Validation,"Responsable del desembolso inválido.",400);
        x.PaidBy=r.PayerType;x.IsRebillable=r.IsRebillable;x.RebillAmount=r.RebillAmount;x.Notes=Clean(r.Notes);x.ModifiedAt=DateTime.UtcNow;x.ModifiedBy=currentUser.UserId;
        AddEvent(operationId,"EXPENSE_UPDATED","Gasto corregido",$"Gasto {id}: {x.Description}, {x.Amount:N2} {x.CurrencyCode}.");await db.SaveChangesAsync(ct);await RefreshStatusAsync(operationId,ct);return MapExpense(x,x.ExpenseStatus=="PAID"?x.BaseAmount:0);
    }
    public async Task VoidExpenseAsync(long operationId,long id,VoidFinanceRequest request,CancellationToken ct)
    {
        await EnsureFinanceEditableAsync(operationId,ct);RequireReason(request.Reason);
        var x=await db.OperationExpenses.SingleOrDefaultAsync(v=>v.OperationExpenseId==id&&v.OperationId==operationId&&v.CompanyId==currentCompany.CompanyId&&!v.IsDeleted,ct)
            ??throw new BusinessException(ErrorCodes.NotFound,"Gasto no encontrado.",404);
        if(await db.ExpensePayments.AnyAsync(p=>p.OperationExpenseId==id&&!p.IsVoid,ct))throw new BusinessException("EXPENSE_HAS_PAYMENTS","El gasto tiene pagos. Anula primero los pagos mediante un flujo autorizado.",409);
        if(await db.ExpensePaymentReceipts.AnyAsync(p=>p.CompanyId==currentCompany.CompanyId&&p.OperationExpenseId==id&&!p.IsVoid,ct))
            throw new BusinessException("EXPENSE_HAS_RECEIPTS","El gasto tiene constancias PDF. Anula primero las constancias antes de anular el gasto.",409);
        db.Entry(x).Property(v=>v.RowVer).OriginalValue=ConcurrencyToken.Decode(request.RowVersion);
        x.IsDeleted=true;x.ExpenseStatus="CANCELLED";x.ModifiedAt=DateTime.UtcNow;x.ModifiedBy=currentUser.UserId;
        AddEvent(operationId,"EXPENSE_VOIDED","Gasto anulado",$"Gasto {id}. Motivo: {request.Reason.Trim()}");await db.SaveChangesAsync(ct);await RefreshStatusAsync(operationId,ct);
    }
    public async Task<CollectionDto> UpdateCollectionAsync(long operationId,long id,EditCollectionRequest request,CancellationToken ct)
    {
        await EnsureFinanceEditableAsync(operationId,ct);
        var x=await db.ClientCollections.SingleOrDefaultAsync(v=>v.ClientCollectionId==id&&v.OperationId==operationId&&v.CompanyId==currentCompany.CompanyId&&!v.IsVoid,ct)
            ??throw new BusinessException(ErrorCodes.NotFound,"Cobro no encontrado.",404);
        if(await db.ServiceCollectionAllocations.AnyAsync(v=>v.ClientCollectionId==id,ct))throw new BusinessException("COLLECTION_ALLOCATED","El cobro ya está aplicado a servicios.",409);
        db.Entry(x).Property(v=>v.RowVer).OriginalValue=ConcurrencyToken.Decode(request.RowVersion);
        var r=request.Data;ValidateMoney(r.Amount,r.ExchangeRate);await ValidateCurrencyAsync(r.CurrencyCode,ct);
        if(r.CollectionType is not("SERVICE" or "FUND" or "OTHER" or "LIQUIDATION"))throw new BusinessException(ErrorCodes.Validation,"Tipo de cobro inválido.",400);
        if(r.CollectionType=="LIQUIDATION")throw new BusinessException(ErrorCodes.Validation,"Los cobros de liquidación emitida no se editan; registre una reversión autorizada.",409);
        x.CollectionType=r.CollectionType;x.CollectionDate=r.CollectionDate;x.Amount=r.Amount;x.CurrencyCode=r.CurrencyCode.Trim().ToUpperInvariant();x.ExchangeRate=r.ExchangeRate;x.BaseAmount=Base(r.Amount,r.ExchangeRate);
        x.PaymentMethodId=r.PaymentMethodId;x.ReferenceNo=Clean(r.ReferenceNo);x.Notes=Clean(r.Notes);
        AddEvent(operationId,"COLLECTION_UPDATED","Cobro corregido",$"Cobro {id} actualizado.");await db.SaveChangesAsync(ct);await RefreshStatusAsync(operationId,ct);return MapCollection(x);
    }
    public async Task VoidCollectionAsync(long operationId,long id,VoidFinanceRequest request,CancellationToken ct)
    {
        await EnsureFinanceEditableAsync(operationId,ct);RequireReason(request.Reason);
        var x=await db.ClientCollections.SingleOrDefaultAsync(v=>v.ClientCollectionId==id&&v.OperationId==operationId&&v.CompanyId==currentCompany.CompanyId&&!v.IsVoid,ct)
            ??throw new BusinessException(ErrorCodes.NotFound,"Cobro no encontrado.",404);
        if(await db.ServiceCollectionAllocations.AnyAsync(v=>v.ClientCollectionId==id,ct))throw new BusinessException("COLLECTION_ALLOCATED","El cobro tiene aplicaciones y no puede anularse directamente.",409);
        db.Entry(x).Property(v=>v.RowVer).OriginalValue=ConcurrencyToken.Decode(request.RowVersion);
        x.IsVoid=true;x.CollectionStatus="VOID";x.VoidedAt=DateTime.UtcNow;x.VoidedBy=currentUser.UserId;x.VoidReason=request.Reason.Trim();
        AddEvent(operationId,"COLLECTION_VOIDED","Cobro anulado",$"Cobro {id}. Motivo: {request.Reason.Trim()}");await db.SaveChangesAsync(ct);await RefreshStatusAsync(operationId,ct);
    }
    public async Task<RefundDto> UpdateRefundAsync(long operationId,long id,EditRefundRequest request,CancellationToken ct)
    {
        await EnsureFinanceEditableAsync(operationId,ct);
        var x=await db.ClientRefunds.SingleOrDefaultAsync(v=>v.ClientRefundId==id&&v.OperationId==operationId&&v.CompanyId==currentCompany.CompanyId&&v.RefundStatus!="CANCELLED",ct)
            ??throw new BusinessException(ErrorCodes.NotFound,"Devolución no encontrada.",404);
        db.Entry(x).Property(v=>v.RowVer).OriginalValue=ConcurrencyToken.Decode(request.RowVersion);
        var r=request.Data;ValidateMoney(r.Amount,r.ExchangeRate);await ValidateCurrencyAsync(r.CurrencyCode,ct);
        if(string.IsNullOrWhiteSpace(r.Reason))throw new BusinessException(ErrorCodes.Validation,"Razón obligatoria.",400);
        var summary=await GetSummaryAsync(operationId,ct);var newBase=Base(r.Amount,r.ExchangeRate);
        if(newBase>x.BaseAmount+summary.ManagedFundBalance+0.01m)throw new BusinessException("REFUND_EXCEEDS_BALANCE","La devolución supera el saldo disponible.",409);
        if(r.CurrencyCode.Equals("PEN",StringComparison.OrdinalIgnoreCase)){
            var advancesPen=await db.ClientFunds.AsNoTracking().Where(v=>v.CompanyId==currentCompany.CompanyId&&v.OperationId==operationId&&!v.IsDeleted&&v.FundStatus!="CANCELLED"&&v.CurrencyCode=="PEN").SumAsync(v=>(decimal?)v.ReceivedAmount,ct)??0;
            var refundedOther=await db.ClientRefunds.AsNoTracking().Where(v=>v.CompanyId==currentCompany.CompanyId&&v.OperationId==operationId&&v.ClientRefundId!=id&&v.RefundStatus=="COMPLETED"&&v.CurrencyCode=="PEN").SumAsync(v=>(decimal?)v.Amount,ct)??0;
            var applied=await db.CustomsTaxFinancePayments.AsNoTracking().Where(v=>v.CompanyId==currentCompany.CompanyId&&v.OperationId==operationId&&!v.IsVoid&&v.FundingSource=="CLIENT_ADVANCE").SumAsync(v=>(decimal?)v.AmountPen,ct)??0;
            if(r.Amount>advancesPen-refundedOther-applied+0.01m)throw new BusinessException("REFUND_EXCEEDS_PEN_ADVANCE","La devolución PEN excede el anticipo no aplicado a tributos.",409);
        }
        x.Amount=r.Amount;x.CurrencyCode=r.CurrencyCode.Trim().ToUpperInvariant();x.ExchangeRate=r.ExchangeRate;x.BaseAmount=newBase;x.PaymentMethodId=r.PaymentMethodId;x.ReferenceNo=Clean(r.ReferenceNo);x.Reason=r.Reason.Trim();x.Notes=Clean(r.Notes);x.ModifiedAt=DateTime.UtcNow;x.ModifiedBy=currentUser.UserId;
        AddEvent(operationId,"REFUND_UPDATED","Devolución corregida",$"Devolución {id} actualizada.");await db.SaveChangesAsync(ct);await RefreshStatusAsync(operationId,ct);return MapRefund(x);
    }
    public async Task VoidRefundAsync(long operationId,long id,VoidFinanceRequest request,CancellationToken ct)
    {
        await EnsureFinanceEditableAsync(operationId,ct);RequireReason(request.Reason);
        var x=await db.ClientRefunds.SingleOrDefaultAsync(v=>v.ClientRefundId==id&&v.OperationId==operationId&&v.CompanyId==currentCompany.CompanyId&&v.RefundStatus!="CANCELLED",ct)
            ??throw new BusinessException(ErrorCodes.NotFound,"Devolución no encontrada.",404);
        db.Entry(x).Property(v=>v.RowVer).OriginalValue=ConcurrencyToken.Decode(request.RowVersion);
        x.RefundStatus="CANCELLED";x.ModifiedAt=DateTime.UtcNow;x.ModifiedBy=currentUser.UserId;
        AddEvent(operationId,"REFUND_VOIDED","Devolución anulada",$"Devolución {id}. Motivo: {request.Reason.Trim()}");await db.SaveChangesAsync(ct);await RefreshStatusAsync(operationId,ct);
    }

    private async Task RefreshStatusAsync(long operationId,CancellationToken ct)
    {
        var s=await GetSummaryAsync(operationId,ct);string status;
        if(s.TotalClientFunds==0&&s.TotalExpenses==0&&s.TotalCollections==0&&s.TotalServices==0) status=FinancialStatuses.NoMovements;
        else if(s.TotalClientFunds>0&&s.TotalExpenses==0&&s.TotalCollections==0) status=FinancialStatuses.Funded;
        else if(await db.IssuedLiquidations.AsNoTracking().AnyAsync(x=>x.CompanyId==currentCompany.CompanyId&&x.OperationId==operationId,ct))
        {
            var balances=await LiquidationBalancesAsync(operationId,ct);
            status=balances.Values.All(v=>v<=0.01m)?FinancialStatuses.Closed:FinancialStatuses.PendingCollection;
        }
        else if(await db.OperationSettlements.AsNoTracking().AnyAsync(x=>x.CompanyId==currentCompany.CompanyId&&x.OperationId==operationId&&x.SettlementStatus=="APPROVED",ct))
            status=FinancialStatuses.PendingCollection;
        else if(s.PendingServiceCollection>0) status=FinancialStatuses.PendingCollection;
        else if(s.ManagedFundBalance>0.01m) status=FinancialStatuses.PendingRefund;
        else status=FinancialStatuses.InExecution;
        await operationService.SetFinancialStatusAsync(operationId,status,$"Recalculado por Finance: fondos={s.TotalClientFunds:N2}, gastos={s.TotalExpenses:N2}, cobros pendientes={s.PendingServiceCollection:N2}.",ct);
    }

    // El snapshot emitido es inmutable. Solo los cobros LIQUIDATION posteriores lo amortizan.
    // Los cobros SERVICE previos ya fueron descontados al emitir y no deben descontarse otra vez.
    private async Task<Dictionary<string,decimal>> LiquidationBalancesAsync(long operationId,CancellationToken ct)
    {
        var issued=await db.IssuedLiquidations.AsNoTracking().SingleOrDefaultAsync(x=>x.CompanyId==currentCompany.CompanyId&&x.OperationId==operationId,ct)
            ??throw new BusinessException(ErrorCodes.Validation,"Debe emitir la liquidación antes de registrar su cobranza.",409);
        using var doc=JsonDocument.Parse(issued.SnapshotJson);
        var result=new Dictionary<string,decimal>(StringComparer.OrdinalIgnoreCase);
        if(!doc.RootElement.TryGetProperty("CurrencySections",out var sections)||sections.ValueKind!=JsonValueKind.Array)
            throw new BusinessException(ErrorCodes.Validation,"La liquidación emitida no contiene saldos por moneda.",409);
        foreach(var section in sections.EnumerateArray())
        {
            var code=section.GetProperty("CurrencyCode").GetString()??"";
            var original=section.GetProperty("Balance").GetDecimal();
            var paid=await db.ClientCollections.AsNoTracking().Where(x=>x.OperationId==operationId&&x.CompanyId==currentCompany.CompanyId&&!x.IsVoid&&x.CollectionStatus!="VOID"&&x.CollectionType=="LIQUIDATION"&&x.CurrencyCode==code&&x.CreatedAt>=issued.IssuedAt).SumAsync(x=>(decimal?)x.Amount,ct)??0m;
            result[code]=Math.Max(0m,-original-paid);
        }
        return result;
    }

    private async Task<ImportOperation> GetOperationAsync(long id,CancellationToken ct)
    {
        if(currentUser.IsClientUser)
            throw new BusinessException(ErrorCodes.Forbidden,"La información financiera operativa es de uso interno.",403);

        return await db.ImportOperations.SingleOrDefaultAsync(x=>
            x.OperationId==id&&
            x.CompanyId==currentCompany.CompanyId,ct)
            ??throw new BusinessException(ErrorCodes.NotFound,"Operación no encontrada.",404);
    }
    private async Task ValidateCurrencyAsync(string code,CancellationToken ct){var c=code.Trim().ToUpperInvariant();if(!await db.Currencies.AsNoTracking().AnyAsync(x=>x.CurrencyCode==c&&x.IsActive,ct))throw new BusinessException(ErrorCodes.Validation,"Moneda inválida.",400);}
    private static void ValidateMoney(decimal amount,decimal? ex){if(amount<=0)throw new BusinessException(ErrorCodes.Validation,"Monto debe ser > 0.",400);if(ex.HasValue&&ex.Value<=0)throw new BusinessException(ErrorCodes.Validation,"Tipo de cambio debe ser > 0.",400);}
    private static decimal Base(decimal amount,decimal? ex)=>decimal.Round(amount*(ex??1),4,MidpointRounding.AwayFromZero);
    private void AddEvent(long op,string type,string title,string description)=>db.OperationEvents.Add(new(){CompanyId=currentCompany.CompanyId,OperationId=op,EventType=type,EventDate=DateTime.UtcNow,Title=title,Description=description,SourceType=OperationEventSources.System,CreatedAt=DateTime.UtcNow,CreatedBy=currentUser.UserId});
    private static string? Clean(string? x)=>string.IsNullOrWhiteSpace(x)?null:x.Trim();
    private static BudgetDto MapBudget(OperationBudget x)=>new(x.OperationBudgetId,x.VersionNo,x.BudgetStatus,x.BaseCurrencyCode,x.TotalEstimatedCost,x.TotalEstimatedService,x.TotalEstimatedAmount,ConcurrencyToken.Encode(x.RowVer));
    private static FundDto MapFund(ClientFund x)=>new(x.ClientFundId,x.FundStatus,x.RequestedAmount,x.ReceivedAmount,x.CurrencyCode,x.BaseAmount,x.ReferenceNo,ConcurrencyToken.Encode(x.RowVer),x.ExchangeRate,x.PaymentMethodId,x.Notes);
    private static ExpenseDto MapExpense(OperationExpense x,decimal paid)=>new(x.OperationExpenseId,x.ExpenseStatus,x.Description,x.Amount,x.CurrencyCode,x.BaseAmount,x.IsRebillable,paid,ConcurrencyToken.Encode(x.RowVer),x.ExpenseCategoryId,x.SupplierId,x.DocumentReference,x.ExpenseDate,x.DueDate,x.ExchangeRate,x.RebillAmount,x.Notes,x.PaidBy??"UNREVIEWED");
    private static ServiceDto MapService(OperationServiceCharge x,decimal col)=>new(x.OperationServiceId,x.ServiceStatus,x.Description,x.Amount,x.CurrencyCode,x.BaseAmount,col,ConcurrencyToken.Encode(x.RowVer),x.ServiceCategoryId,x.ExchangeRate,x.Notes);
    private static CollectionDto MapCollection(ClientCollection x)=>new(x.ClientCollectionId,x.CollectionType,x.CollectionStatus,x.Amount,x.CurrencyCode,x.BaseAmount,x.ReferenceNo,ConcurrencyToken.Encode(x.RowVer),x.CollectionDate,x.ExchangeRate,x.PaymentMethodId,x.Notes);
    private static RefundDto MapRefund(ClientRefund x)=>new(x.ClientRefundId,x.RefundStatus,x.Amount,x.CurrencyCode,x.BaseAmount,x.ReferenceNo,ConcurrencyToken.Encode(x.RowVer),x.ExchangeRate,x.PaymentMethodId,x.Reason,x.Notes);
    private static SettlementDto MapSettlement(OperationSettlement x)=>new(x.OperationSettlementId,x.SettlementStatus,x.BaseCurrencyCode,x.TotalClientFunds,x.TotalExpenses,x.TotalCompanyServices,x.TotalCollections,x.TotalRefunds,x.TotalAdjustments,x.ClientBalance,ConcurrencyToken.Encode(x.RowVer));
}
