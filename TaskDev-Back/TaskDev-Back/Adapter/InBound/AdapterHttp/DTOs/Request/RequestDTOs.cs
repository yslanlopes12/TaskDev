using System.ComponentModel.DataAnnotations;

namespace TaskDev.Api.Adapter.InBound.AdapterHttp.DTOs.Request;

public sealed record RequestLogin
{
    [Required(AllowEmptyStrings = false)]
    [MinLength(1)]
    public string Username { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    [MinLength(1)]
    public string Password { get; init; } = string.Empty;

    public RequestLogin() { }
    public RequestLogin(string username, string password)
    {
        Username = username;
        Password = password;
    }
}

public sealed record RequestRegister
{
    [Required]
    [StringLength(80, MinimumLength = 1)]
    public string FirstName { get; init; } = string.Empty;

    [Required]
    [StringLength(80, MinimumLength = 1)]
    public string LastName { get; init; } = string.Empty;

    [StringLength(100)]
    public string? Timezone { get; init; }

    [StringLength(100, MinimumLength = 6)]
    public string? Password { get; init; }

    public RequestRegister() { }
    public RequestRegister(string firstName, string lastName, string? timezone = null, string? password = null)
    {
        FirstName = firstName;
        LastName = lastName;
        Timezone = timezone;
        Password = password;
    }
}

public sealed record RequestCreateSpace
{
    [Required]
    [StringLength(120, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    public string? Icon { get; init; }
    public string? Color { get; init; }
    public string? Visibility { get; init; }

    public RequestCreateSpace() { }
    public RequestCreateSpace(string name, string? icon = null, string? color = null, string? visibility = null)
    {
        Name = name;
        Icon = icon;
        Color = color;
        Visibility = visibility;
    }
}

public sealed record RequestUpdateSpace
{
    [Required]
    [StringLength(120, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    public string? Icon { get; init; }
    public string? Color { get; init; }
    public string? Visibility { get; init; }

    public RequestUpdateSpace() { }
    public RequestUpdateSpace(string name, string? icon = null, string? color = null, string? visibility = null)
    {
        Name = name;
        Icon = icon;
        Color = color;
        Visibility = visibility;
    }
}

public sealed record RequestAddMember
{
    [Required(AllowEmptyStrings = false)]
    [MinLength(1)]
    public string Username { get; init; } = string.Empty;

    public RequestAddMember() { }
    public RequestAddMember(string username)
    {
        Username = username;
    }
}

public sealed record RequestCreateTask
{
    [Required]
    public Guid SpaceId { get; init; }

    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;

    public string? Description { get; init; }
    public string? Priority { get; init; }
    public string? DueDate { get; init; }
    public Guid? AssigneeUserId { get; init; }
    public IReadOnlyList<string>? Subtasks { get; init; }

    public RequestCreateTask() { }
    public RequestCreateTask(Guid spaceId, string title, string? description = null, string? priority = null, string? dueDate = null, Guid? assigneeUserId = null)
    {
        SpaceId = spaceId;
        Title = title;
        Description = description;
        Priority = priority;
        DueDate = dueDate;
        AssigneeUserId = assigneeUserId;
    }

    public RequestCreateTask(Guid spaceId, string title, string? description, string? priority, DateOnly? dueDate, Guid? assigneeUserId = null)
        : this(spaceId, title, description, priority, dueDate?.ToString("yyyy-MM-dd"), assigneeUserId)
    {
    }
}

public sealed record RequestUpdateTask
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;

    public string? Description { get; init; }
    public string? Status { get; init; }
    public string? Priority { get; init; }
    public string? DueDate { get; init; }
    public Guid? AssigneeUserId { get; init; }

    public RequestUpdateTask() { }
    public RequestUpdateTask(string title, string? description = null, string? status = null, string? priority = null, string? dueDate = null, Guid? assigneeUserId = null)
    {
        Title = title;
        Description = description;
        Status = status;
        Priority = priority;
        DueDate = dueDate;
        AssigneeUserId = assigneeUserId;
    }

    public RequestUpdateTask(string title, string? description, string? status, string? priority, DateOnly? dueDate, Guid? assigneeUserId = null)
        : this(title, description, status, priority, dueDate?.ToString("yyyy-MM-dd"), assigneeUserId)
    {
    }
}

public sealed record RequestMoveTask
{
    [Required]
    public string Status { get; init; } = string.Empty;

    [Required]
    public long Position { get; init; }

    public RequestMoveTask() { }
    public RequestMoveTask(string status, long position)
    {
        Status = status;
        Position = position;
    }
}

public sealed record RequestUpdateStatus
{
    [Required]
    public string Status { get; init; } = string.Empty;

    public RequestUpdateStatus() { }
    public RequestUpdateStatus(string status)
    {
        Status = status;
    }
}

public sealed record RequestCreateSubtask
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;

    public RequestCreateSubtask() { }
    public RequestCreateSubtask(string title)
    {
        Title = title;
    }
}

public sealed record RequestUpdateSubtask
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;

    public bool IsCompleted { get; init; }

    public RequestUpdateSubtask() { }
    public RequestUpdateSubtask(string title, bool isCompleted)
    {
        Title = title;
        IsCompleted = isCompleted;
    }
}

public sealed record RequestReorderSubtasks
{
    [Required]
    public IReadOnlyList<Guid> SubtaskIds { get; init; } = [];

