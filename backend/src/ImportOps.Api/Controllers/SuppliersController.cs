using ImportOps.Api.Security;
using ImportOps.Application.Security;
using ImportOps.Application.Suppliers;
using Microsoft.AspNetCore.Mvc;

namespace ImportOps.Api.Controllers;

[ApiController]
[RequirePermission(Permissions.SuppliersView)]
[Route("api/suppliers")]
public sealed class SuppliersController(ISupplierService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? search = null, [FromQuery] bool? isActive = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await service.GetAsync(search, isActive, page, pageSize, ct));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await service.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [RequirePermission(Permissions.SuppliersManage)]
    public async Task<IActionResult> Create([FromBody] CreateSupplierRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.SupplierId }, result);
    }

    [HttpPut("{id:long}")]
    [RequirePermission(Permissions.SuppliersManage)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateSupplierRequest request, CancellationToken ct)
    {
        var result = await service.UpdateAsync(id, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:long}")]
    [RequirePermission(Permissions.SuppliersManage)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
        => await service.DeleteAsync(id, ct) ? NoContent() : NotFound();

    [HttpPost("{id:long}/contacts")]
    [RequirePermission(Permissions.SuppliersManage)]
    public async Task<IActionResult> AddContact(long id, [FromBody] UpsertSupplierContactRequest request, CancellationToken ct)
    {
        var result = await service.AddContactAsync(id, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{id:long}/contacts/{contactId:long}")]
    [RequirePermission(Permissions.SuppliersManage)]
    public async Task<IActionResult> UpdateContact(long id, long contactId, [FromBody] UpsertSupplierContactRequest request, CancellationToken ct)
    {
        var result = await service.UpdateContactAsync(id, contactId, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:long}/contacts/{contactId:long}")]
    [RequirePermission(Permissions.SuppliersManage)]
    public async Task<IActionResult> DeleteContact(long id, long contactId, CancellationToken ct)
        => await service.DeleteContactAsync(id, contactId, ct) ? NoContent() : NotFound();
}
