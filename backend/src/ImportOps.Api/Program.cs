using ImportOps.Api.Middleware;
using ImportOps.Api.Security;
using ImportOps.Api.Services;
using ImportOps.Application.Common;
using ImportOps.Infrastructure;
using ImportOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ImportOps API",
        Version = "v1"
    });
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentCompany, HttpCurrentCompany>();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddScoped<IRequestMetadata, HttpRequestMetadata>();

// Azure App Service termina HTTPS en el proxy; respetar X-Forwarded-Proto.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<AlertEvaluationHostedService>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = builder.Configuration["LocalAuthentication:CookieName"] ?? "ImportOps.DevSession";
        options.Cookie.HttpOnly = true;
        // Desarrollo local: cookie first-party. Azure: cookie cross-site HTTPS.
        options.Cookie.SameSite = builder.Environment.IsDevelopment()
            ? SameSiteMode.Lax
            : SameSiteMode.None;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var allowedOrigins =
    builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? Array.Empty<string>();

if (allowedOrigins.Length == 0 && !builder.Environment.IsDevelopment())
    throw new InvalidOperationException("Cors:AllowedOrigins debe configurarse en producción.");

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy
                .WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ImportOpsDbContext>(
        "mariadb",
        tags: new[] { "ready" });

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();

// Protección de origen para solicitudes con efectos secundarios (incluido login).
// La cabecera Origin del navegador debe coincidir exactamente con un origen autorizado.
// En producción, no aceptar solicitudes mutantes sin Origin/Referer desde navegadores.
app.Use(async (context, next) =>
{
    if (HttpMethods.IsPost(context.Request.Method) ||
        HttpMethods.IsPut(context.Request.Method) ||
        HttpMethods.IsPatch(context.Request.Method) ||
        HttpMethods.IsDelete(context.Request.Method))
    {
        var origin = context.Request.Headers.Origin.ToString();
        if (string.IsNullOrWhiteSpace(origin) &&
            Uri.TryCreate(context.Request.Headers.Referer.ToString(), UriKind.Absolute, out var referer))
        {
            origin = referer.GetLeftPart(UriPartial.Authority);
        }

        // Exigir origen a las solicitudes con cookies de autenticación.
        // Para login, exigirlo también cuando procede de un navegador.
        var hasAuthCookie = context.Request.Cookies.ContainsKey(
            builder.Configuration["LocalAuthentication:CookieName"] ?? "ImportOps.DevSession");
        var isLogin = context.Request.Path.Equals("/api/auth/login", StringComparison.OrdinalIgnoreCase);
        var isBrowser = context.Request.Headers.ContainsKey("Sec-Fetch-Mode");
        if ((hasAuthCookie || (isLogin && isBrowser)) &&
            (string.IsNullOrWhiteSpace(origin) ||
             !allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase)))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                code = "ORIGIN_NOT_ALLOWED",
                message = "Origen no autorizado para esta operación."
            });
            return;
        }
    }

    await next();
});
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
}).AllowAnonymous();

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
}).AllowAnonymous();

app.Run();

public partial class Program;
