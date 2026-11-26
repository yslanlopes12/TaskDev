using TaskDev.Api.Adapter.InBound.AdapterHttp.DTOs.Request;
using TaskDev.Api.Adapter.InBound.AdapterHttp.DTOs.Validation;
using TaskDev.Api.Adapter.InBound.AdapterHttp.Mapper;
using TaskDev.Api.Domain.Ports.InBound;
using TaskDev.Api.Infra.Configuration.InBound;

namespace TaskDev.Api.Adapter.InBound.AdapterHttp.Routes;

public static class EndpointAuth
{
    public static RouteGroupBuilder MapEndpointAuth(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", async (RequestRegister request, IAuthUseCase authUseCase) =>
        {
            var (isValid, errors) = HttpValidator.Validate(request);
            if (!isValid) return Results.BadRequest(new { message = "Dados inválidos.", errors });

            var result = await authUseCase.RegisterAsync(request.ToCommand());
            return result.ToHttpResult(res => res.ToResponse(), response => Results.Created($"/api/users/{response.Id}", response));
        }).RequireRateLimiting(ConfigurationSession.AuthPolicyName);

        group.MapPost("/login", async (RequestLogin request, IAuthUseCase authUseCase, ISessionHandler sessionHandler,
            HttpContext context, IConfiguration configuration) =>
        {
            var (isValid, errors) = HttpValidator.Validate(request);
            if (!isValid) return Results.BadRequest(new { message = "Dados inválidos.", errors });

            var result = await authUseCase.LoginAsync(request.ToCommand());
            if (!result.IsSuccess || result.SuccessObject == null)
            {
                return Results.Unauthorized();
            }

            sessionHandler.CreateSession(context, result.SuccessObject.User);

            var csrfToken = Guid.NewGuid().ToString("N");
            context.Response.Cookies.Append(CsrfMiddleware.CsrfCookieName, csrfToken, new CookieOptions
            {
                HttpOnly = false,
                Secure = context.Request.IsHttps || configuration.GetValue("Security:SecureCookies", false),
                SameSite = SameSiteMode.Lax,
                Path = "/"
            });

            return Results.Ok(new
            {
                message = "Login realizado com sucesso.",
                csrfToken,
                user = result.SuccessObject.ToResponse()
            });
        }).RequireRateLimiting(ConfigurationSession.AuthPolicyName);

        group.MapPost("/logout", (ISessionHandler sessionHandler, HttpContext context) =>
        {
            sessionHandler.InvalidateSession(context);
            return Results.Ok(new { message = "Sessão encerrada com sucesso." });
        });

        group.MapGet("/me", async (ISessionHandler sessionHandler, IAuthUseCase authUseCase, HttpContext context) =>
        {
            var user = await sessionHandler.GetCurrentUserAsync(context);
            if (user == null) return Results.Unauthorized();

            var result = await authUseCase.GetMeAsync(user.Id);
            return result.ToHttpResult(res => res.ToResponse());
        });

        group.MapGet("/csrf", (HttpContext context, IConfiguration configuration) =>
        {
            var token = context.Request.Cookies[CsrfMiddleware.CsrfCookieName] ?? Guid.NewGuid().ToString("N");
            context.Response.Cookies.Append(CsrfMiddleware.CsrfCookieName, token, new CookieOptions
            {
                HttpOnly = false,
                Secure = context.Request.IsHttps || configuration.GetValue("Security:SecureCookies", false),
                SameSite = SameSiteMode.Lax,
                Path = "/"
            });
            return Results.Ok(new { csrfToken = token });
        });

        return group;
    }
}
