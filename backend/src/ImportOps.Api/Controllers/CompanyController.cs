using ImportOps.Api.Security;
using ImportOps.Application.Security;
using ImportOps.Application.Company;
using Microsoft.AspNetCore.Mvc;

namespace ImportOps.Api.Controllers;

[ApiController]
[Route("api/company")]
public sealed class CompanyController(ICompanyService service) : ControllerBase
{
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrent(CancellationToken ct)
    {
        var result = await service.GetCurrentAsync(ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("current")]
    [RequirePermission(Permissions.SecurityManage)]
    public async Task<IActionResult> UpdateCurrent([FromBody] UpdateCompanyRequest request, CancellationToken ct)
    {
        var result = await service.UpdateCurrentAsync(request, ct);
        return result is null ? NotFound() : Ok(result);
    }
}
