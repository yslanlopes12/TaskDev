using TaskDev.Api.Adapter.InBound.AdapterHttp.DTOs.Request;
using TaskDev.Api.Adapter.InBound.AdapterHttp.DTOs.Response;
using TaskDev.Api.Domain.Core.Entities;
using TaskDev.Api.Domain.Core.Enums;
using TaskDev.Api.Domain.Core.Models;
using TaskDev.Api.Domain.Core.ValueObjects;
using TaskDev.Api.Domain.Ports.InBound;

namespace TaskDev.Api.Adapter.InBound.AdapterHttp.Mapper;

public static class MapperRequestToDomain
{
    public static RegisterCommand ToCommand(this RequestRegister req) =>
        new(req.FirstName, req.LastName, req.Timezone, req.Password);

    public static LoginCommand ToCommand(this RequestLogin req) =>
        new(req.Username, req.Password);

    public static CreateSpaceCommand ToCommand(this RequestCreateSpace req, Guid userId) =>
        new(userId, req.Name, req.Icon, req.Color, req.Visibility ?? "PRIVATE");

    public static UpdateSpaceCommand ToCommand(this RequestUpdateSpace req, Guid spaceId, Guid userId) =>
        new(spaceId, userId, req.Name, req.Icon, req.Color, req.Visibility ?? "PRIVATE");

    private static DateOnly? ParseDueDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var datePart = raw.Split('T')[0].Trim();
        if (DateOnly.TryParse(datePart, out var d)) return d;
        if (DateTime.TryParse(raw, out var dt)) return DateOnly.FromDateTime(dt);
        return null;
    }

    public static CreateTaskCommand ToCommand(this RequestCreateTask req, Guid createdByUserId) =>
        new(req.SpaceId, createdByUserId, req.Title, req.Description, req.Priority ?? "MEDIUM", ParseDueDate(req.DueDate), req.AssigneeUserId, req.Subtasks);

    public static UpdateTaskCommand ToCommand(this RequestUpdateTask req, Guid taskId, Guid userId) =>
        new(taskId, userId, req.Title, req.Description, req.Status ?? "PENDING", req.Priority ?? "MEDIUM", ParseDueDate(req.DueDate), req.AssigneeUserId);

    public static MoveTaskCommand ToCommand(this RequestMoveTask req, Guid taskId, Guid userId) =>
        new(taskId, userId, req.Status, req.Position);

    public static CreateSubtaskCommand ToCommand(this RequestCreateSubtask req, Guid taskId, Guid userId) =>
        new(taskId, userId, req.Title);

    public static UpdateSubtaskCommand ToCommand(this RequestUpdateSubtask req, Guid subtaskId, Guid userId) =>
        new(subtaskId, userId, req.Title, req.IsCompleted);

    public static ReorderSubtasksCommand ToCommand(this RequestReorderSubtasks req, Guid taskId, Guid userId) =>
        new(taskId, userId, req.SubtaskIds);

    public static SaveAgentConfigCommand ToCommand(this RequestSaveAgentConfig req, Guid userId) =>
        new(userId, req.Name, req.EndpointUrl, req.ProviderType, req.Model, req.ApiKey, req.IsEnabled);

    public static BootstrapAdminCommand ToCommand(this RequestBootstrapAdmin req) =>
        new(req.FirstName, req.LastName);
}

public static class MapperDomainToHttp
{
    public static ResponseRegister ToResponse(this RegisterResult res) =>
        new(res.User.Id, res.User.FirstName, res.User.LastName, res.User.Username, res.GeneratedPassword, res.User.Role, "Guarde sua senha com segurança. Ela não será exibida novamente.");

    public static ResponseMe ToResponse(this LoginResult res) =>
        new(res.User.Id, res.User.FirstName, res.User.LastName, res.User.Username, res.User.Role, res.User.Timezone, res.Level, res.Rank, res.TotalXp, res.User.CreatedAt);

    public static ResponseSpace ToResponse(this Space space) =>
        new(space.Id, space.OwnerUserId, space.Name, space.Icon, space.Color, space.Visibility, space.CreatedAt, space.UpdatedAt);

    public static IReadOnlyList<ResponseSpace> ToResponse(this IReadOnlyList<Space> spaces) =>
        spaces.Select(ToResponse).ToList();

    public static ResponseTask ToResponse(this TaskItem task) =>
        new(task.Id, task.SpaceId, task.CreatedByUserId, task.AssigneeUserId, task.Title, task.Description, task.Status, task.Priority, task.DueDate, task.Position, task.XpAwarded, task.CompletedAt, task.CreatedAt, task.UpdatedAt, task.Subtasks.Select(ToResponse).ToList());

