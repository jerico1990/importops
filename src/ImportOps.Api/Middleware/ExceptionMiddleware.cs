using ImportOps.Application.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ImportOps.Api.Middleware;

public sealed class ExceptionMiddleware(
    RequestDelegate next,
    ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var (status, code, message, detail) = Map(ex);

            logger.LogError(
                ex,
                "Request failed. Code={ErrorCode} Status={StatusCode}",
                code,
                status);

            var traceId = context.TraceIdentifier;
            var correlationId = context.Items[CorrelationIdMiddleware.HeaderName]?.ToString();

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = status,
                Title = message,
                Detail = detail,
                Extensions =
                {
                    ["code"] = code,
                    ["traceId"] = traceId,
                    ["correlationId"] = correlationId
                }
            });
        }
    }

    private static (int Status, string Code, string Message, string? Detail) Map(Exception ex)
    {
        return ex switch
        {
            BusinessException b =>
                (b.StatusCode, b.Code, b.Message, b.Message),

            DbUpdateConcurrencyException =>
                (409, ErrorCodes.ConcurrencyConflict,
                 "El registro fue modificado por otro usuario.",
                 "Recargue la información antes de volver a guardar."),

            ArgumentException a =>
                (400, ErrorCodes.Validation, a.Message, a.Message),

            InvalidOperationException i =>
                (409, ErrorCodes.BusinessRule, i.Message, i.Message),

            UnauthorizedAccessException =>
                (403, ErrorCodes.Forbidden, "No tiene autorización para ejecutar esta acción.", null),

            _ =>
                (500, "INTERNAL_ERROR", "Se produjo un error interno.", null)
        };
    }
}
