using ImportOps.Api.Security;
using ImportOps.Application.Common;

namespace ImportOps.Api.Services;

public sealed class HttpCurrentCompany(IHttpContextAccessor accessor) : ICurrentCompany
{
    public long CompanyId
    {
        get
        {
            var value = accessor.HttpContext?.User
                .FindFirst(ImportOpsClaimTypes.CompanyId)?.Value;

            if (long.TryParse(value, out var id) && id > 0)
                return id;

            throw new BusinessException(
                ErrorCodes.InvalidIdentity,
                "No se pudo resolver la empresa del usuario autenticado.",
                401);
        }
    }
}
