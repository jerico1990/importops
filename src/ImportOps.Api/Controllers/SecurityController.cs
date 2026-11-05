using ImportOps.Api.Security;
using ImportOps.Application.Security;
using Microsoft.AspNetCore.Mvc;

namespace ImportOps.Api.Controllers;

[ApiController]
[Route("api/security")]
public sealed class SecurityController(ISecurityService service) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct) => Ok(await service.GetMeAsync(ct));

    [HttpGet("users")]
    [RequirePermission(Permissions.SecurityManage)]
    public async Task<IActionResult> Users(
        [FromQuery] string? search = null,
        [FromQuery] string? userType = null,
        [FromQuery] long? clientId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await service.GetUsersAsync(search, userType, clientId, isActive, page, pageSize, ct));

    [HttpGet("roles")]
    [RequirePermission(Permissions.SecurityManage)]
    public async Task<IActionResult> Roles(CancellationToken ct) => Ok(await service.GetRolesAsync(ct));

    [HttpPost("users")]
    [RequirePermission(Permissions.SecurityManage)]
    public async Task<IActionResult> CreateUser([FromBody] CreateSecurityUserRequest request, CancellationToken ct)
        => Ok(await service.CreateUserAsync(request, ct));

    [HttpPut("users/{userId:long}")]
    [RequirePermission(Permissions.SecurityManage)]
    public async Task<IActionResult> UpdateUser(long userId, [FromBody] UpdateSecurityUserRequest request, CancellationToken ct)
    {
        var result = await service.UpdateUserAsync(userId, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("users/{userId:long}/roles")]
    [RequirePermission(Permissions.SecurityManage)]
    public async Task<IActionResult> UpdateRoles(long userId, [FromBody] UpdateUserRolesRequest request, CancellationToken ct)
    {
        var result = await service.UpdateUserRolesAsync(userId, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("users/{userId:long}/active")]
    [RequirePermission(Permissions.SecurityManage)]
    public async Task<IActionResult> SetActive(long userId, [FromBody] SetUserActiveRequest request, CancellationToken ct)
    {
        var result = await service.SetUserActiveAsync(userId, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("users/{userId:long}/reset-password")]
    [RequirePermission(Permissions.SecurityManage)]
    public async Task<IActionResult> ResetPassword(long userId, [FromBody] ResetUserPasswordRequest request, CancellationToken ct)
    {
        await service.ResetPasswordAsync(userId, request, ct);
        return NoContent();
    }
}
