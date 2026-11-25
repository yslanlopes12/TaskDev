using System.ComponentModel.DataAnnotations;

namespace TaskDev.Api.Adapter.InBound.AdapterHttp.DTOs.Validation;

public static class HttpValidator
{
    public static (bool IsValid, Dictionary<string, string[]>? Errors) Validate(object model)
    {
        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        if (isValid)
        {
            return (true, null);
        }

        var errors = new Dictionary<string, string[]>();
        foreach (var error in results)
        {
            var key = error.MemberNames.FirstOrDefault() ?? "Request";
            if (!errors.ContainsKey(key))
            {
                errors[key] = [error.ErrorMessage ?? "Campo inválido."];
            }
            else
            {
                errors[key] = [.. errors[key], error.ErrorMessage ?? "Campo inválido."];
            }
        }

        return (false, errors);
    }
}
