using ImportOps.Application.Common;

namespace ImportOps.Application.Security;

public interface ISecurityService
{
    Task<CurrentSecurityUserDto> GetMeAsync(CancellationToken ct);
    Task<PagedResult<SecurityUserDto>> GetUsersAsync(string? search, string? userType, long? clientId, bool? isActive, int page, int pageSize, CancellationToken ct);
    Task<IReadOnlyCollection<SecurityRoleDto>> GetRolesAsync(CancellationToken ct);
    Task<SecurityUserDto> CreateUserAsync(CreateSecurityUserRequest request, CancellationToken ct);
    Task<SecurityUserDto?> UpdateUserAsync(long userId, UpdateSecurityUserRequest request, CancellationToken ct);
    Task<SecurityUserDto?> SetUserActiveAsync(long userId, SetUserActiveRequest request, CancellationToken ct);
    Task<SecurityUserDto?> UpdateUserRolesAsync(long userId, UpdateUserRolesRequest request, CancellationToken ct);
    Task ResetPasswordAsync(long userId, ResetUserPasswordRequest request, CancellationToken ct);
}
