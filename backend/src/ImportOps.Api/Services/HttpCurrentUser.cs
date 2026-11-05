using ImportOps.Api.Security;
using ImportOps.Application.Common;
using ImportOps.Domain.Entities;

namespace ImportOps.Api.Services;

public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public long UserId
    {
        get
        {
            var value = accessor.HttpContext?.User.FindFirst(ImportOpsClaimTypes.UserId)?.Value;
            if (long.TryParse(value, out var id) && id > 0) return id;
            throw new BusinessException(ErrorCodes.InvalidIdentity, "No existe una sesión válida de ImportOps.", 401);
        }
    }

    public string UserType =>
        accessor.HttpContext?.User.FindFirst(ImportOpsClaimTypes.UserType)?.Value
        ?? UserTypes.Internal;

    public long? ClientId
    {
        get
        {
            var value = accessor.HttpContext?.User.FindFirst(ImportOpsClaimTypes.ClientId)?.Value;
            return long.TryParse(value, out var id) && id > 0 ? id : null;
        }
    }

    public bool IsClientUser =>
        string.Equals(UserType, UserTypes.Client, StringComparison.OrdinalIgnoreCase);
}
