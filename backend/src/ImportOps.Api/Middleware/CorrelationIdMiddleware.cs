namespace ImportOps.Api.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var raw = context.Request.Headers[HeaderName].FirstOrDefault();
        var correlationId = Guid.TryParse(raw, out var parsed)
            ? parsed
            : Guid.NewGuid();

        context.Items[HeaderName] = correlationId;
        context.Response.Headers[HeaderName] = correlationId.ToString();

        using (context.RequestServices
            .GetRequiredService<ILogger<CorrelationIdMiddleware>>()
            .BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId
            }))
        {
            await next(context);
        }
    }
}
