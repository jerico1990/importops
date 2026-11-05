using ImportOps.Api.Security;
using ImportOps.Application.Customs;
using ImportOps.Application.Security;
using Microsoft.AspNetCore.Mvc;

namespace ImportOps.Api.Controllers;

[ApiController]
[Route("api/customs")]
[RequirePermission(Permissions.CustomsView)]
public sealed class CustomsController(ICustomsService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] CustomsQuery query, CancellationToken ct)
        => Ok(await service.GetAsync(query, ct));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await service.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("by-operation/{operationId:long}")]
    public async Task<IActionResult> ByOperation(long operationId, CancellationToken ct)
        => Ok(await service.GetByOperationAsync(operationId, ct));

    [HttpPost("from-operation/{operationId:long}")]
    [RequirePermission(Permissions.CustomsManage)]
    public async Task<IActionResult> Create(long operationId, [FromBody] CreateCustomsProcessRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(operationId, request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.CustomsProcessId }, result);
    }

    [HttpPut("{id:long}")]
    [RequirePermission(Permissions.CustomsManage)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateCustomsProcessRequest request, CancellationToken ct)
    {
        var result = await service.UpdateAsync(id, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:long}/start-review")]
    [RequirePermission(Permissions.CustomsManage)]
    public async Task<IActionResult> StartReview(long id, [FromBody] CustomsActionRequest request, CancellationToken ct)
    {
        var result = await service.StartReviewAsync(id, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:long}/channel")]
    [RequirePermission(Permissions.CustomsManage)]
    public async Task<IActionResult> AssignChannel(long id, [FromBody] AssignCustomsChannelRequest request, CancellationToken ct)
    {
        var result = await service.AssignChannelAsync(id, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:long}/observations")]
    [RequirePermission(Permissions.CustomsManage)]
    public async Task<IActionResult> AddObservation(long id, [FromBody] CreateCustomsObservationRequest request, CancellationToken ct)
    {
        var result = await service.AddObservationAsync(id, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:long}/observations/{observationId:long}/resolve")]
    [RequirePermission(Permissions.CustomsManage)]
    public async Task<IActionResult> ResolveObservation(long id, long observationId, [FromBody] ResolveCustomsObservationRequest request, CancellationToken ct)
    {
        var result = await service.ResolveObservationAsync(id, observationId, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:long}/taxes")]
    [RequirePermission(Permissions.CustomsManage)]
    public async Task<IActionResult> AddTax(long id, [FromBody] UpsertCustomsTaxRequest request, CancellationToken ct)
    {
        var result = await service.AddTaxAsync(id, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{id:long}/taxes/{taxId:long}")]
    [RequirePermission(Permissions.CustomsManage)]
    public async Task<IActionResult> UpdateTax(long id, long taxId, [FromBody] UpsertCustomsTaxRequest request, CancellationToken ct)
    {
        var result = await service.UpdateTaxAsync(id, taxId, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:long}/taxes/{taxId:long}/status")]
    [RequirePermission(Permissions.CustomsManage)]
    public async Task<IActionResult> ChangeTaxStatus(long id, long taxId, [FromBody] CustomsTaxStatusRequest request, CancellationToken ct)
    {
        var result = await service.ChangeTaxStatusAsync(id, taxId, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:long}/release")]
    [RequirePermission(Permissions.CustomsRelease)]
    public async Task<IActionResult> Release(long id, [FromBody] ReleaseCustomsRequest request, CancellationToken ct)
    {
        var result = await service.ReleaseAsync(id, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:long}/cancel")]
    [RequirePermission(Permissions.CustomsManage)]
    public async Task<IActionResult> Cancel(long id, [FromBody] CancelCustomsRequest request, CancellationToken ct)
    {
        var result = await service.CancelAsync(id, request, ct);
        return result is null ? NotFound() : Ok(result);
    }
}
