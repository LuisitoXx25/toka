namespace Toka.Application.Payments;

public static class CardRules
{
    /// <summary>Luhn checksum used by all major card networks to catch typos.</summary>
    public static bool PassesLuhn(string number)
    {
        if (string.IsNullOrEmpty(number) || !number.All(char.IsAsciiDigit)) return false;

        var sum = 0;
        var doubleDigit = false;
        for (var i = number.Length - 1; i >= 0; i--)
        {
            var digit = number[i] - '0';
            if (doubleDigit)
            {
                digit *= 2;
                if (digit > 9) digit -= 9;
            }
            sum += digit;
            doubleDigit = !doubleDigit;
        }
        return sum % 10 == 0;
    }

    /// <summary>Card network from the number prefix (IIN ranges).</summary>
    public static string DetectBrand(string number) => number switch
    {
        ['4', ..] => "VISA",
        ['3', '4' or '7', ..] => "AMEX",
        ['5', >= '1' and <= '5', ..] => "MASTERCARD",
        ['2', ..] when number.Length >= 4 && int.TryParse(number.AsSpan(0, 4), out var prefix) && prefix is >= 2221 and <= 2720 => "MASTERCARD",
        _ => "UNKNOWN",
    };

    /// <summary>A card is valid through the last day of its expiry month.</summary>
    public static bool IsExpired(int month, int year, DateTimeOffset now) =>
        year < now.Year || (year == now.Year && month < now.Month);
}
