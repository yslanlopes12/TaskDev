namespace TaskDev.Api.Domain.Core.Enums;

public enum EnumStatus
{
    Success,
    BusinessError,
    ValidationError,
    NotFound,
    Unauthorized,
    Forbidden,
    SystemError
}

public static class EnumRole
{
    public const string Admin = "ADMIN";
    public const string Operator = "OPERATOR";
    public const string Member = "MEMBER";

    public static bool IsValid(string role) =>
        role is Admin or Operator or Member;
}

public static class EnumTaskStatus
{
    public const string Pending = "PENDING";
    public const string InProgress = "IN_PROGRESS";
    public const string Done = "DONE";

    public static bool IsValid(string status) =>
        status is Pending or InProgress or Done;
}

public static class EnumTaskPriority
{
    public const string Low = "LOW";
    public const string Medium = "MEDIUM";
    public const string High = "HIGH";
    public const string Urgent = "URGENT";

    public static bool IsValid(string priority) =>
        priority is Low or Medium or High or Urgent;
}

public static class EnumSpaceVisibility
{
    public const string Private = "PRIVATE";
    public const string Shared = "SHARED";

    public static bool IsValid(string visibility) =>
        visibility is Private or Shared;
}

public static class EnumAgentProviderType
{
    public const string OpenAiCompatible = "OPENAI_COMPATIBLE";
    public const string Anthropic = "ANTHROPIC";
    public const string Gemini = "GEMINI";
    public const string Webhook = "WEBHOOK";

    public static bool IsValid(string type) =>
        type is OpenAiCompatible or Anthropic or Gemini or Webhook;
}
