using System.Security.Claims;
using ImportOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace ImportOps.Api.Security;

public sealed class DatabaseClaimsTransformation(ImportOpsDbContext db) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
            return principal;

        if (principal.HasClaim(x => x.Type == ImportOpsClaimTypes.UserId))
            return principal;

        var externalId = principal.FindFirst("sub")?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(externalId))
            return principal;

        var user = await db.AppUsers.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ExternalAuthId == externalId && x.IsActive);

        if (user is null)
            return principal;

        var roles = await (
            from ur in db.UserRoles.AsNoTracking()
            join r in db.Roles.AsNoTracking() on ur.RoleId equals r.RoleId
            where ur.UserId == user.UserId
               && r.CompanyId == user.CompanyId
               && r.IsActive
            select new { r.RoleId, r.Code })
            .ToListAsync();

        var roleIds = roles.Select(x => x.RoleId).ToArray();

        var permissions = roleIds.Length == 0
            ? new List<string>()
            : await (
                from rp in db.RolePermissions.AsNoTracking()
                join p in db.Permissions.AsNoTracking() on rp.PermissionId equals p.PermissionId
                where roleIds.Contains(rp.RoleId) && p.IsActive
                select p.Code)
                .Distinct()
                .ToListAsync();

        var identity = new ClaimsIdentity();

        identity.AddClaim(new Claim(ImportOpsClaimTypes.UserId, user.UserId.ToString()));
        identity.AddClaim(new Claim(ImportOpsClaimTypes.CompanyId, user.CompanyId.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Name, user.Username));

        foreach (var role in roles)
            identity.AddClaim(new Claim(ClaimTypes.Role, role.Code));

        foreach (var permission in permissions)
            identity.AddClaim(new Claim(ImportOpsClaimTypes.Permission, permission));

        principal.AddIdentity(identity);
        return principal;
    }
}
