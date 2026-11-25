using TaskDev.Api.Domain.Core.Entities;

namespace TaskDev.Api.Domain.Core.Models;

public sealed class DashboardModel
{
    public int TodayCount { get; init; }
    public int PendingCount { get; init; }
    public int PlannedCount { get; init; }
    public int DoneCount { get; init; }
    public IReadOnlyList<TaskItem> TodayTasks { get; init; } = [];
    public IReadOnlyList<TaskItem> PendingTasks { get; init; } = [];
    public IReadOnlyList<TaskItem> PlannedTasks { get; init; } = [];
    public IReadOnlyList<TaskItem> DoneTasks { get; init; } = [];
}

public sealed class ProgressModel
{
    public int TotalXp { get; init; }
    public int Level { get; init; }
    public string Rank { get; init; } = "E";
    public int XpInCurrentLevel { get; init; }
    public int XpForNextLevel { get; init; } = 100;
    public int XpToNextLevel { get; init; }
}

public sealed class RecentXpModel
{
    public Guid Id { get; init; }
    public int Points { get; init; }
    public string? TaskTitle { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class SuggestedTaskModel
{
    public Guid? TaskId { get; init; }
    public Guid? SpaceId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Status { get; init; }
    public string Priority { get; init; } = "MEDIUM";
    public string? DueDate { get; init; }
    public IReadOnlyList<string> Subtasks { get; init; } = [];
    public IReadOnlyList<string>? ChangedFields { get; init; }
}

public sealed record AgentChatMessageModel(string Role, string Content);

public sealed record AgentChatPollModel(
    string Question,
    bool MultiSelect,
    IReadOnlyList<string> Options,
    bool AllowCustomOption = true);

public sealed record AgentRawCompletion(
    string Text,
    string? ToolName = null,
    string? ToolArgumentsJson = null,
    string? FinishReason = null);

public sealed class AgentChatResponseModel
{
    public string Reply { get; init; } = string.Empty;
    public bool IsAgentAction { get; init; }
    public string? ActionType { get; init; }
    public SuggestedTaskModel? SuggestedTask { get; init; }
    public bool NeedsSpace { get; init; }
    public AgentChatPollModel? Poll { get; init; }
}

public sealed class AuthSessionModel
{
    public Guid UserId { get; init; }
    public Guid Id => UserId;
    public int AuthVersion { get; init; }
    public string Role { get; init; } = "MEMBER";
    public string Username { get; init; } = string.Empty;
}
