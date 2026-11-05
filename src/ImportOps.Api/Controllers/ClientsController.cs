using ImportOps.Api.Security;
using ImportOps.Application.Security;
using ImportOps.Application.Clients;
using Microsoft.AspNetCore.Mvc;

namespace ImportOps.Api.Controllers;

[ApiController]
[RequirePermission(Permissions.ClientsView)]
[Route("api/clients")]
public sealed class ClientsController(IClientService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await service.GetAsync(search, page, pageSize, ct));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await service.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [RequirePermission(Permissions.ClientsManage)]
    public async Task<IActionResult> Create([FromBody] CreateClientRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.ClientId }, result);
    }

    [HttpPut("{id:long}")]
    [RequirePermission(Permissions.ClientsManage)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateClientRequest request, CancellationToken ct)
    {
        var result = await service.UpdateAsync(id, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:long}")]
    [RequirePermission(Permissions.ClientsManage)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
        => await service.DeleteAsync(id, ct) ? NoContent() : NotFound();

    [HttpPost("{id:long}/contacts")]
    [RequirePermission(Permissions.ClientsManage)]
    public async Task<IActionResult> AddContact(long id, [FromBody] CreateClientContactRequest request, CancellationToken ct)
    {
        var result = await service.AddContactAsync(id, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{id:long}/contacts/{contactId:long}")]
    [RequirePermission(Permissions.ClientsManage)]
    public async Task<IActionResult> UpdateContact(long id, long contactId, [FromBody] UpdateClientContactRequest request, CancellationToken ct)
    {
        var result = await service.UpdateContactAsync(id, contactId, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:long}/contacts/{contactId:long}")]
    [RequirePermission(Permissions.ClientsManage)]
    public async Task<IActionResult> DeleteContact(long id, long contactId, CancellationToken ct)
        => await service.DeleteContactAsync(id, contactId, ct) ? NoContent() : NotFound();

    [HttpPost("{id:long}/addresses")]
    [RequirePermission(Permissions.ClientsManage)]
    public async Task<IActionResult> AddAddress(long id, [FromBody] CreateClientAddressRequest request, CancellationToken ct)
    {
        var result = await service.AddAddressAsync(id, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{id:long}/addresses/{addressId:long}")]
    [RequirePermission(Permissions.ClientsManage)]
    public async Task<IActionResult> UpdateAddress(long id, long addressId, [FromBody] UpdateClientAddressRequest request, CancellationToken ct)
    {
        var result = await service.UpdateAddressAsync(id, addressId, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:long}/addresses/{addressId:long}")]
    [RequirePermission(Permissions.ClientsManage)]
    public async Task<IActionResult> DeleteAddress(long id, long addressId, CancellationToken ct)
        => await service.DeleteAddressAsync(id, addressId, ct) ? NoContent() : NotFound();
}
