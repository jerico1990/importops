namespace ImportOps.Application.Security;

public sealed record CurrentSecurityUserDto(
    long UserId,
    long CompanyId,
    string Username,
    string? Email,
    string? FirstName,
    string? LastName,
    string UserType,
    long? ClientId,
    string? ClientName,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);

public sealed record SecurityRoleDto(
    int RoleId,
    string Code,
    string Name,
    bool IsSystem,
    bool IsActive);

public sealed record SecurityUserDto(
    long UserId,
    long CompanyId,
    string Username,
    string? Email,
    string? FirstName,
    string? LastName,
    string UserType,
    long? ClientId,
    string? ClientName,
    bool MustChangePassword,
    bool IsActive,
    IReadOnlyCollection<SecurityRoleDto> Roles);

public sealed class CreateSecurityUserRequest
{
    public string Username { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }

    public string UserType { get; init; } = "INTERNAL";
    public long? ClientId { get; init; }

    public string Password { get; init; } = string.Empty;
    public bool MustChangePassword { get; init; } = true;

    public IReadOnlyCollection<int> RoleIds { get; init; } = Array.Empty<int>();
}

public sealed class UpdateUserRolesRequest
{
    public IReadOnlyCollection<int> RoleIds { get; init; } = Array.Empty<int>();
}

public sealed class ResetUserPasswordRequest
{
    public string NewPassword { get; init; } = string.Empty;
    public bool MustChangePassword { get; init; } = true;
}


public sealed class UpdateSecurityUserRequest
{
    public string? Email { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string UserType { get; init; } = "INTERNAL";
    public long? ClientId { get; init; }
    public IReadOnlyCollection<int> RoleIds { get; init; } = Array.Empty<int>();
}

public sealed class SetUserActiveRequest
{
    public bool IsActive { get; init; }
}
