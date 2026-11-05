using ImportOps.Api.Security;
using ImportOps.Application.Security;
using ImportOps.Application.Shipments;
using Microsoft.AspNetCore.Mvc;

namespace ImportOps.Api.Controllers;

[ApiController]
[Route("api/shipments")]
[RequirePermission(Permissions.ShipmentsView)]
public sealed class ShipmentsController(IShipmentService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] ShipmentQuery query, CancellationToken ct)
        => Ok(await service.GetAsync(query, ct));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await service.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("by-operation/{operationId:long}")]
    public async Task<IActionResult> GetByOperation(long operationId, CancellationToken ct)
        => Ok(await service.GetByOperationAsync(operationId, ct));

    [HttpGet("{id:long}/available-actions")]
    [RequirePermission(Permissions.ShipmentsManage)]
    public async Task<IActionResult> AvailableActions(long id, CancellationToken ct)
        => Ok(await service.GetAvailableActionsAsync(id, ct));

    [HttpPost("from-operation/{operationId:long}")]
    [RequirePermission(Permissions.ShipmentsManage)]
    public async Task<IActionResult> Create(long operationId, [FromBody] CreateShipmentRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(operationId, request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.ShipmentId }, result);
    }

    [HttpPut("{id:long}")]
    [RequirePermission(Permissions.ShipmentsManage)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateShipmentRequest request, CancellationToken ct)
    {
        var result = await service.UpdateAsync(id, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:long}")]
    [RequirePermission(Permissions.ShipmentsManage)]
    public async Task<IActionResult> Delete(long id, [FromQuery] string rowVersion, CancellationToken ct)
        => await service.DeletePlannedAsync(id, rowVersion, ct) ? NoContent() : NotFound();

    [HttpPost("{id:long}/containers")]
    [RequirePermission(Permissions.ShipmentsManage)]
    public async Task<IActionResult> AddContainer(long id, [FromBody] UpsertShipmentContainerRequest request, CancellationToken ct)
    {
        var result = await service.AddContainerAsync(id, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{id:long}/containers/{containerId:long}")]
    [RequirePermission(Permissions.ShipmentsManage)]
    public async Task<IActionResult> UpdateContainer(long id, long containerId, [FromBody] UpsertShipmentContainerRequest request, CancellationToken ct)
    {
        var result = await service.UpdateContainerAsync(id, containerId, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:long}/containers/{containerId:long}")]
    [RequirePermission(Permissions.ShipmentsManage)]
    public async Task<IActionResult> DeleteContainer(long id, long containerId, CancellationToken ct)
        => await service.DeleteContainerAsync(id, containerId, ct) ? NoContent() : NotFound();

    [HttpPost("{id:long}/book")]
    [RequirePermission(Permissions.ShipmentsManage)]
    public async Task<IActionResult> Book(long id, [FromBody] ShipmentStatusActionRequest? request, CancellationToken ct)
    {
        var result = await service.BookAsync(id, request?.Comment, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:long}/depart")]
    [RequirePermission(Permissions.ShipmentsManage)]
    public async Task<IActionResult> Depart(long id, [FromBody] ShipmentStatusActionRequest request, CancellationToken ct)
    {
        if (!request.EventAt.HasValue)
            return BadRequest(new { code = "VALIDATION_ERROR", message = "eventAt es obligatorio." });

        var result = await service.DepartAsync(id, request.EventAt.Value, request.Comment, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:long}/in-transit")]
    [RequirePermission(Permissions.ShipmentsManage)]
    public async Task<IActionResult> InTransit(long id, [FromBody] ShipmentStatusActionRequest? request, CancellationToken ct)
    {
        var result = await service.MarkInTransitAsync(id, request?.Comment, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:long}/arrive")]
    [RequirePermission(Permissions.ShipmentsManage)]
    public async Task<IActionResult> Arrive(long id, [FromBody] ShipmentStatusActionRequest request, CancellationToken ct)
    {
        if (!request.EventAt.HasValue)
            return BadRequest(new { code = "VALIDATION_ERROR", message = "eventAt es obligatorio." });

        var result = await service.ArriveAsync(id, request.EventAt.Value, request.Comment, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:long}/cancel")]
    [RequirePermission(Permissions.ShipmentsManage)]
    public async Task<IActionResult> Cancel(long id, [FromBody] CancelShipmentRequest request, CancellationToken ct)
    {
        var result = await service.CancelAsync(id, request.Reason, ct);
        return result is null ? NotFound() : Ok(result);
    }
}
