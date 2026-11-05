using ImportOps.Api.Security;
using ImportOps.Application.Finance;
using ImportOps.Application.Security;
using Microsoft.AspNetCore.Mvc;

namespace ImportOps.Api.Controllers;

[ApiController]
[Route("api/finance/operations/{operationId:long}")]
[RequirePermission(Permissions.FinanceView)]
public sealed class FinanceController(IFinanceService service):ControllerBase
{
 [HttpGet("summary")] public async Task<IActionResult> Summary(long operationId,CancellationToken ct)=>Ok(await service.GetSummaryAsync(operationId,ct));
 [HttpGet("budget")] public async Task<IActionResult> GetBudget(long operationId,CancellationToken ct){var x=await service.GetBudgetAsync(operationId,ct);return x is null?NoContent():Ok(x);}
 [HttpGet("settlement")] public async Task<IActionResult> GetSettlement(long operationId,CancellationToken ct){var x=await service.GetSettlementAsync(operationId,ct);return x is null?NoContent():Ok(x);}
 [HttpPost("budget/ensure")] [RequirePermission(Permissions.FinanceManage)] public async Task<IActionResult> Budget(long operationId,CancellationToken ct)=>Ok(await service.EnsureBudgetFromQuotationAsync(operationId,ct));

 [HttpGet("funds")] public async Task<IActionResult> Funds(long operationId,CancellationToken ct)=>Ok(await service.GetFundsAsync(operationId,ct));
 [HttpPost("funds")] [RequirePermission(Permissions.FinanceManage)] public async Task<IActionResult> AddFund(long operationId,[FromBody]RegisterFundRequest r,CancellationToken ct)=>Ok(await service.RegisterFundAsync(operationId,r,ct));

 [HttpGet("expenses")] public async Task<IActionResult> Expenses(long operationId,CancellationToken ct)=>Ok(await service.GetExpensesAsync(operationId,ct));
 [HttpPost("expenses")] [RequirePermission(Permissions.FinanceManage)] public async Task<IActionResult> AddExpense(long operationId,[FromBody]RegisterExpenseRequest r,CancellationToken ct)=>Ok(await service.RegisterExpenseAsync(operationId,r,ct));
 [HttpPost("expenses/{expenseId:long}/payments")] [RequirePermission(Permissions.FinanceManage)] public async Task<IActionResult> PayExpense(long operationId,long expenseId,[FromBody]PayExpenseRequest r,CancellationToken ct)=>Ok(await service.PayExpenseAsync(operationId,expenseId,r,ct));

 [HttpGet("expenses/{expenseId:long}/payments")]
 [RequirePermission(Permissions.FinanceManage)]
 public async Task<IActionResult> ExpensePayments(long operationId,long expenseId,CancellationToken ct)=>Ok(await service.GetExpensePaymentsAsync(operationId,expenseId,ct));
 [HttpPost("expenses/{expenseId:long}/payments/{paymentId:long}/void")]
 [RequirePermission(Permissions.FinanceManage)]
 public async Task<IActionResult> VoidExpensePayment(long operationId,long expenseId,long paymentId,[FromBody]VoidExpensePaymentRequest r,CancellationToken ct)=>Ok(await service.VoidExpensePaymentAsync(operationId,expenseId,paymentId,r,ct));

 [HttpGet("services")] public async Task<IActionResult> Services(long operationId,CancellationToken ct)=>Ok(await service.GetServicesAsync(operationId,ct));
 [HttpPost("services")] [RequirePermission(Permissions.FinanceManage)] public async Task<IActionResult> AddService(long operationId,[FromBody]SaveServiceRequest r,CancellationToken ct)=>Ok(await service.AddServiceAsync(operationId,r,ct));
 [HttpPut("services/{id:long}")] [RequirePermission(Permissions.FinanceManage)] public async Task<IActionResult> EditService(long operationId,long id,[FromBody]EditServiceRequest r,CancellationToken ct)=>Ok(await service.UpdateServiceAsync(operationId,id,r,ct));
 [HttpPost("services/{id:long}/void")] [RequirePermission(Permissions.FinanceManage)] public async Task<IActionResult> VoidService(long operationId,long id,[FromBody]VoidFinanceRequest r,CancellationToken ct){await service.VoidServiceAsync(operationId,id,r,ct);return NoContent();}
 [HttpGet("collections")] public async Task<IActionResult> Collections(long operationId,CancellationToken ct)=>Ok(await service.GetCollectionsAsync(operationId,ct));
 [HttpPost("collections")] [RequirePermission(Permissions.FinanceManage)] public async Task<IActionResult> AddCollection(long operationId,[FromBody]RegisterCollectionRequest r,CancellationToken ct)=>Ok(await service.RegisterCollectionAsync(operationId,r,ct));
 [HttpPost("collections/{collectionId:long}/allocate")] [RequirePermission(Permissions.FinanceManage)] public async Task<IActionResult> Allocate(long operationId,long collectionId,[FromBody]AllocateCollectionRequest r,CancellationToken ct){await service.AllocateCollectionAsync(operationId,collectionId,r,ct);return NoContent();}

