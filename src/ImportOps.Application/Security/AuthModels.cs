namespace ImportOps.Application.Security;

public sealed class LoginRequest
{
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public bool RememberMe { get; init; }
}

public sealed record LoginUserDto(
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
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);

public sealed record LoginResultDto(
    LoginUserDto User,
    DateTime ExpiresAt);

public sealed class ChangePasswordRequest
{
    public string CurrentPassword { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
}
