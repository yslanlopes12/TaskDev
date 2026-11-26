using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.DataProtection;
using TaskDev.Api.Domain.Core.Entities;
using TaskDev.Api.Domain.Core.Models;
using TaskDev.Api.Domain.Ports.OutBound;

namespace TaskDev.Api.Infra.Configuration.InBound;

public interface ISessionHandler
{
    void CreateSession(HttpContext context, User user);
    Task<AuthSessionModel?> GetCurrentUserAsync(HttpContext context);
    void InvalidateSession(HttpContext context);
}

public class SessionHandler : ISessionHandler
{
    private const string CookieName = "taskdev-session";
    private readonly IDataProtector _protector;
    private readonly IUserRepository _userRepository;
    private readonly bool _secureCookies;

    public SessionHandler(IDataProtectionProvider provider, IUserRepository userRepository, IConfiguration configuration)
    {
        _protector = provider.CreateProtector("TaskDev.Session.Protection.v1");
        _userRepository = userRepository;
        _secureCookies = configuration.GetValue("Security:SecureCookies", false);
    }

    public void CreateSession(HttpContext context, User user)
    {
        var session = new AuthSessionModel
        {
            UserId = user.Id,
            AuthVersion = user.AuthVersion,
            Role = user.Role,
            Username = user.Username,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        };

        var json = JsonSerializer.Serialize(session);
        var encrypted = _protector.Protect(json);

        var isHttps = context.Request.IsHttps;
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = _secureCookies || isHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(7),
            Path = "/"
        };

        context.Response.Cookies.Append(CookieName, encrypted, cookieOptions);
    }

    public async Task<AuthSessionModel?> GetCurrentUserAsync(HttpContext context)
    {
        if (!context.Request.Cookies.TryGetValue(CookieName, out var cookieValue) || string.IsNullOrWhiteSpace(cookieValue))
        {
            return null;
        }

        try
        {
            var decryptedJson = _protector.Unprotect(cookieValue);
            var session = JsonSerializer.Deserialize<AuthSessionModel>(decryptedJson);
            if (session == null || session.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                InvalidateSession(context);
                return null;
            }

            var user = await _userRepository.GetByIdAsync(session.UserId);
            if (user == null || !user.IsActive || user.AuthVersion != session.AuthVersion)
            {
                InvalidateSession(context);
                return null;
            }

            // Authorization must use the current role, not a stale cookie claim.
            return new AuthSessionModel
            {
                UserId = user.Id,
                AuthVersion = user.AuthVersion,
                Role = user.Role,
                Username = user.Username,
                ExpiresAt = session.ExpiresAt
            };
        }
        catch
        {
            InvalidateSession(context);
            return null;
        }
    }

    public void InvalidateSession(HttpContext context)
    {
        context.Response.Cookies.Delete(CookieName, new CookieOptions
        {
            Path = "/"
        });
    }
}

public class SessionMiddleware
{
    private readonly RequestDelegate _next;

    public SessionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ISessionHandler sessionHandler)
    {
        var session = await sessionHandler.GetCurrentUserAsync(context);
        if (session != null)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, session.UserId.ToString()),
                new Claim(ClaimTypes.Name, session.Username),
                new Claim(ClaimTypes.Role, session.Role)
            };

            var identity = new ClaimsIdentity(claims, "TaskDevCookie");
            context.User = new ClaimsPrincipal(identity);
        }

        await _next(context);
    }
}

public class CsrfMiddleware
{
    private readonly RequestDelegate _next;
    private readonly bool _secureCookies;
    public const string CsrfCookieName = "XSRF-TOKEN";
    public const string CsrfHeaderName = "X-XSRF-TOKEN";
    public const string AlternativeHeaderName = "X-CSRF-TOKEN";

    public CsrfMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _secureCookies = configuration.GetValue("Security:SecureCookies", false);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var isSafeMethod = HttpMethods.IsGet(context.Request.Method) ||
                           HttpMethods.IsHead(context.Request.Method) ||
                           HttpMethods.IsOptions(context.Request.Method) ||
                           HttpMethods.IsTrace(context.Request.Method);

        // Se o cliente ainda não tiver o cookie XSRF-TOKEN, gera um novo
        if (!context.Request.Cookies.ContainsKey(CsrfCookieName))
        {
            var token = Guid.NewGuid().ToString("N");
            context.Response.Cookies.Append(CsrfCookieName, token, new CookieOptions
            {
                HttpOnly = false,
                Secure = _secureCookies || context.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Path = "/"
            });
        }

        // Validação CSRF para métodos mutativos de usuários autenticados
        if (!isSafeMethod && context.User.Identity?.IsAuthenticated == true)
        {
            var cookieToken = context.Request.Cookies[CsrfCookieName];
            var headerToken = context.Request.Headers[CsrfHeaderName].FirstOrDefault()
                              ?? context.Request.Headers[AlternativeHeaderName].FirstOrDefault();

            // Fail closed: both tokens must be present and equal.
            if (string.IsNullOrWhiteSpace(cookieToken) || string.IsNullOrWhiteSpace(headerToken) ||
                !string.Equals(cookieToken, headerToken, StringComparison.Ordinal))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"message\":\"Token CSRF ausente ou inválido. Obtenha um novo token em /api/auth/csrf.\"}");
                return;
            }
        }

        await _next(context);
    }
}

public static class ConfigurationSession
{
    public const string AuthPolicyName = "AuthRateLimit";

    public static IServiceCollection AddSessionAndSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDataProtection();
        services.AddHttpContextAccessor();
        services.AddScoped<ISessionHandler, SessionHandler>();

        var maxRequests = configuration.GetValue("RateLimit:LoginMaxRequests", 10);
        var periodSeconds = configuration.GetValue("RateLimit:LoginPeriodSeconds", 60);

        services.AddRateLimiter(options =>
        {
            options.AddPolicy(AuthPolicyName, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = maxRequests,
                        Window = TimeSpan.FromSeconds(periodSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";
                await context.HttpContext.Response.WriteAsync("{\"message\":\"Muitas tentativas. Aguarde um momento antes de tentar novamente.\"}", token);
            };
        });

        return services;
    }

    public static IApplicationBuilder UseSessionAndCsrf(this IApplicationBuilder app)
    {
        app.UseMiddleware<SessionMiddleware>();
        app.UseMiddleware<CsrfMiddleware>();
        return app;
    }
}