    public static IReadOnlyList<ResponseTask> ToResponse(this IReadOnlyList<TaskItem> tasks) =>
        tasks.Select(ToResponse).ToList();

    public static ResponseSubtask ToResponse(this Subtask subtask) =>
        new(subtask.Id, subtask.TaskId, subtask.Title, subtask.IsCompleted, subtask.Position, subtask.CreatedAt);

    public static ResponseToggleSubtask ToResponse(this ToggleSubtaskResult res) =>
        new(res.Subtask.Id, res.Subtask.TaskId, res.Subtask.Title, res.Subtask.IsCompleted, res.Subtask.Position, res.Subtask.CreatedAt, res.PointsAwarded, res.PointsDeducted);

    public static IReadOnlyList<ResponseSubtask> ToResponse(this IReadOnlyList<Subtask> subtasks) =>
        subtasks.Select(ToResponse).ToList();

    public static ResponseAgentConfig ToResponse(this AgentConfig config) =>
        new(
            config.Name,
            config.EndpointUrl,
            config.ProviderType,
            config.Model,
            config.IsEnabled,
            !string.IsNullOrWhiteSpace(config.EncryptedApiKey)
            );

    public static ResponseDashboard ToResponse(this DashboardModel model) =>
        new(model.TodayCount, model.PendingCount, model.PlannedCount, model.DoneCount, model.TodayTasks.ToResponse(), model.PendingTasks.ToResponse(), model.PlannedTasks.ToResponse(), model.DoneTasks.ToResponse());

    public static ResponseProgress ToResponse(this ProgressModel model) =>
        new(model.TotalXp, model.Level, model.Rank, model.XpInCurrentLevel, model.XpForNextLevel, model.XpToNextLevel);

    public static ResponseAdminUser ToAdminResponse(this User user) =>
        new(user.Id, user.FirstName, user.LastName, user.Username, user.Role, user.IsActive, user.CreatedAt);

    public static IReadOnlyList<ResponseAdminUser> ToAdminResponse(this IReadOnlyList<User> users) =>
        users.Select(ToAdminResponse).ToList();

    public static ResponseResetPassword ToResponse(this ResetPasswordResult res) =>
        new(res.Username, res.NewPassword, "Senha resetada com sucesso. Guarde a nova senha.");
}

public static class MapperErrorToHttp
{
    public static IResult ToHttpResult<TDomain, THttp>(this BaseReturn<TDomain> result, Func<TDomain, THttp> mapper, Func<THttp, IResult>? customSuccess = null)
    {
        return result.Status switch
        {
            EnumStatus.Success => customSuccess != null
                ? customSuccess(mapper(result.SuccessObject!))
                : Results.Ok(mapper(result.SuccessObject!)),
            EnumStatus.ValidationError => Results.BadRequest(new { message = result.ErrorObject?.Message ?? "Dados inválidos.", errors = result.ErrorObject?.Details }),
            EnumStatus.BusinessError => Results.UnprocessableEntity(new { message = result.ErrorObject?.Message ?? "Erro de negócio." }),
            EnumStatus.NotFound => Results.NotFound(new { message = result.ErrorObject?.Message ?? "Recurso não encontrado." }),
            EnumStatus.Unauthorized => Results.Unauthorized(),
            EnumStatus.Forbidden => Results.Forbid(),
            _ => Results.Problem(detail: result.ErrorObject?.Message ?? "Erro inesperado.", statusCode: 500)
        };
    }

    public static IResult ToHttpResult<TDomain>(this BaseReturn<TDomain> result, Func<TDomain, IResult>? customSuccess = null)
    {
        return result.Status switch
        {
            EnumStatus.Success => customSuccess != null
                ? customSuccess(result.SuccessObject!)
                : Results.Ok(result.SuccessObject),
            EnumStatus.ValidationError => Results.BadRequest(new { message = result.ErrorObject?.Message ?? "Dados inválidos.", errors = result.ErrorObject?.Details }),
            EnumStatus.BusinessError => Results.UnprocessableEntity(new { message = result.ErrorObject?.Message ?? "Erro de negócio." }),
            EnumStatus.NotFound => Results.NotFound(new { message = result.ErrorObject?.Message ?? "Recurso não encontrado." }),
            EnumStatus.Unauthorized => Results.Unauthorized(),
            EnumStatus.Forbidden => Results.Forbid(),
            _ => Results.Problem(detail: result.ErrorObject?.Message ?? "Erro inesperado.", statusCode: 500)
        };
    }
}