    public RequestReorderSubtasks() { }
    public RequestReorderSubtasks(IReadOnlyList<Guid> subtaskIds)
    {
        SubtaskIds = subtaskIds;
    }
}

public sealed record RequestSaveAgentConfig
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [Required]
    public string EndpointUrl { get; init; } = string.Empty;

    [Required]
    public string ProviderType { get; init; } = string.Empty;

    public string? Model { get; init; }
    public string? ApiKey { get; init; }
    public bool IsEnabled { get; init; } = true;

    public RequestSaveAgentConfig() { }
    public RequestSaveAgentConfig(string name, string endpointUrl, string providerType, string? model = null, string? apiKey = null, bool isEnabled = true)
    {
        Name = name;
        EndpointUrl = endpointUrl;
        ProviderType = providerType;
        Model = model;
        ApiKey = apiKey;
        IsEnabled = isEnabled;
    }
}

public sealed record RequestTestAgentConnection
{
    public string? EndpointUrl { get; init; }
    public string? ApiKey { get; init; }
    public string? ProviderType { get; init; }
    public string? Model { get; init; }

    public RequestTestAgentConnection() { }
    public RequestTestAgentConnection(string? endpointUrl, string? apiKey, string? providerType, string? model)
    {
        EndpointUrl = endpointUrl;
        ApiKey = apiKey;
        ProviderType = providerType;
        Model = model;
    }
}

public sealed record RequestPreviewAgentTasks
{
    [Required]
    public string Prompt { get; init; } = string.Empty;

    public RequestPreviewAgentTasks() { }
    public RequestPreviewAgentTasks(string prompt)
    {
        Prompt = prompt;
    }
}

public sealed record RequestChatMessage(string Role, string Content);

public sealed record RequestAgentChat
{
    [Required]
    [MinLength(1)]
    public IReadOnlyList<RequestChatMessage> Messages { get; init; } = [];

    public Guid? SpaceId { get; init; }

    public RequestAgentChat() { }
    public RequestAgentChat(IReadOnlyList<RequestChatMessage> messages, Guid? spaceId = null)
    {
        Messages = messages;
        SpaceId = spaceId;
    }
}

public sealed record RequestBootstrapAdmin
{
    [Required(AllowEmptyStrings = false)]
    [MinLength(1)]
    public string FirstName { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    [MinLength(1)]
    public string LastName { get; init; } = string.Empty;

    public RequestBootstrapAdmin() { }
    public RequestBootstrapAdmin(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }
}

public sealed record RequestUpdateRole
{
    [Required]
    public string Role { get; init; } = string.Empty;

    public RequestUpdateRole() { }
    public RequestUpdateRole(string role)
    {
        Role = role;
    }
}

public sealed record RequestUpdateActive
{
    [Required]
    public bool IsActive { get; init; }

    public RequestUpdateActive() { }
    public RequestUpdateActive(bool isActive)
    {
        IsActive = isActive;
    }
}
