using TaskDev.Api.Adapter.InBound.AdapterHttp.DTOs.Request;
using TaskDev.Api.Adapter.InBound.AdapterHttp.DTOs.Response;
using TaskDev.Api.Adapter.InBound.AdapterHttp.DTOs.Validation;
using TaskDev.Api.Adapter.InBound.AdapterHttp.Mapper;
using TaskDev.Api.Domain.Core.Models;
using TaskDev.Api.Domain.Ports.InBound;
using TaskDev.Api.Infra.Configuration.InBound;

namespace TaskDev.Api.Adapter.InBound.AdapterHttp.Routes;

public static class EndpointAgent
{
    public static RouteGroupBuilder MapEndpointAgent(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/agent").WithTags("Agent");

        group.MapGet("/config", async (HttpContext context, ISessionHandler sessionHandler, IAgentUseCase agentUseCase) =>
        {
            var user = await sessionHandler.GetCurrentUserAsync(context);
            if (user == null) return Results.Unauthorized();

            var result = await agentUseCase.GetConfigAsync(user.Id);
            if (!result.IsSuccess)
            {
                return result.ToHttpResult();
            }

            if (result.SuccessObject == null)
            {
                return Results.Ok((ResponseAgentConfig?)null);
            }

            return Results.Ok(result.SuccessObject.ToResponse());
        });

        group.MapPut("/config", async (RequestSaveAgentConfig request, HttpContext context, ISessionHandler sessionHandler, IAgentUseCase agentUseCase) =>
        {
            var user = await sessionHandler.GetCurrentUserAsync(context);
            if (user == null) return Results.Unauthorized();

            var (isValid, errors) = HttpValidator.Validate(request);
            if (!isValid) return Results.BadRequest(new { message = "Dados inválidos.", errors });

            var result = await agentUseCase.SaveConfigAsync(request.ToCommand(user.Id));
            return result.ToHttpResult(res => res.ToResponse());
        });

        group.MapDelete("/config", async (HttpContext context, ISessionHandler sessionHandler, IAgentUseCase agentUseCase) =>
        {
            var user = await sessionHandler.GetCurrentUserAsync(context);
            if (user == null) return Results.Unauthorized();

            var result = await agentUseCase.DeleteConfigAsync(user.Id);
            return result.ToHttpResult(_ => Results.NoContent());
        });

        group.MapPost("/test", async (RequestTestAgentConnection request, HttpContext context, ISessionHandler sessionHandler, IAgentUseCase agentUseCase) =>
        {
            var user = await sessionHandler.GetCurrentUserAsync(context);
            if (user == null) return Results.Unauthorized();

            var result = await agentUseCase.TestConnectionAsync(user.Id, request.EndpointUrl, request.ApiKey, request.ProviderType, request.Model);
            return result.ToHttpResult(_ => Results.Ok(new { success = true, message = "Conexão com o agente validada com sucesso!" }));
        });

        group.MapPost("/models", async (RequestTestAgentConnection request, HttpContext context, ISessionHandler sessionHandler, IAgentUseCase agentUseCase) =>
        {
            var user = await sessionHandler.GetCurrentUserAsync(context);
            if (user == null) return Results.Unauthorized();

            var result = await agentUseCase.ListModelsAsync(user.Id, request.EndpointUrl, request.ApiKey, request.ProviderType);
            return result.ToHttpResult(models => Results.Ok(new { models }));
        });

        group.MapPost("/preview", async (RequestPreviewAgentTasks request, HttpContext context, ISessionHandler sessionHandler, IAgentUseCase agentUseCase) =>
        {
            var user = await sessionHandler.GetCurrentUserAsync(context);
            if (user == null) return Results.Unauthorized();

            var (isValid, errors) = HttpValidator.Validate(request);
            if (!isValid) return Results.BadRequest(new { message = "Dados inválidos.", errors });

            var result = await agentUseCase.PreviewTasksAsync(user.Id, request.Prompt);
            return result.ToHttpResult(res => new ResponsePreviewTasks(
                res.Select(t => new ResponseSuggestedTask(t.Title, t.Description, t.Priority, t.DueDate, t.Subtasks, t.SpaceId)).ToList()
            ));
        });

        group.MapPost("/chat", async (RequestAgentChat request, HttpContext context, ISessionHandler sessionHandler, IAgentUseCase agentUseCase) =>
        {
            var user = await sessionHandler.GetCurrentUserAsync(context);
            if (user == null) return Results.Unauthorized();

            var (isValid, errors) = HttpValidator.Validate(request);
            if (!isValid) return Results.BadRequest(new { message = "Dados inválidos.", errors });

            var cmd = new ChatAgentCommand(
                user.Id,
                request.Messages.Select(m => new AgentChatMessageModel(m.Role, m.Content)).ToList(),
                request.SpaceId
            );

            var result = await agentUseCase.ChatAsync(cmd);
            return result.ToHttpResult(res => new ResponseAgentChat(
                res.Reply,
                res.IsAgentAction,
                res.NeedsSpace,
                res.SuggestedTask != null
                    ? new ResponseSuggestedTask(
                        res.SuggestedTask.Title,
                        res.SuggestedTask.Description,
                        res.SuggestedTask.Priority,
                        res.SuggestedTask.DueDate,
                        res.SuggestedTask.Subtasks,
                        res.SuggestedTask.SpaceId,
                        res.SuggestedTask.TaskId,
                        res.SuggestedTask.Status,
                        res.SuggestedTask.ChangedFields)
                    : null,
                res.Poll != null
                    ? new ResponseAgentPoll(
                        res.Poll.Question,
                        res.Poll.MultiSelect,
                        res.Poll.Options,
                        res.Poll.AllowCustomOption)
                    : null,
                res.ActionType
            ));
        });

        return group;
    }
}
