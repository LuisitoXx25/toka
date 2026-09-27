namespace Toka.Domain.Common;

internal static class Guard
{
    public static string NotBlank(string? value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("validation", $"El campo {field} es obligatorio.");
        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new DomainException("validation", $"El campo {field} debe tener como máximo {maxLength} caracteres.");
        return trimmed;
    }

    public static int Positive(int value, string field)
    {
        if (value <= 0)
            throw new DomainException("validation", $"El campo {field} debe ser mayor que cero.");
        return value;
    }
}
