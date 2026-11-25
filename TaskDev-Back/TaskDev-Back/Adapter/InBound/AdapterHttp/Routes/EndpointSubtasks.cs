using TaskDev.Api.Adapter.InBound.AdapterHttp.DTOs.Request;
using TaskDev.Api.Adapter.InBound.AdapterHttp.DTOs.Validation;
using TaskDev.Api.Adapter.InBound.AdapterHttp.Mapper;
using TaskDev.Api.Domain.Ports.InBound;
using TaskDev.Api.Infra.Configuration.InBound;

namespace TaskDev.Api.Adapter.InBound.AdapterHttp.Routes;

public static class EndpointSubtasks
{
    public static void MapEndpointSubtasks(this IEndpointRouteBuilder routes)
    {
        var taskSubtasksGroup = routes.MapGroup("/api/tasks/{taskId:guid}/subtasks").WithTags("Subtasks");

        taskSubtasksGroup.MapGet("/", async (Guid taskId, HttpContext context, ISessionHandler sessionHandler, ISubtaskUseCase subtaskUseCase) =>
        {
            var user = await sessionHandler.GetCurrentUserAsync(context);
            if (user == null) return Results.Unauthorized();

            var result = await subtaskUseCase.GetByTaskAsync(taskId, user.Id);
            return result.ToHttpResult(res => res.ToResponse());
        });

        taskSubtasksGroup.MapPost("/", async (Guid taskId, RequestCreateSubtask request, HttpContext context, ISessionHandler sessionHandler, ISubtaskUseCase subtaskUseCase) =>
        {
            var user = await sessionHandler.GetCurrentUserAsync(context);
            if (user == null) return Results.Unauthorized();

            var (isValid, errors) = HttpValidator.Validate(request);
            if (!isValid) return Results.BadRequest(new { message = "Dados inválidos.", errors });

            var result = await subtaskUseCase.CreateSubtaskAsync(request.ToCommand(taskId, user.Id));
            return result.ToHttpResult(res => res.ToResponse(), response => Results.Created($"/api/subtasks/{response.Id}", response));
        });

        taskSubtasksGroup.MapPatch("/reorder", async (Guid taskId, RequestReorderSubtasks request, HttpContext context, ISessionHandler sessionHandler, ISubtaskUseCase subtaskUseCase) =>
        {
            var user = await sessionHandler.GetCurrentUserAsync(context);
            if (user == null) return Results.Unauthorized();

            var (isValid, errors) = HttpValidator.Validate(request);
            if (!isValid) return Results.BadRequest(new { message = "Dados inválidos.", errors });

            var result = await subtaskUseCase.ReorderSubtasksAsync(request.ToCommand(taskId, user.Id));
            return result.ToHttpResult(_ => Results.Ok(new { message = "Subtarefas reordenadas com sucesso." }));
        });

        var subtasksDirectGroup = routes.MapGroup("/api/subtasks").WithTags("Subtasks");

        subtasksDirectGroup.MapPatch("/{id:guid}/toggle", async (Guid id, HttpContext context, ISessionHandler sessionHandler, ISubtaskUseCase subtaskUseCase) =>
        {
            var user = await sessionHandler.GetCurrentUserAsync(context);
            if (user == null) return Results.Unauthorized();

            var result = await subtaskUseCase.ToggleSubtaskAsync(id, user.Id);
            return result.ToHttpResult(res => res.ToResponse());
        });

        async Task<IResult> UpdateSubtaskHandler(Guid id, RequestUpdateSubtask request, HttpContext context, ISessionHandler sessionHandler, ISubtaskUseCase subtaskUseCase)
        {
            var user = await sessionHandler.GetCurrentUserAsync(context);
            if (user == null) return Results.Unauthorized();

            var (isValid, errors) = HttpValidator.Validate(request);
            if (!isValid) return Results.BadRequest(new { message = "Dados inválidos.", errors });

            var result = await subtaskUseCase.UpdateSubtaskAsync(request.ToCommand(id, user.Id));
            return result.ToHttpResult(res => res.ToResponse());
        }

        subtasksDirectGroup.MapPatch("/{id:guid}", UpdateSubtaskHandler);
        subtasksDirectGroup.MapPut("/{id:guid}", UpdateSubtaskHandler);

        subtasksDirectGroup.MapDelete("/{id:guid}", async (Guid id, HttpContext context, ISessionHandler sessionHandler, ISubtaskUseCase subtaskUseCase) =>
        {
            var user = await sessionHandler.GetCurrentUserAsync(context);
            if (user == null) return Results.Unauthorized();

            var result = await subtaskUseCase.DeleteSubtaskAsync(id, user.Id);
            return result.ToHttpResult(_ => Results.NoContent());
        });
    }
}
