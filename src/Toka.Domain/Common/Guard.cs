namespace Toka.Domain.Common;

internal static class Guard
{
    public static string NotBlank(string? value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("validation", $"{field} is required.");
        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new DomainException("validation", $"{field} must be at most {maxLength} characters.");
        return trimmed;
    }

    public static int Positive(int value, string field)
    {
        if (value <= 0)
            throw new DomainException("validation", $"{field} must be greater than zero.");
        return value;
    }
}
