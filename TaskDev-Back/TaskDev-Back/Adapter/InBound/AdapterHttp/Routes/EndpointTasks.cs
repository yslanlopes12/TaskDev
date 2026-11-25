using TaskDev.Api.Adapter.InBound.AdapterHttp.DTOs.Request;
using TaskDev.Api.Adapter.InBound.AdapterHttp.DTOs.Validation;
using TaskDev.Api.Adapter.InBound.AdapterHttp.Mapper;
using TaskDev.Api.Domain.Ports.InBound;
using TaskDev.Api.Infra.Configuration.InBound;

namespace TaskDev.Api.Adapter.InBound.AdapterHttp.Routes;

public static class EndpointTasks
{
    public static RouteGroupBuilder MapEndpointTasks(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/tasks").WithTags("Tasks");

        group.MapGet("/", async (Guid? spaceId, HttpContext context, ISessionHandler sessionHandler, ITaskUseCase taskUseCase) =>
        {
            var user = await sessionHandler.GetCurrentUserAsync(context);
            if (user == null) return Results.Unauthorized();

            var result = await taskUseCase.GetTasksAsync(user.Id, spaceId);
            return result.ToHttpResult(res => res.ToResponse());
        });

        group.MapPost("/", async (RequestCreateTask request, HttpContext context, ISessionHandler sessionHandler, ITaskUseCase taskUseCase) =>
        {
            var user = await sessionHandler.GetCurrentUserAsync(context);
            if (user == null) return Results.Unauthorized();

            var (isValid, errors) = HttpValidator.Validate(request);
            if (!isValid) return Results.BadRequest(new { message = "Dados inválidos.", errors });

            var result = await taskUseCase.CreateTaskAsync(request.ToCommand(user.Id));
            return result.ToHttpResult(res => res.ToResponse(), response => Results.Created($"/api/tasks/{response.Id}", response));
        });

        group.MapGet("/{id:guid}", async (Guid id, HttpContext context, ISessionHandler sessionHandler, ITaskUseCase taskUseCase) =>
        {
            var user = await sessionHandler.GetCurrentUserAsync(context);
            if (user == null) return Results.Unauthorized();

            var result = await taskUseCase.GetByIdAsync(id, user.Id);
            return result.ToHttpResult(res => res.ToResponse());
        });

        group.MapPut("/{id:guid}", async (Guid id, RequestUpdateTask request, HttpContext context, ISessionHandler sessionHandler, ITaskUseCase taskUseCase) =>
        {
            var user = await sessionHandler.GetCurrentUserAsync(context);
            if (user == null) return Results.Unauthorized();

            var (isValid, errors) = HttpValidator.Validate(request);
            if (!isValid) return Results.BadRequest(new { message = "Dados inválidos.", errors });

            var result = await taskUseCase.UpdateTaskAsync(request.ToCommand(id, user.Id));
            return result.ToHttpResult(res => res.ToResponse());
        });

        group.MapDelete("/{id:guid}", async (Guid id, HttpContext context, ISessionHandler sessionHandler, ITaskUseCase taskUseCase) =>
        {
            var user = await sessionHandler.GetCurrentUserAsync(context);
            if (user == null) return Results.Unauthorized();

            var result = await taskUseCase.DeleteTaskAsync(id, user.Id);
            return result.ToHttpResult(_ => Results.NoContent());
        });

        group.MapPatch("/{id:guid}/status", async (Guid id, RequestUpdateStatus request, HttpContext context, ISessionHandler sessionHandler, ITaskUseCase taskUseCase) =>
        {
            var user = await sessionHandler.GetCurrentUserAsync(context);
            if (user == null) return Results.Unauthorized();

            var (isValid, errors) = HttpValidator.Validate(request);
            if (!isValid) return Results.BadRequest(new { message = "Dados inválidos.", errors });

            var result = await taskUseCase.UpdateStatusAsync(id, user.Id, request.Status);
            return result.ToHttpResult(_ => Results.Ok(new { message = "Status atualizado com sucesso." }));
        });

        group.MapPatch("/{id:guid}/move", async (Guid id, RequestMoveTask request, HttpContext context, ISessionHandler sessionHandler, ITaskUseCase taskUseCase) =>
        {
            var user = await sessionHandler.GetCurrentUserAsync(context);
            if (user == null) return Results.Unauthorized();

            var (isValid, errors) = HttpValidator.Validate(request);
            if (!isValid) return Results.BadRequest(new { message = "Dados inválidos.", errors });

            var result = await taskUseCase.MoveTaskAsync(request.ToCommand(id, user.Id));
            return result.ToHttpResult(_ => Results.Ok(new { message = "Posição atualizada com sucesso." }));
        });

        group.MapPatch("/{id:guid}/complete", async (Guid id, HttpContext context, ISessionHandler sessionHandler, ITaskUseCase taskUseCase) =>
        {
            var user = await sessionHandler.GetCurrentUserAsync(context);
            if (user == null) return Results.Unauthorized();

            var result = await taskUseCase.CompleteTaskAsync(id, user.Id);
            return result.ToHttpResult(res => Results.Ok(new
            {
                message = "Tarefa concluída com sucesso.",
                xpAwarded = res.XpAwarded,
                points = res.PointsAwarded
            }));
        });

        group.MapPatch("/{id:guid}/uncomplete", async (Guid id, HttpContext context, ISessionHandler sessionHandler, ITaskUseCase taskUseCase) =>
        {
            var user = await sessionHandler.GetCurrentUserAsync(context);
            if (user == null) return Results.Unauthorized();

            var result = await taskUseCase.UncompleteTaskAsync(id, user.Id);
            return result.ToHttpResult(pointsDeducted => Results.Ok(new
            {
                message = "Tarefa desconcluída.",
                pointsDeducted
            }));
        });

        return group;
    }
}