 [HttpPost("adjustments")] [RequirePermission(Permissions.FinanceManage)] public async Task<IActionResult> Adjustment(long operationId,[FromBody]RegisterAdjustmentRequest r,CancellationToken ct){await service.RegisterAdjustmentAsync(operationId,r,ct);return NoContent();}
 [HttpGet("refunds")] public async Task<IActionResult> Refunds(long operationId,CancellationToken ct)=>Ok(await service.GetRefundsAsync(operationId,ct));
 [HttpPost("refunds")] [RequirePermission(Permissions.FinanceManage)] public async Task<IActionResult> Refund(long operationId,[FromBody]RegisterRefundRequest r,CancellationToken ct)=>Ok(await service.RegisterRefundAsync(operationId,r,ct));

 [HttpPut("funds/{id:long}")] [RequirePermission(Permissions.FinanceManage)] public async Task<IActionResult> EditFund(long operationId,long id,[FromBody]EditFundRequest r,CancellationToken ct)=>Ok(await service.UpdateFundAsync(operationId,id,r,ct));
 [HttpPost("funds/{id:long}/void")] [RequirePermission(Permissions.FinanceManage)] public async Task<IActionResult> VoidFund(long operationId,long id,[FromBody]VoidFinanceRequest r,CancellationToken ct){await service.VoidFundAsync(operationId,id,r,ct);return NoContent();}
 [HttpPut("expenses/{id:long}")] [RequirePermission(Permissions.FinanceManage)] public async Task<IActionResult> EditExpense(long operationId,long id,[FromBody]EditExpenseRequest r,CancellationToken ct)=>Ok(await service.UpdateExpenseAsync(operationId,id,r,ct));
 [HttpPost("expenses/{id:long}/void")] [RequirePermission(Permissions.FinanceManage)] public async Task<IActionResult> VoidExpense(long operationId,long id,[FromBody]VoidFinanceRequest r,CancellationToken ct){await service.VoidExpenseAsync(operationId,id,r,ct);return NoContent();}
 [HttpPut("collections/{id:long}")] [RequirePermission(Permissions.FinanceManage)] public async Task<IActionResult> EditCollection(long operationId,long id,[FromBody]EditCollectionRequest r,CancellationToken ct)=>Ok(await service.UpdateCollectionAsync(operationId,id,r,ct));
 [HttpPost("collections/{id:long}/void")] [RequirePermission(Permissions.FinanceManage)] public async Task<IActionResult> VoidCollection(long operationId,long id,[FromBody]VoidFinanceRequest r,CancellationToken ct){await service.VoidCollectionAsync(operationId,id,r,ct);return NoContent();}
 [HttpPut("refunds/{id:long}")] [RequirePermission(Permissions.FinanceManage)] public async Task<IActionResult> EditRefund(long operationId,long id,[FromBody]EditRefundRequest r,CancellationToken ct)=>Ok(await service.UpdateRefundAsync(operationId,id,r,ct));
 [HttpPost("refunds/{id:long}/void")] [RequirePermission(Permissions.FinanceManage)] public async Task<IActionResult> VoidRefund(long operationId,long id,[FromBody]VoidFinanceRequest r,CancellationToken ct){await service.VoidRefundAsync(operationId,id,r,ct);return NoContent();}
 [HttpPost("settlement/prepare")] [RequirePermission(Permissions.FinanceManage)] public async Task<IActionResult> Prepare(long operationId,CancellationToken ct)=>Ok(await service.PrepareSettlementAsync(operationId,ct));
 [HttpPost("settlement/approve")] [RequirePermission(Permissions.FinanceApprove)] public async Task<IActionResult> Approve(long operationId,[FromBody]ApproveSettlementRequest r,CancellationToken ct)=>Ok(await service.ApproveSettlementAsync(operationId,r,ct));
}
