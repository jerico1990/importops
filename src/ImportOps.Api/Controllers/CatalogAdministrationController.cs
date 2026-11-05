using ImportOps.Api.Security;
using ImportOps.Application.Catalogs;
using ImportOps.Application.Security;
using Microsoft.AspNetCore.Mvc;

namespace ImportOps.Api.Controllers;

[ApiController]
[Route("api/configuration/catalogs")]
[RequirePermission(Permissions.ConfigurationView)]
public sealed class CatalogAdministrationController(ICatalogAdministrationService service) : ControllerBase
{
    [HttpGet("{catalog}")]
    public async Task<IActionResult> Search(string catalog,[FromQuery] CatalogAdminQuery query,CancellationToken ct) => Ok(await service.SearchAsync(catalog,query,ct));

    [HttpPost("{catalog}")]
    [RequirePermission(Permissions.ConfigurationManage)]
    public async Task<IActionResult> Create(string catalog,[FromBody] CatalogAdminSaveRequest request,CancellationToken ct) => Ok(await service.CreateAsync(catalog,request,ct));

    [HttpPut("{catalog}/{id}")]
    [RequirePermission(Permissions.ConfigurationManage)]
    public async Task<IActionResult> Update(string catalog,string id,[FromBody] CatalogAdminSaveRequest request,CancellationToken ct) => Ok(await service.UpdateAsync(catalog,id,request,ct));

    [HttpPatch("{catalog}/{id}/active")]
    [RequirePermission(Permissions.ConfigurationManage)]
    public async Task<IActionResult> Active(string catalog,string id,[FromBody] SetCatalogActiveRequest request,CancellationToken ct){await service.SetActiveAsync(catalog,id,request.IsActive,ct);return NoContent();}

    [HttpGet("{catalog}/{id}/delete-check")]
    [RequirePermission(Permissions.ConfigurationManage)]
    public async Task<IActionResult> DeleteCheck(string catalog,string id,CancellationToken ct)=>Ok(await service.CheckDeleteAsync(catalog,id,ct));

    [HttpDelete("{catalog}/{id}")]
    [RequirePermission(Permissions.ConfigurationManage)]
    public async Task<IActionResult> Delete(string catalog,string id,CancellationToken ct){await service.DeleteAsync(catalog,id,ct);return NoContent();}
}

public sealed record SetCatalogActiveRequest(bool IsActive);
