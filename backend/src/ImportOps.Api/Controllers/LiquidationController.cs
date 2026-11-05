using ImportOps.Api.Security;
using ImportOps.Application.Finance;
using ImportOps.Application.Security;
using Microsoft.AspNetCore.Mvc;

namespace ImportOps.Api.Controllers;

[ApiController]
[Route("api/operations/{operationId:long}/liquidation")]
[RequirePermission(Permissions.OperationsView)]
public sealed class LiquidationController(ILiquidationService service) : ControllerBase
{
    [HttpGet("financing")]
    [RequirePermission(Permissions.FinanceView)]
    public async Task<IActionResult> Financing(long operationId, CancellationToken ct)
        => Ok(await service.TermsAsync(operationId, ct));

    [HttpPut("financing")]
    [RequirePermission(Permissions.FinanceManage)]
    public async Task<IActionResult> SetFinancing(long operationId, [FromBody] SetFinancingRequest request, CancellationToken ct)
        => Ok(await service.SetTermsAsync(operationId, request.FinancingDays, ct));

    [HttpGet("delivery")]
    [RequirePermission(Permissions.FinanceView)]
    public async Task<IActionResult> Delivery(long operationId, CancellationToken ct)
        => await service.DeliveryAsync(operationId, ct) is { } data ? Ok(data) : NotFound();

    [HttpPost("send")]
    [RequirePermission(Permissions.FinanceApprove)]
    public async Task<IActionResult> Send(long operationId, [FromBody] SendLiquidationRequest request, CancellationToken ct)
        => Ok(await service.SendAsync(operationId, request, ct));

    [HttpGet("issued")]
    public async Task<IActionResult> Issued(long operationId, CancellationToken ct)
        => await service.IssuedAsync(operationId, ct) is { } data ? Ok(data) : NotFound();

    [HttpGet("pdf")]
    public async Task<IActionResult> Pdf(long operationId, CancellationToken ct)
        => await service.PdfAsync(operationId, ct) is { } pdf ? File(pdf.Content, "application/pdf", pdf.FileName) : NotFound();

    [HttpGet("preview")]
    [RequirePermission(Permissions.FinanceView)]
    public async Task<IActionResult> Preview(long operationId, CancellationToken ct)
        => Ok(await service.PreviewAsync(operationId, ct));

    [HttpPost("issue")]
    [RequirePermission(Permissions.FinanceApprove)]
    public async Task<IActionResult> Issue(long operationId, CancellationToken ct)
        => Ok(await service.IssueAsync(operationId, ct));
}

public sealed record SetFinancingRequest(int FinancingDays);
