using ImportOps.Api.Security;
using ImportOps.Application.Security;
using ImportOps.Application.ImportRequests;
using Microsoft.AspNetCore.Mvc;

namespace ImportOps.Api.Controllers;

[ApiController]
[RequirePermission(Permissions.RequestsView)]
[Route("api/import-requests")]
public sealed class ImportRequestsController(IImportRequestService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] ImportRequestQuery query, CancellationToken ct)
        => Ok(await service.GetAsync(query, ct));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await service.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [RequirePermission(Permissions.RequestsCreate)]
    public async Task<IActionResult> Create([FromBody] CreateImportRequestRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.RequestId }, result);
    }

    [HttpPut("{id:long}")]
    [RequirePermission(Permissions.RequestsCreate)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateImportRequestRequest request, CancellationToken ct)
    {
        var result = await service.UpdateAsync(id, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:long}")]
    [RequirePermission(Permissions.RequestsCreate)]
    public async Task<IActionResult> DeleteDraft(long id, CancellationToken ct)
        => await service.DeleteDraftAsync(id, ct) ? NoContent() : NotFound();

    [HttpPost("{id:long}/submit-review")]
    [RequirePermission(Permissions.RequestsCreate)]
    public async Task<IActionResult> SubmitForReview(long id, [FromBody] ChangeRequestStatusRequest? request, CancellationToken ct)
    {
        var result = await service.SubmitForReviewAsync(id, request?.Comment, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:long}/observe")]
    [RequirePermission(Permissions.RequestsManage)]
    public async Task<IActionResult> Observe(long id, [FromBody] ChangeRequestStatusRequest request, CancellationToken ct)
    {
        var result = await service.ObserveAsync(id, request.Comment, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:long}/reject")]
    [RequirePermission(Permissions.RequestsManage)]
    public async Task<IActionResult> Reject(long id, [FromBody] ChangeRequestStatusRequest request, CancellationToken ct)
    {
        var result = await service.RejectAsync(id, request.Comment, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:long}/cancel")]
    [RequirePermission(Permissions.RequestsManage)]
    public async Task<IActionResult> Cancel(long id, [FromBody] ChangeRequestStatusRequest request, CancellationToken ct)
    {
        var result = await service.CancelAsync(id, request.Comment, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:long}/assign")]
    [RequirePermission(Permissions.RequestsManage)]
    public async Task<IActionResult> Assign(long id, [FromBody] AssignImportRequestRequest request, CancellationToken ct)
    {
        var result = await service.AssignAsync(id, request.AssignedUserId, request.Comment, ct);
        return result is null ? NotFound() : Ok(result);
    }
}
