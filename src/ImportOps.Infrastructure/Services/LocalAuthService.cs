using ImportOps.Application.Common;
using ImportOps.Application.Security;
using ImportOps.Domain.Entities;
using ImportOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportOps.Infrastructure.Services;

public sealed class LocalAuthService(
    ImportOpsDbContext db,
    IPasswordHasher passwordHasher) : ILocalAuthService
{
    public async Task<LoginUserDto?> ValidateCredentialsAsync(
        string username,
        string password,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return null;

        var normalized = username.Trim();

        var user = await db.AppUsers
            .SingleOrDefaultAsync(x =>
                !x.IsDeleted &&
                x.IsActive &&
                (x.Username == normalized || x.Email == normalized), ct);

        if (user is null || string.IsNullOrWhiteSpace(user.PasswordHash))
            return null;

        if (!passwordHasher.Verify(password, user.PasswordHash))
            return null;

        user.LastLoginAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return await BuildUserAsync(user, ct);
    }

    public async Task<LoginUserDto> GetUserAsync(long userId, CancellationToken ct)
    {
        var user = await db.AppUsers.AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == userId && x.IsActive && !x.IsDeleted, ct)
            ?? throw new BusinessException(ErrorCodes.InvalidIdentity, "Usuario no habilitado.", 401);

        return await BuildUserAsync(user, ct);
    }

    public async Task ChangePasswordAsync(long userId, ChangePasswordRequest request, CancellationToken ct)
    {
        var user = await db.AppUsers
            .SingleOrDefaultAsync(x => x.UserId == userId && x.IsActive && !x.IsDeleted, ct)
            ?? throw new BusinessException(ErrorCodes.InvalidIdentity, "Usuario no habilitado.", 401);

        if (string.IsNullOrWhiteSpace(user.PasswordHash) ||
            !passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new BusinessException("INVALID_CURRENT_PASSWORD", "La contraseña actual es incorrecta.", 400);

        ValidatePassword(request.NewPassword);

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        user.MustChangePassword = false;
        user.ModifiedAt = DateTime.UtcNow;
        user.ModifiedBy = user.UserId;

        await db.SaveChangesAsync(ct);
    }

    private async Task<LoginUserDto> BuildUserAsync(AppUser user, CancellationToken ct)
    {
        var roleRows = await (
            from ur in db.UserRoles.AsNoTracking()
            join r in db.Roles.AsNoTracking() on ur.RoleId equals r.RoleId
            where ur.UserId == user.UserId &&
                  r.CompanyId == user.CompanyId &&
                  r.IsActive
            select new { r.RoleId, r.Code })
            .ToListAsync(ct);

        var roleIds = roleRows.Select(x => x.RoleId).ToArray();

        var permissions = roleIds.Length == 0
            ? new List<string>()
            : await (
                from rp in db.RolePermissions.AsNoTracking()
                join p in db.Permissions.AsNoTracking() on rp.PermissionId equals p.PermissionId
                where roleIds.Contains(rp.RoleId) && p.IsActive
                select p.Code)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync(ct);

        string? clientName = null;

        if (user.ClientId.HasValue)
        {
            clientName = await db.Clients.AsNoTracking()
                .Where(x => x.ClientId == user.ClientId.Value &&
                            x.CompanyId == user.CompanyId)
                .Select(x => x.LegalName)
                .SingleOrDefaultAsync(ct);
        }

        return new LoginUserDto(
            user.UserId,
            user.CompanyId,
            user.Username,
            user.Email,
            user.FirstName,
            user.LastName,
            user.UserType,
            user.ClientId,
            clientName,
            user.MustChangePassword,
            roleRows.Select(x => x.Code).OrderBy(x => x).ToArray(),
            permissions);
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            throw new BusinessException("PASSWORD_TOO_SHORT", "La contraseña debe tener al menos 8 caracteres.", 400);

        if (!password.Any(char.IsUpper) ||
            !password.Any(char.IsLower) ||
            !password.Any(char.IsDigit))
            throw new BusinessException(
                "PASSWORD_WEAK",
                "La contraseña debe incluir mayúscula, minúscula y número.",
                400);
    }
}
