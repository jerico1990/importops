using ImportOps.Api.Security;
using ImportOps.Application.Security;
using ImportOps.Application.Quotations;
using Microsoft.AspNetCore.Mvc;

namespace ImportOps.Api.Controllers;

[ApiController]
[RequirePermission(Permissions.QuotationsView)]
[Route("api/quotations")]
public sealed class QuotationsController(IQuotationService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] QuotationQuery query, CancellationToken ct)
        => Ok(await service.GetAsync(query, ct));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await service.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("by-request/{requestId:long}")]
    public async Task<IActionResult> GetByRequest(long requestId, CancellationToken ct)
        => Ok(await service.GetByRequestAsync(requestId, ct));

    [HttpPost("from-request/{requestId:long}")]
    [RequirePermission(Permissions.QuotationsManage)]
    public async Task<IActionResult> CreateFromRequest(long requestId, [FromBody] CreateQuotationRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(requestId, request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.QuotationId }, result);
    }

    [HttpPut("{id:long}")]
    [RequirePermission(Permissions.QuotationsManage)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateQuotationRequest request, CancellationToken ct)
    {
        var result = await service.UpdateAsync(id, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:long}")]
    [RequirePermission(Permissions.QuotationsManage)]
    public async Task<IActionResult> DeleteDraft(long id, CancellationToken ct)
        => await service.DeleteDraftAsync(id, ct) ? NoContent() : NotFound();

    [HttpPost("{id:long}/items")]
    [RequirePermission(Permissions.QuotationsManage)]
    public async Task<IActionResult> AddItem(long id, [FromBody] CreateQuotationItemRequest request, CancellationToken ct)
    {
        var result = await service.AddItemAsync(id, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{id:long}/items/{itemId:long}")]
    [RequirePermission(Permissions.QuotationsManage)]
    public async Task<IActionResult> UpdateItem(long id, long itemId, [FromBody] UpdateQuotationItemRequest request, CancellationToken ct)
    {
        var result = await service.UpdateItemAsync(id, itemId, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:long}/items/{itemId:long}")]
    [RequirePermission(Permissions.QuotationsManage)]
    public async Task<IActionResult> DeleteItem(long id, long itemId, CancellationToken ct)
        => await service.DeleteItemAsync(id, itemId, ct) ? NoContent() : NotFound();

    [HttpPost("{id:long}/send")]
    [RequirePermission(Permissions.QuotationsManage)]
    public async Task<IActionResult> Send(long id, [FromBody] QuotationActionRequest? request, CancellationToken ct)
    {
        var result = await service.SendAsync(id, request?.Comment, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:long}/approve")]
    [RequirePermission(Permissions.QuotationsApprove)]
    public async Task<IActionResult> Approve(long id, [FromBody] ApproveQuotationRequest request, CancellationToken ct)
    {
        var result = await service.ApproveAsync(id, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:long}/reject")]
    [RequirePermission(Permissions.QuotationsApprove)]
    public async Task<IActionResult> Reject(long id, [FromBody] RejectQuotationRequest request, CancellationToken ct)
    {
        var result = await service.RejectAsync(id, request.Reason, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:long}/cancel")]
    [RequirePermission(Permissions.QuotationsManage)]
    public async Task<IActionResult> Cancel(long id, [FromBody] RejectQuotationRequest request, CancellationToken ct)
    {
        var result = await service.CancelAsync(id, request.Reason, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:long}/expire")]
    [RequirePermission(Permissions.QuotationsManage)]
    public async Task<IActionResult> Expire(long id, [FromBody] QuotationActionRequest? request, CancellationToken ct)
    {
        var result = await service.ExpireAsync(id, request?.Comment, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:long}/new-version")]
    [RequirePermission(Permissions.QuotationsManage)]
    public async Task<IActionResult> NewVersion(long id, [FromBody] NewQuotationVersionRequest request, CancellationToken ct)
    {
        var result = await service.CreateNewVersionAsync(id, request, ct);
        return result is null
            ? NotFound()
            : CreatedAtAction(nameof(GetById), new { id = result.QuotationId }, result);
    }

}
