using ImportOps.Api.Middleware;
using ImportOps.Application.Common;

namespace ImportOps.Api.Services;

public sealed class HttpRequestMetadata(IHttpContextAccessor accessor) : IRequestMetadata
{
    public string? IpAddress => accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    public string? UserAgent => accessor.HttpContext?.Request.Headers.UserAgent.ToString();

    public Guid? CorrelationId
    {
        get
        {
            var value = accessor.HttpContext?.Items[CorrelationIdMiddleware.HeaderName];
            return value is Guid id ? id : null;
        }
    }
}
