using FluentValidation.Results;

namespace DevJourney.Application.Common;

internal static class ValidationExtensions
{
    public static Error ToError(this ValidationResult validation) =>
        Error.Validation(
            "Um ou mais campos são inválidos.",
            validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()));
}
