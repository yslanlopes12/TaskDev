using TaskDev.Api.Domain.Core.Entities;
using TaskDev.Api.Domain.Core.Models;
using TaskDev.Api.Domain.Core.ValueObjects;

namespace TaskDev.Api.Domain.Ports.InBound;

public sealed record RegisterCommand(string FirstName, string LastName, string? Timezone = null, string? Password = null);
public sealed record RegisterResult(User User, string GeneratedPassword);

public sealed record LoginCommand(string Username, string Password);
public sealed record LoginResult(User User, int TotalXp, int Level, string Rank);

public interface IAuthUseCase
{
    Task<BaseReturn<RegisterResult>> RegisterAsync(RegisterCommand command, CancellationToken cancellationToken = default);
    Task<BaseReturn<LoginResult>> LoginAsync(LoginCommand command, CancellationToken cancellationToken = default);
    Task<BaseReturn<LoginResult>> GetMeAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed record CreateSpaceCommand(Guid UserId, string Name, string? Icon, string? Color, string Visibility);
public sealed record UpdateSpaceCommand(Guid SpaceId, Guid UserId, string Name, string? Icon, string? Color, string Visibility);

public interface ISpaceUseCase
{
    Task<BaseReturn<IReadOnlyList<Space>>> GetSpacesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<BaseReturn<Space>> GetByIdAsync(Guid spaceId, Guid userId, CancellationToken cancellationToken = default);
    Task<BaseReturn<Space>> CreateSpaceAsync(CreateSpaceCommand command, CancellationToken cancellationToken = default);
    Task<BaseReturn<Space>> UpdateSpaceAsync(UpdateSpaceCommand command, CancellationToken cancellationToken = default);
    Task<BaseReturn<bool>> DeleteSpaceAsync(Guid spaceId, Guid userId, CancellationToken cancellationToken = default);
    Task<BaseReturn<bool>> AddMemberAsync(Guid spaceId, Guid ownerUserId, string memberUsername, CancellationToken cancellationToken = default);
    Task<BaseReturn<bool>> RemoveMemberAsync(Guid spaceId, Guid ownerUserId, Guid targetUserId, CancellationToken cancellationToken = default);
    Task<BaseReturn<IReadOnlyList<User>>> GetMembersAsync(Guid spaceId, Guid userId, CancellationToken cancellationToken = default);
}

public sealed record CreateTaskCommand(Guid SpaceId, Guid CreatedByUserId, string Title, string? Description, string Priority, DateOnly? DueDate, Guid? AssigneeUserId, IReadOnlyList<string>? Subtasks = null);
public sealed record UpdateTaskCommand(Guid TaskId, Guid UserId, string Title, string? Description, string Status, string Priority, DateOnly? DueDate, Guid? AssigneeUserId);
public sealed record MoveTaskCommand(Guid TaskId, Guid UserId, string Status, long Position);
public sealed record CompleteTaskResult(bool XpAwarded, int PointsAwarded);

public interface ITaskUseCase
{
    Task<BaseReturn<IReadOnlyList<TaskItem>>> GetTasksAsync(Guid userId, Guid? spaceId, CancellationToken cancellationToken = default);
    Task<BaseReturn<TaskItem>> GetByIdAsync(Guid taskId, Guid userId, CancellationToken cancellationToken = default);
    Task<BaseReturn<TaskItem>> CreateTaskAsync(CreateTaskCommand command, CancellationToken cancellationToken = default);
    Task<BaseReturn<TaskItem>> UpdateTaskAsync(UpdateTaskCommand command, CancellationToken cancellationToken = default);
    Task<BaseReturn<bool>> DeleteTaskAsync(Guid taskId, Guid userId, CancellationToken cancellationToken = default);
    Task<BaseReturn<bool>> UpdateStatusAsync(Guid taskId, Guid userId, string status, CancellationToken cancellationToken = default);
    Task<BaseReturn<bool>> MoveTaskAsync(MoveTaskCommand command, CancellationToken cancellationToken = default);
    Task<BaseReturn<CompleteTaskResult>> CompleteTaskAsync(Guid taskId, Guid userId, CancellationToken cancellationToken = default);
    Task<BaseReturn<int>> UncompleteTaskAsync(Guid taskId, Guid userId, CancellationToken cancellationToken = default);
}

public sealed record CreateSubtaskCommand(Guid TaskId, Guid UserId, string Title);
public sealed record UpdateSubtaskCommand(Guid SubtaskId, Guid UserId, string Title, bool IsCompleted);
public sealed record ReorderSubtasksCommand(Guid TaskId, Guid UserId, IReadOnlyList<Guid> SubtaskIdsInOrder);
public sealed record ToggleSubtaskResult(Subtask Subtask, bool IsCompleted, int PointsAwarded, int PointsDeducted);

public interface ISubtaskUseCase
{
    Task<BaseReturn<IReadOnlyList<Subtask>>> GetByTaskAsync(Guid taskId, Guid userId, CancellationToken cancellationToken = default);
    Task<BaseReturn<Subtask>> CreateSubtaskAsync(CreateSubtaskCommand command, CancellationToken cancellationToken = default);
    Task<BaseReturn<Subtask>> UpdateSubtaskAsync(UpdateSubtaskCommand command, CancellationToken cancellationToken = default);
    Task<BaseReturn<ToggleSubtaskResult>> ToggleSubtaskAsync(Guid subtaskId, Guid userId, CancellationToken cancellationToken = default);
    Task<BaseReturn<bool>> DeleteSubtaskAsync(Guid subtaskId, Guid userId, CancellationToken cancellationToken = default);
    Task<BaseReturn<bool>> ReorderSubtasksAsync(ReorderSubtasksCommand command, CancellationToken cancellationToken = default);
}

public interface IDashboardUseCase
{
    Task<BaseReturn<DashboardModel>> GetDashboardAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<BaseReturn<ProgressModel>> GetProgressAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed record SaveAgentConfigCommand(Guid UserId, string Name, string EndpointUrl, string ProviderType, string? Model, string? ApiKey, bool IsEnabled);
public sealed record ChatAgentCommand(Guid UserId, IReadOnlyList<AgentChatMessageModel> Messages, Guid? SpaceId);

public interface IAgentUseCase
{
    Task<BaseReturn<AgentConfig?>> GetConfigAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<BaseReturn<AgentConfig>> SaveConfigAsync(SaveAgentConfigCommand command, CancellationToken cancellationToken = default);
    Task<BaseReturn<bool>> DeleteConfigAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<BaseReturn<bool>> TestConnectionAsync(Guid userId, string? endpointUrl, string? apiKey, string? providerType, string? model, CancellationToken cancellationToken = default);
    Task<BaseReturn<IReadOnlyList<SuggestedTaskModel>>> PreviewTasksAsync(Guid userId, string prompt, CancellationToken cancellationToken = default);
    Task<BaseReturn<AgentChatResponseModel>> ChatAsync(ChatAgentCommand command, CancellationToken cancellationToken = default);
    Task<BaseReturn<IReadOnlyList<string>>> ListModelsAsync(Guid userId, string? endpointUrl, string? apiKey, string? providerType, CancellationToken cancellationToken = default);
}

public sealed record BootstrapAdminCommand(string FirstName, string LastName);
public sealed record ResetPasswordResult(string Username, string NewPassword);

public interface IAdminUseCase
{
    Task<BaseReturn<RegisterResult>> BootstrapAdminAsync(BootstrapAdminCommand command, CancellationToken cancellationToken = default);
    Task<BaseReturn<IReadOnlyList<User>>> GetAllUsersAsync(Guid requestedByUserId, CancellationToken cancellationToken = default);
    Task<BaseReturn<ResetPasswordResult>> ResetPasswordAsync(Guid requestedByUserId, Guid targetUserId, CancellationToken cancellationToken = default);
    Task<BaseReturn<bool>> UpdateUserRoleAsync(Guid requestedByUserId, Guid targetUserId, string newRole, CancellationToken cancellationToken = default);
    Task<BaseReturn<bool>> UpdateUserActiveAsync(Guid requestedByUserId, Guid targetUserId, bool isActive, CancellationToken cancellationToken = default);
}
