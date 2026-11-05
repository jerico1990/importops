using System.Security.Claims;
using ImportOps.Api.Security;
using ImportOps.Application.Common;
using ImportOps.Application.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImportOps.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    ILocalAuthService authService,
    ICurrentUser currentUser) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var user = await authService.ValidateCredentialsAsync(
            request.Username,
            request.Password,
            ct);

        if (user is null)
            return Unauthorized(new
            {
                code = "INVALID_CREDENTIALS",
                message = "Usuario o contraseña incorrectos."
            });

        var claims = new List<Claim>
        {
            new(ImportOpsClaimTypes.UserId, user.UserId.ToString()),
            new(ImportOpsClaimTypes.CompanyId, user.CompanyId.ToString()),
            new(ImportOpsClaimTypes.UserType, user.UserType),
            new(ClaimTypes.Name, user.Username)
        };

        if (user.ClientId.HasValue)
            claims.Add(new Claim(ImportOpsClaimTypes.ClientId, user.ClientId.Value.ToString()));

        foreach (var role in user.Roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        foreach (var permission in user.Permissions)
            claims.Add(new Claim(ImportOpsClaimTypes.Permission, permission));

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);

        var expires = DateTimeOffset.UtcNow.AddHours(request.RememberMe ? 24 : 8);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = request.RememberMe,
                ExpiresUtc = expires,
                AllowRefresh = true
            });

        return Ok(new LoginResultDto(user, expires.UtcDateTime));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    [Authorize]
    [HttpGet("session")]
    public async Task<IActionResult> Session(CancellationToken ct)
        => Ok(await authService.GetUserAsync(currentUser.UserId, ct));

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken ct)
    {
        await authService.ChangePasswordAsync(currentUser.UserId, request, ct);
        return NoContent();
    }
}
