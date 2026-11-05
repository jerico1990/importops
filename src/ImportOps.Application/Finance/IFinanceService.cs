namespace ImportOps.Application.Finance;

public interface IFinanceService
{
    Task<FinancialSummaryDto> GetSummaryAsync(long operationId,CancellationToken ct);
    Task<BudgetDto?> GetBudgetAsync(long operationId,CancellationToken ct);
    Task<SettlementDto?> GetSettlementAsync(long operationId,CancellationToken ct);
    Task<BudgetDto> EnsureBudgetFromQuotationAsync(long operationId,CancellationToken ct);
    Task<IReadOnlyCollection<FundDto>> GetFundsAsync(long operationId,CancellationToken ct);
    Task<FundDto> RegisterFundAsync(long operationId,RegisterFundRequest request,CancellationToken ct);
    Task<IReadOnlyCollection<ExpenseDto>> GetExpensesAsync(long operationId,CancellationToken ct);
    Task<ExpenseDto> RegisterExpenseAsync(long operationId,RegisterExpenseRequest request,CancellationToken ct);
    Task<IReadOnlyCollection<ExpensePaymentDto>> GetExpensePaymentsAsync(long operationId,long expenseId,CancellationToken ct);
    Task<ExpenseDto> VoidExpensePaymentAsync(long operationId,long expenseId,long paymentId,VoidExpensePaymentRequest request,CancellationToken ct);
    Task<ExpenseDto> PayExpenseAsync(long operationId,long expenseId,PayExpenseRequest request,CancellationToken ct);
    Task<IReadOnlyCollection<ServiceDto>> GetServicesAsync(long operationId,CancellationToken ct);
    Task<ServiceDto> AddServiceAsync(long operationId,SaveServiceRequest request,CancellationToken ct);
    Task<ServiceDto> UpdateServiceAsync(long operationId,long id,EditServiceRequest request,CancellationToken ct);
    Task VoidServiceAsync(long operationId,long id,VoidFinanceRequest request,CancellationToken ct);
    Task<IReadOnlyCollection<CollectionDto>> GetCollectionsAsync(long operationId,CancellationToken ct);
    Task<CollectionDto> RegisterCollectionAsync(long operationId,RegisterCollectionRequest request,CancellationToken ct);
    Task AllocateCollectionAsync(long operationId,long collectionId,AllocateCollectionRequest request,CancellationToken ct);
    Task RegisterAdjustmentAsync(long operationId,RegisterAdjustmentRequest request,CancellationToken ct);
    Task<IReadOnlyCollection<RefundDto>> GetRefundsAsync(long operationId,CancellationToken ct);
    Task<RefundDto> RegisterRefundAsync(long operationId,RegisterRefundRequest request,CancellationToken ct);
    Task<FundDto> UpdateFundAsync(long operationId,long id,EditFundRequest request,CancellationToken ct);
    Task VoidFundAsync(long operationId,long id,VoidFinanceRequest request,CancellationToken ct);
    Task<ExpenseDto> UpdateExpenseAsync(long operationId,long id,EditExpenseRequest request,CancellationToken ct);
    Task VoidExpenseAsync(long operationId,long id,VoidFinanceRequest request,CancellationToken ct);
    Task<CollectionDto> UpdateCollectionAsync(long operationId,long id,EditCollectionRequest request,CancellationToken ct);
    Task VoidCollectionAsync(long operationId,long id,VoidFinanceRequest request,CancellationToken ct);
    Task<RefundDto> UpdateRefundAsync(long operationId,long id,EditRefundRequest request,CancellationToken ct);
    Task VoidRefundAsync(long operationId,long id,VoidFinanceRequest request,CancellationToken ct);
    Task<SettlementDto> PrepareSettlementAsync(long operationId,CancellationToken ct);
    Task<SettlementDto> ApproveSettlementAsync(long operationId,ApproveSettlementRequest request,CancellationToken ct);
}
