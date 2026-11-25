namespace TaskDev.Api.Domain.Core.Exceptions;

public class BusinessException : Exception
{
    public string? Code { get; }

    public BusinessException(string message, string? code = null) : base(message)
    {
        Code = code;
    }
}

public class ValidationException : Exception
{
    public Dictionary<string, string[]> Errors { get; }

    public ValidationException(string message, Dictionary<string, string[]>? errors = null) : base(message)
    {
        Errors = errors ?? new();
    }
}

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message = "Usuário não autenticado.") : base(message) { }
}

public class ForbiddenException : Exception
{
    public ForbiddenException(string message = "Acesso negado para este recurso.") : base(message) { }
}
