using System.ComponentModel.DataAnnotations;

namespace Meetly.Api.Errors;

/// <summary>
/// Запускает DataAnnotations, сгенерированные из OpenAPI-контракта.
/// Minimal API не применяет их автоматически без встроенного ProblemDetails,
/// формат которого не совпадает с контрактным ErrorBody.
/// </summary>
public static class RequestValidation
{
    public static bool TryValidate(object value, out object? details)
    {
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(
            value,
            new ValidationContext(value),
            results,
            validateAllProperties: true);

        if (valid)
        {
            details = null;
            return true;
        }

        var fields = results
            .SelectMany(result => result.MemberNames.DefaultIfEmpty("body")
                .Select(member => new
                {
                    Field = char.ToLowerInvariant(member[0]) + member[1..],
                    Message = result.ErrorMessage ?? "Некорректное значение."
                }))
            .GroupBy(error => error.Field)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.Message).ToArray());

        details = new { fields };
        return false;
    }
}
