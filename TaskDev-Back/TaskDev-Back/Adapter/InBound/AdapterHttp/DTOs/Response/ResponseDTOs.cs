namespace TaskDev.Api.Adapter.InBound.AdapterHttp.DTOs.Response;

public sealed record ResponseRegister(
    Guid Id,
    string FirstName,
    string LastName,
    string Username,
    string GeneratedPassword,
    string Role,
    string Message);

public sealed record ResponseMe(
    Guid Id,
    string FirstName,
    string LastName,
    string Username,
    string Role,
    string? Timezone,
    int Level,
    string Rank,
    int TotalXp,
    DateTimeOffset CreatedAt);

public sealed record ResponseSpace(
    Guid Id,
    Guid OwnerUserId,
    string Name,
    string? Icon,
    string? Color,
    string Visibility,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ResponseTask(
    Guid Id,
    Guid SpaceId,
    Guid CreatedByUserId,
    Guid? AssigneeUserId,
    string Title,
    string? Description,
    string Status,
    string Priority,
    DateOnly? DueDate,
    long Position,
    bool XpAwarded,
    DateTimeOffset? CompletedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<ResponseSubtask>? Subtasks = null);

public sealed record ResponseSubtask(
    Guid Id,
    Guid TaskId,
    string Title,
    bool IsCompleted,
    long Position,
    DateTimeOffset CreatedAt);

public sealed record ResponseToggleSubtask(
    Guid Id,
    Guid TaskId,
    string Title,
    bool IsCompleted,
    long Position,
    DateTimeOffset CreatedAt,
    int PointsAwarded,
    int PointsDeducted);

public sealed record ResponseAgentConfig(
    string Name,
    string EndpointUrl,
    string ProviderType,
    string? Model,
    bool IsEnabled,
    bool HasApiKey);

public sealed record ResponseSuggestedTask(
    string Title,
    string? Description,
    string Priority,
    string? DueDate,
    IReadOnlyList<string> Subtasks,
    Guid? SpaceId = null,
    Guid? TaskId = null,
    string? Status = null,
    IReadOnlyList<string>? ChangedFields = null);

public sealed record ResponsePreviewTasks(
    IReadOnlyList<ResponseSuggestedTask> Tasks);

public sealed record ResponseAgentPoll(
    string Question,
    bool MultiSelect,
    IReadOnlyList<string> Options,
    bool AllowCustomOption = true);

public sealed record ResponseAgentChat(
    string Reply,
    bool IsAgentAction,
    bool NeedsSpace,
    ResponseSuggestedTask? SuggestedTask,
    ResponseAgentPoll? Poll = null,
    string? ActionType = null);

public sealed record ResponseDashboard(
    int TodayCount,
    int PendingCount,
    int PlannedCount,
    int DoneCount,
    IReadOnlyList<ResponseTask> TodayTasks,
    IReadOnlyList<ResponseTask> PendingTasks,
    IReadOnlyList<ResponseTask> PlannedTasks,
    IReadOnlyList<ResponseTask> DoneTasks);

public sealed record ResponseProgress(
    int TotalXp,
    int Level,
    string Rank,
    int XpInCurrentLevel,
    int XpForNextLevel,
    int XpToNextLevel);

public sealed record ResponseAdminUser(
    Guid Id,
    string FirstName,
    string LastName,
    string Username,
    string Role,
    bool IsActive,
    DateTimeOffset CreatedAt);

public sealed record ResponseResetPassword(
    string Username,
    string NewPassword,
    string Message);
