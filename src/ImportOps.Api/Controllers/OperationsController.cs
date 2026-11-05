using ImportOps.Api.Security;
using ImportOps.Application.Security;
using ImportOps.Application.Operations;
using Microsoft.AspNetCore.Mvc;

namespace ImportOps.Api.Controllers;

[ApiController]
[RequirePermission(Permissions.OperationsView)]
[Route("api/operations")]
public sealed class OperationsController(IImportOperationService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] OperationQuery query, CancellationToken ct)
        => Ok(await service.GetAsync(query, ct));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await service.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("from-quotation/{quotationId:long}")]
    [RequirePermission(Permissions.OperationsManage)]
    public async Task<IActionResult> CreateFromQuotation(long quotationId, [FromBody] CreateOperationFromQuotationRequest request, CancellationToken ct)
    {
        var result = await service.CreateFromQuotationAsync(quotationId, request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.OperationId }, result);
    }

    [HttpPut("{id:long}")]
    [RequirePermission(Permissions.OperationsManage)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateOperationRequest request, CancellationToken ct)
    {
        var result = await service.UpdateAsync(id, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:long}/assign")]
    [RequirePermission(Permissions.OperationsManage)]
    public async Task<IActionResult> Assign(long id, [FromBody] AssignOperationRequest request, CancellationToken ct)
    {
        var result = await service.AssignAsync(id, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:long}/change-stage")]
    [RequirePermission(Permissions.OperationsManage)]
    public async Task<IActionResult> ChangeStage(long id, [FromBody] ChangeStageRequest request, CancellationToken ct)
    {
        var result = await service.ChangeStageAsync(id, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{id:long}/available-stages")]
    [RequirePermission(Permissions.OperationsManage)]
    public async Task<IActionResult> AvailableStages(long id, CancellationToken ct)
        => Ok(await service.GetAvailableStagesAsync(id, ct));

    [HttpGet("{id:long}/stage-history")]
    public async Task<IActionResult> StageHistory(long id, CancellationToken ct)
        => Ok(await service.GetStageHistoryAsync(id, ct));

    [HttpGet("{id:long}/status-history")]
    public async Task<IActionResult> StatusHistory(long id, CancellationToken ct)
        => Ok(await service.GetStatusHistoryAsync(id, ct));

    [HttpPost("{id:long}/hold")]
    [RequirePermission(Permissions.OperationsManage)]
    public async Task<IActionResult> Hold(long id, [FromBody] OperationActionRequest request, CancellationToken ct)
    {
        var result = await service.HoldAsync(id, request.Comment, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:long}/resume")]
    [RequirePermission(Permissions.OperationsManage)]
    public async Task<IActionResult> Resume(long id, [FromBody] OperationActionRequest request, CancellationToken ct)
    {
        var result = await service.ResumeAsync(id, request.Comment, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:long}/cancel")]
    [RequirePermission(Permissions.OperationsManage)]
    public async Task<IActionResult> Cancel(long id, [FromBody] OperationActionRequest request, CancellationToken ct)
    {
        var result = await service.CancelAsync(id, request.Comment, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:long}/close")]
    [RequirePermission(Permissions.OperationsClose)]
    public async Task<IActionResult> Close(long id, [FromBody] OperationActionRequest request, CancellationToken ct)
    {
        var result = await service.CloseAsync(id, request.Comment, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{id:long}/tasks")]
    public async Task<IActionResult> Tasks(long id, [FromQuery] string? status, [FromQuery] int? stageId, CancellationToken ct)
        => Ok(await service.GetTasksAsync(id, status, stageId, ct));

    [HttpPost("{id:long}/tasks")]
    [RequirePermission(Permissions.OperationsManage)]
    public async Task<IActionResult> CreateTask(long id, [FromBody] CreateOperationTaskRequest request, CancellationToken ct)
    {
        var result = await service.CreateTaskAsync(id, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{id:long}/tasks/{taskId:long}")]
    [RequirePermission(Permissions.OperationsManage)]
    public async Task<IActionResult> UpdateTask(long id, long taskId, [FromBody] UpdateOperationTaskRequest request, CancellationToken ct)
    {
        var result = await service.UpdateTaskAsync(id, taskId, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:long}/tasks/{taskId:long}/status")]
    [RequirePermission(Permissions.OperationsManage)]
    public async Task<IActionResult> ChangeTaskStatus(long id, long taskId, [FromBody] ChangeTaskStatusRequest request, CancellationToken ct)
    {
        var result = await service.ChangeTaskStatusAsync(id, taskId, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{id:long}/events")]
    public async Task<IActionResult> Events(long id, CancellationToken ct)
        => Ok(await service.GetEventsAsync(id, ct));

    [HttpPost("{id:long}/events")]
    [RequirePermission(Permissions.OperationsManage)]
    public async Task<IActionResult> AddEvent(long id, [FromBody] CreateOperationEventRequest request, CancellationToken ct)
    {
        var result = await service.AddManualEventAsync(id, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{id:long}/notes")]
    public async Task<IActionResult> Notes(long id, [FromQuery] string? noteType, CancellationToken ct)
        => Ok(await service.GetNotesAsync(id, noteType, ct));

    [HttpPost("{id:long}/notes")]
    [RequirePermission(Permissions.OperationsManage)]
    public async Task<IActionResult> AddNote(long id, [FromBody] CreateOperationNoteRequest request, CancellationToken ct)
    {
        var result = await service.AddNoteAsync(id, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPatch("{id:long}/notes/{noteId:long}/pin")]
    [RequirePermission(Permissions.OperationsManage)]
    public async Task<IActionResult> PinNote(long id, long noteId, [FromQuery] bool pinned, CancellationToken ct)
    {
        var result = await service.SetNotePinnedAsync(id, noteId, pinned, ct);
        return result is null ? NotFound() : Ok(result);
    }
}
