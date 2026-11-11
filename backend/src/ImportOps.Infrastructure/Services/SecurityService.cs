using ImportOps.Application.Common;
using ImportOps.Application.Security;
using ImportOps.Domain.Entities;
using ImportOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportOps.Infrastructure.Services;

public sealed class SecurityService(
    ImportOpsDbContext db,
    ICurrentUser currentUser,
    ICurrentCompany currentCompany,
    IPasswordHasher passwordHasher) : ISecurityService
{
    public async Task<CurrentSecurityUserDto> GetMeAsync(CancellationToken ct)
    {
        var user = await db.AppUsers.AsNoTracking()
            .SingleOrDefaultAsync(x =>
                x.UserId == currentUser.UserId &&
                x.CompanyId == currentCompany.CompanyId &&
                x.IsActive &&
                !x.IsDeleted, ct)
            ?? throw new BusinessException(ErrorCodes.InvalidIdentity, "Usuario no habilitado en ImportOps.", 401);

        var roles = await GetUserRolesAsync(user.UserId, user.CompanyId, ct);
        var permissions = await GetPermissionsAsync(roles.Select(x => x.RoleId), ct);

        var clientName = await GetClientNameAsync(user.ClientId, user.CompanyId, ct);

        return new(
            user.UserId,
            user.CompanyId,
            user.Username,
            user.Email,
            user.FirstName,
            user.LastName,
            user.UserType,
            user.ClientId,
            clientName,
            roles.Select(x => x.Code).OrderBy(x => x).ToArray(),
            permissions);
    }

    public async Task<PagedResult<SecurityUserDto>> GetUsersAsync(
        string? search,
        string? userType,
        long? clientId,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var q = db.AppUsers.AsNoTracking()
            .Where(x => x.CompanyId == currentCompany.CompanyId);

        if (currentUser.IsClientUser)
        {
            if (!currentUser.ClientId.HasValue)
                throw new BusinessException(ErrorCodes.Forbidden, "Usuario cliente sin client_id.", 403);

            q = q.Where(x =>
                x.UserType == UserTypes.Client &&
                x.ClientId == currentUser.ClientId.Value);
        }

        if (!string.IsNullOrWhiteSpace(userType))
        {
            var normalizedUserType = NormalizeUserType(userType);
            q = q.Where(x => x.UserType == normalizedUserType);
        }

        if (clientId.HasValue)
            q = q.Where(x => x.ClientId == clientId.Value);

        if (isActive.HasValue)
            q = q.Where(x => x.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            q = q.Where(x =>
                x.Username.Contains(term) ||
                (x.Email != null && x.Email.Contains(term)) ||
                (x.FirstName != null && x.FirstName.Contains(term)) ||
                (x.LastName != null && x.LastName.Contains(term)));
        }

        var total = await q.CountAsync(ct);

        var users = await q
            .OrderBy(x => x.Username)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var result = new List<SecurityUserDto>();

        foreach (var user in users)
            result.Add(await MapUserAsync(user, ct));

        return new(result, page, pageSize, total);
    }

    public async Task<IReadOnlyCollection<SecurityRoleDto>> GetRolesAsync(CancellationToken ct)
    {
        var q = db.Roles.AsNoTracking()
            .Where(x =>
                x.CompanyId == currentCompany.CompanyId &&
                x.IsActive);

        if (currentUser.IsClientUser)
            q = q.Where(x => x.Code == "CLIENT_ADMIN" || x.Code == "CLIENT_ADMI" || x.Code == "CLIENT_USER");

        return await q
            .OrderBy(x => x.Name)
            .Select(x => new SecurityRoleDto(
                x.RoleId,
                x.Code,
                x.Name,
                x.IsSystem,
                x.IsActive))
            .ToListAsync(ct);
    }

    public async Task<SecurityUserDto> CreateUserAsync(
        CreateSecurityUserRequest r,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(r.Username))
            throw new BusinessException(ErrorCodes.Validation, "Username es obligatorio.", 400);

        ValidatePassword(r.Password);

        var userType = NormalizeUserType(r.UserType);

        if (userType == UserTypes.Client && !r.ClientId.HasValue)
            throw new BusinessException(ErrorCodes.Validation, "Un usuario CLIENT requiere client_id.", 400);

        if (userType == UserTypes.Internal && r.ClientId.HasValue)
            throw new BusinessException(ErrorCodes.Validation, "Un usuario INTERNAL no puede tener client_id.", 400);

        if (r.ClientId.HasValue &&
            !await db.Clients.AsNoTracking().AnyAsync(x =>
                x.ClientId == r.ClientId.Value &&
                x.CompanyId == currentCompany.CompanyId &&
                x.IsActive, ct))
            throw new BusinessException(ErrorCodes.Validation, "Cliente inválido.", 400);

        var username = r.Username.Trim();

        if (await db.AppUsers.IgnoreQueryFilters().AnyAsync(x =>
            x.CompanyId == currentCompany.CompanyId &&
            x.Username == username &&
            !x.IsDeleted, ct))
            throw new BusinessException("USERNAME_EXISTS", "Ya existe un usuario con ese username.", 409);

        await ValidateRolesAsync(r.RoleIds, userType, ct);

        var user = new AppUser
        {
            CompanyId = currentCompany.CompanyId,
            Username = username,
            Email = Clean(r.Email),
            FirstName = Clean(r.FirstName),
            LastName = Clean(r.LastName),
            UserType = userType,
            ClientId = r.ClientId,
            PasswordHash = passwordHasher.Hash(r.Password),
            MustChangePassword = r.MustChangePassword,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUser.UserId,
            IsDeleted = false
        };

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        db.AppUsers.Add(user);
        await db.SaveChangesAsync(ct);

        foreach (var roleId in r.RoleIds.Distinct())
        {
            db.UserRoles.Add(new UserRole
            {
                UserId = user.UserId,
                RoleId = roleId,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = currentUser.UserId
            });
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return await MapUserAsync(user, ct);
    }

    public async Task<SecurityUserDto?> UpdateUserAsync(
        long userId,
        UpdateSecurityUserRequest r,
        CancellationToken ct)
    {
        var user = await db.AppUsers.SingleOrDefaultAsync(x =>
            x.UserId == userId &&
            x.CompanyId == currentCompany.CompanyId &&
            !x.IsDeleted, ct);

        if (user is null) return null;

        var userType = NormalizeUserType(r.UserType);

        if (userType == UserTypes.Client && !r.ClientId.HasValue)
            throw new BusinessException(ErrorCodes.Validation, "Un usuario CLIENT requiere client_id.", 400);

        if (userType == UserTypes.Internal && r.ClientId.HasValue)
            throw new BusinessException(ErrorCodes.Validation, "Un usuario INTERNAL no puede tener client_id.", 400);

        if (r.ClientId.HasValue &&
            !await db.Clients.AsNoTracking().AnyAsync(x =>
                x.ClientId == r.ClientId.Value &&
                x.CompanyId == currentCompany.CompanyId &&
                x.IsActive, ct))
            throw new BusinessException(ErrorCodes.Validation, "Cliente inválido.", 400);

        await ValidateRolesAsync(r.RoleIds, userType, ct);

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        user.Email = Clean(r.Email);
        user.FirstName = Clean(r.FirstName);
        user.LastName = Clean(r.LastName);
        user.UserType = userType;
        user.ClientId = r.ClientId;
        user.ModifiedAt = DateTime.UtcNow;
        user.ModifiedBy = currentUser.UserId;

        var currentRoles = await db.UserRoles
            .Where(x => x.UserId == userId)
            .ToListAsync(ct);

        db.UserRoles.RemoveRange(currentRoles);

        foreach (var roleId in r.RoleIds.Distinct())
        {
            db.UserRoles.Add(new UserRole
            {
                UserId = userId,
                RoleId = roleId,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = currentUser.UserId
            });
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return await MapUserAsync(user, ct);
    }

    public async Task<SecurityUserDto?> SetUserActiveAsync(
        long userId,
        SetUserActiveRequest request,
        CancellationToken ct)
    {
        if (userId == currentUser.UserId && !request.IsActive)
            throw new BusinessException("SELF_DEACTIVATION_NOT_ALLOWED", "No puedes desactivar tu propio usuario.", 409);

        var user = await db.AppUsers.SingleOrDefaultAsync(x =>
            x.UserId == userId &&
            x.CompanyId == currentCompany.CompanyId &&
            !x.IsDeleted, ct);

        if (user is null) return null;

        user.IsActive = request.IsActive;
        user.ModifiedAt = DateTime.UtcNow;
        user.ModifiedBy = currentUser.UserId;

        await db.SaveChangesAsync(ct);
        return await MapUserAsync(user, ct);
    }

    public async Task<SecurityUserDto?> UpdateUserRolesAsync(
        long userId,
        UpdateUserRolesRequest r,
        CancellationToken ct)
    {
        var user = await db.AppUsers.SingleOrDefaultAsync(x =>
            x.UserId == userId &&
            x.CompanyId == currentCompany.CompanyId, ct);

        if (user is null) return null;

        await ValidateRolesAsync(r.RoleIds, user.UserType, ct);

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var current = await db.UserRoles
            .Where(x => x.UserId == userId)
            .ToListAsync(ct);

        db.UserRoles.RemoveRange(current);

        foreach (var roleId in r.RoleIds.Distinct())
        {
            db.UserRoles.Add(new UserRole
            {
                UserId = userId,
                RoleId = roleId,
                AssignedAt = DateTime.UtcNow,
                AssignedBy = currentUser.UserId
            });
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return await MapUserAsync(user, ct);
    }

    public async Task ResetPasswordAsync(
        long userId,
        ResetUserPasswordRequest request,
        CancellationToken ct)
    {
        ValidatePassword(request.NewPassword);

        var user = await db.AppUsers.SingleOrDefaultAsync(x =>
            x.UserId == userId &&
            x.CompanyId == currentCompany.CompanyId, ct)
            ?? throw new BusinessException(ErrorCodes.NotFound, "Usuario no encontrado.", 404);

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        user.MustChangePassword = request.MustChangePassword;
        user.ModifiedAt = DateTime.UtcNow;
        user.ModifiedBy = currentUser.UserId;

        await db.SaveChangesAsync(ct);
    }

    private async Task ValidateRolesAsync(
        IEnumerable<int> roleIds,
        string userType,
        CancellationToken ct)
    {
        var ids = roleIds.Distinct().ToArray();

        if (ids.Length == 0)
            throw new BusinessException(ErrorCodes.Validation, "Debe asignar al menos un rol.", 400);

        var roles = await db.Roles.AsNoTracking()
            .Where(x =>
                ids.Contains(x.RoleId) &&
                x.CompanyId == currentCompany.CompanyId &&
                x.IsActive)
            .ToListAsync(ct);

        if (roles.Count != ids.Length)
            throw new BusinessException(ErrorCodes.Validation, "Uno o más roles son inválidos.", 400);

        var clientRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CLIENT_ADMIN",
            "CLIENT_ADMI",
            "CLIENT_USER"
        };

        if (userType == UserTypes.Client && roles.Any(x => !clientRoles.Contains(x.Code)))
            throw new BusinessException(
                ErrorCodes.Validation,
                "Un usuario CLIENT solo puede tener roles CLIENT_ADMIN/CLIENT_USER.",
                400);

        if (userType == UserTypes.Internal && roles.Any(x => clientRoles.Contains(x.Code)))
            throw new BusinessException(
                ErrorCodes.Validation,
                "Un usuario INTERNAL no puede tener roles de cliente.",
                400);
    }

    private async Task<SecurityUserDto> MapUserAsync(AppUser user, CancellationToken ct)
    {
        var roles = await GetUserRolesAsync(user.UserId, user.CompanyId, ct);
        var clientName = await GetClientNameAsync(user.ClientId, user.CompanyId, ct);

        return new(
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
            user.IsActive,
            roles);
    }

    private async Task<List<SecurityRoleDto>> GetUserRolesAsync(
        long userId,
        long companyId,
        CancellationToken ct)
        => await (
            from ur in db.UserRoles.AsNoTracking()
            join r in db.Roles.AsNoTracking() on ur.RoleId equals r.RoleId
            where ur.UserId == userId &&
                  r.CompanyId == companyId &&
                  r.IsActive
            select new SecurityRoleDto(
                r.RoleId,
                r.Code,
                r.Name,
                r.IsSystem,
                r.IsActive))
            .ToListAsync(ct);

    private async Task<List<string>> GetPermissionsAsync(
        IEnumerable<int> roleIds,
        CancellationToken ct)
    {
        var ids = roleIds.Distinct().ToArray();

        if (ids.Length == 0) return new();

        return await (
            from rp in db.RolePermissions.AsNoTracking()
            join p in db.Permissions.AsNoTracking() on rp.PermissionId equals p.PermissionId
            where ids.Contains(rp.RoleId) && p.IsActive
            select p.Code)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(ct);
    }

    private async Task<string?> GetClientNameAsync(
        long? clientId,
        long companyId,
        CancellationToken ct)
    {
        if (!clientId.HasValue) return null;

        return await db.Clients.AsNoTracking()
            .Where(x =>
                x.ClientId == clientId.Value &&
                x.CompanyId == companyId)
            .Select(x => x.LegalName)
            .SingleOrDefaultAsync(ct);
    }

    private static string NormalizeUserType(string value)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? UserTypes.Internal
            : value.Trim().ToUpperInvariant();

        if (normalized is not (UserTypes.Internal or UserTypes.Client))
            throw new BusinessException(ErrorCodes.Validation, "UserType debe ser INTERNAL o CLIENT.", 400);

        return normalized;
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

    private static string? Clean(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
