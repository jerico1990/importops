namespace ImportOps.Application.Security;

public interface ILocalAuthService
{
    Task<LoginUserDto?> ValidateCredentialsAsync(string username, string password, CancellationToken ct);
    Task<LoginUserDto> GetUserAsync(long userId, CancellationToken ct);
    Task<LoginUserDto> UpdateMyProfileAsync(long userId, UpdateMyProfileRequest request, CancellationToken ct);
    Task ChangePasswordAsync(long userId, ChangePasswordRequest request, CancellationToken ct);
}
