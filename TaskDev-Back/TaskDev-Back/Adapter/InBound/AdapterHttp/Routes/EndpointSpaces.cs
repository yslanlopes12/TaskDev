using TaskDev.Api.Adapter.InBound.AdapterHttp.DTOs.Request;
using TaskDev.Api.Adapter.InBound.AdapterHttp.DTOs.Validation;
using TaskDev.Api.Adapter.InBound.AdapterHttp.Mapper;
using TaskDev.Api.Domain.Ports.InBound;
using TaskDev.Api.Infra.Configuration.InBound;

namespace TaskDev.Api.Adapter.InBound.AdapterHttp.Routes;

public static class EndpointSpaces
{
    public static RouteGroupBuilder MapEndpointSpaces(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/spaces").WithTags("Spaces");

        group.MapGet("/", async (HttpContext context, ISessionHandler sessionHandler, ISpaceUseCase spaceUseCase) =>
        {
            var user = await sessionHandler.GetCurrentUserAsync(context);
            if (user == null) return Results.Unauthorized();

            var result = await spaceUseCase.GetSpacesAsync(user.Id);
            return result.ToHttpResult(res => res.ToResponse());
        });

        group.MapPost("/", async (RequestCreateSpace request, HttpContext context, ISessionHandler sessionHandler, ISpaceUseCase spaceUseCase) =>
        {
            var user = await sessionHandler.GetCurrentUserAsync(context);
            if (user == null) return Results.Unauthorized();

            var (isValid, errors) = HttpValidator.Validate(request);
            if (!isValid) return Results.BadRequest(new { message = "Dados inválidos.", errors });

            var result = await spaceUseCase.CreateSpaceAsync(request.ToCommand(user.Id));
            return result.ToHttpResult(res => res.ToResponse(), response => Results.Created($"/api/spaces/{response.Id}", response));
        });

        group.MapGet("/{id:guid}", async (Guid id, HttpContext context, ISessionHandler sessionHandler, ISpaceUseCase spaceUseCase) =>
        {
            var user = await sessionHandler.GetCurrentUserAsync(context);
            if (user == null) return Results.Unauthorized();

            var result = await spaceUseCase.GetByIdAsync(id, user.Id);
            return result.ToHttpResult(res => res.ToResponse());
        });

        group.MapPut("/{id:guid}", async (Guid id, RequestUpdateSpace request, HttpContext context, ISessionHandler sessionHandler, ISpaceUseCase spaceUseCase) =>
        {
            var user = await sessionHandler.GetCurrentUserAsync(context);
            if (user == null) return Results.Unauthorized();

            var (isValid, errors) = HttpValidator.Validate(request);
            if (!isValid) return Results.BadRequest(new { message = "Dados inválidos.", errors });

            var result = await spaceUseCase.UpdateSpaceAsync(request.ToCommand(id, user.Id));
            return result.ToHttpResult(res => res.ToResponse());
        });

        group.MapDelete("/{id:guid}", async (Guid id, HttpContext context, ISessionHandler sessionHandler, ISpaceUseCase spaceUseCase) =>
        {
            var user = await sessionHandler.GetCurrentUserAsync(context);
            if (user == null) return Results.Unauthorized();

            var result = await spaceUseCase.DeleteSpaceAsync(id, user.Id);
            return result.ToHttpResult(_ => Results.NoContent());
        });

        group.MapPost("/{id:guid}/members", async (Guid id, RequestAddMember request, HttpContext context, ISessionHandler sessionHandler, ISpaceUseCase spaceUseCase) =>
        {
            var user = await sessionHandler.GetCurrentUserAsync(context);
            if (user == null) return Results.Unauthorized();

            var (isValid, errors) = HttpValidator.Validate(request);
            if (!isValid) return Results.BadRequest(new { message = "Dados inválidos.", errors });

            var result = await spaceUseCase.AddMemberAsync(id, user.Id, request.Username);
            return result.ToHttpResult(_ => Results.Ok(new { message = "Membro adicionado com sucesso." }));
        });

        group.MapDelete("/{id:guid}/members/{userId:guid}", async (Guid id, Guid userId, HttpContext context, ISessionHandler sessionHandler, ISpaceUseCase spaceUseCase) =>
        {
            var user = await sessionHandler.GetCurrentUserAsync(context);
            if (user == null) return Results.Unauthorized();

            var result = await spaceUseCase.RemoveMemberAsync(id, user.Id, userId);
            return result.ToHttpResult(_ => Results.NoContent());
        });

        group.MapGet("/{id:guid}/members", async (Guid id, HttpContext context, ISessionHandler sessionHandler, ISpaceUseCase spaceUseCase) =>
        {
            var user = await sessionHandler.GetCurrentUserAsync(context);
            if (user == null) return Results.Unauthorized();

            var result = await spaceUseCase.GetMembersAsync(id, user.Id);
            return result.ToHttpResult(res => res.ToAdminResponse());
        });

        return group;
    }
}
