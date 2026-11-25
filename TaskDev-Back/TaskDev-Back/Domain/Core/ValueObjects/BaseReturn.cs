using TaskDev.Api.Domain.Core.Enums;

namespace TaskDev.Api.Domain.Core.ValueObjects;

public sealed class BaseError
{
    public string Message { get; init; } = string.Empty;
    public string? Code { get; init; }
    public Dictionary<string, string[]>? Details { get; init; }

    public BaseError(string message, string? code = null, Dictionary<string, string[]>? details = null)
    {
        Message = message;
        Code = code;
        Details = details;
    }
}

public readonly struct BaseReturn<T>
{
    public EnumStatus Status { get; init; }
    public T? SuccessObject { get; init; }
    public BaseError? ErrorObject { get; init; }

    public bool IsSuccess => Status == EnumStatus.Success;

    private BaseReturn(EnumStatus status, T? successObject, BaseError? errorObject)
    {
        Status = status;
        SuccessObject = successObject;
        ErrorObject = errorObject;
    }

    public static BaseReturn<T> Success(T value) =>
        new(EnumStatus.Success, value, null);

    public static BaseReturn<T> BusinessError(BaseError error) =>
        new(EnumStatus.BusinessError, default, error);

    public static BaseReturn<T> BusinessError(string message, string? code = null) =>
        new(EnumStatus.BusinessError, default, new BaseError(message, code));

    public static BaseReturn<T> ValidationError(string message, Dictionary<string, string[]>? details = null) =>
        new(EnumStatus.ValidationError, default, new BaseError(message, "VALIDATION_ERROR", details));

    public static BaseReturn<T> NotFound(string message) =>
        new(EnumStatus.NotFound, default, new BaseError(message, "NOT_FOUND"));

    public static BaseReturn<T> Unauthorized(string message = "Não autenticado.") =>
        new(EnumStatus.Unauthorized, default, new BaseError(message, "UNAUTHORIZED"));

    public static BaseReturn<T> Forbidden(string message = "Acesso negado.") =>
        new(EnumStatus.Forbidden, default, new BaseError(message, "FORBIDDEN"));

    public static BaseReturn<T> SystemError(BaseError error) =>
        new(EnumStatus.SystemError, default, error);

    public static BaseReturn<T> SystemError(string message) =>
        new(EnumStatus.SystemError, default, new BaseError(message, "SYSTEM_ERROR"));
}
