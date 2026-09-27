using FluentValidation;

namespace Toka.Application.Common;

internal static class ValidationExtensions
{
    /// <summary>Runs the validator and returns a validation <see cref="Error"/> grouped by field, or null when valid.</summary>
    public static async Task<Error?> ValidateToErrorAsync<T>(this IValidator<T> validator, T instance, CancellationToken ct)
    {
        var result = await validator.ValidateAsync(instance, ct);
        if (result.IsValid) return null;

        var details = result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());
        return new Error("validation", "Uno o más campos no son válidos.", ErrorType.Validation, details);
    }
}
