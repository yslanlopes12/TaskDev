using TaskDev.Api.Adapter.InBound.AdapterHttp.DTOs.Request;
using TaskDev.Api.Adapter.InBound.AdapterHttp.DTOs.Validation;
using TaskDev.Api.Adapter.InBound.AdapterHttp.Mapper;
using TaskDev.Api.Domain.Ports.InBound;
using TaskDev.Api.Infra.Configuration.InBound;

namespace TaskDev.Api.Adapter.InBound.AdapterHttp.Routes;

public static class EndpointAdmin
{
    public static RouteGroupBuilder MapEndpointAdmin(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/admin").WithTags("Admin");

        // Bootstrap do primeiro administrador
        group.MapPost("/bootstrap", async (RequestBootstrapAdmin request, IAdminUseCase adminUseCase,
            IConfiguration configuration, IWebHostEnvironment environment) =>
        {
            if (!configuration.GetValue("Admin:BootstrapEnabled", environment.IsDevelopment()))
                return Results.NotFound();

            var (isValid, errors) = HttpValidator.Validate(request);
            if (!isValid) return Results.BadRequest(new { message = "Dados inválidos.", errors });

            var result = await adminUseCase.BootstrapAdminAsync(request.ToCommand());
            return result.ToHttpResult(res => res.ToResponse(), response => Results.Created($"/api/users/{response.Id}", response));
        }).RequireRateLimiting(ConfigurationSession.AuthPolicyName);

        // Listar todos os usuários (ADMIN ou OPERATOR)
        group.MapGet("/users", async (HttpContext context, ISessionHandler sessionHandler, IAdminUseCase adminUseCase) =>
        {
            var user = await sessionHandler.GetCurrentUserAsync(context);
            if (user == null) return Results.Unauthorized();

            var result = await adminUseCase.GetAllUsersAsync(user.Id);
            return result.ToHttpResult(res => res.ToAdminResponse());
        });

        // Resetar senha (ADMIN ou OPERATOR)
        group.MapPost("/users/{id:guid}/reset-password", async (Guid id, HttpContext context, ISessionHandler sessionHandler, IAdminUseCase adminUseCase) =>
        {
            var user = await sessionHandler.GetCurrentUserAsync(context);
            if (user == null) return Results.Unauthorized();

            var result = await adminUseCase.ResetPasswordAsync(user.Id, id);
            return result.ToHttpResult(res => res.ToResponse());
        });

        // Alterar perfil de acesso (ADMIN)
        group.MapPatch("/users/{id:guid}/role", async (Guid id, RequestUpdateRole request, HttpContext context, ISessionHandler sessionHandler, IAdminUseCase adminUseCase) =>
        {
            var user = await sessionHandler.GetCurrentUserAsync(context);
            if (user == null) return Results.Unauthorized();

            var (isValid, errors) = HttpValidator.Validate(request);
            if (!isValid) return Results.BadRequest(new { message = "Dados inválidos.", errors });

            var result = await adminUseCase.UpdateUserRoleAsync(user.Id, id, request.Role);
            return result.ToHttpResult(_ => Results.Ok(new { message = "Papel atualizado com sucesso." }));
        });

        // Ativar / Desativar usuário (ADMIN)
        group.MapPatch("/users/{id:guid}/active", async (Guid id, RequestUpdateActive request, HttpContext context, ISessionHandler sessionHandler, IAdminUseCase adminUseCase) =>
        {
            var user = await sessionHandler.GetCurrentUserAsync(context);
            if (user == null) return Results.Unauthorized();

            var (isValid, errors) = HttpValidator.Validate(request);
            if (!isValid) return Results.BadRequest(new { message = "Dados inválidos.", errors });

            var result = await adminUseCase.UpdateUserActiveAsync(user.Id, id, request.IsActive);
            return result.ToHttpResult(_ => Results.Ok(new { message = request.IsActive ? "Usuário ativado com sucesso." : "Usuário desativado com sucesso." }));
        });

        return group;
    }
}
