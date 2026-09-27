using Toka.Application.Payments;

namespace Toka.UnitTests.Application;

public class CardRulesTests
{
    [Theory]
    [InlineData("4111111111111111", true)]
    [InlineData("5555555555554444", true)]
    [InlineData("378282246310005", true)]
    [InlineData("4111111111111112", false)]
    [InlineData("4111-1111-1111-1111", false)]
    [InlineData("", false)]
    public void Luhn(string number, bool expected) => Assert.Equal(expected, CardRules.PassesLuhn(number));

    [Theory]
    [InlineData("4111111111111111", "VISA")]
    [InlineData("5555555555554444", "MASTERCARD")]
    [InlineData("2223003122003222", "MASTERCARD")]
    [InlineData("378282246310005", "AMEX")]
    [InlineData("6011111111111117", "UNKNOWN")]
    [InlineData("2", "UNKNOWN")]
    public void Brand(string number, string expected) => Assert.Equal(expected, CardRules.DetectBrand(number));

    [Theory]
    [InlineData(9, 2026, false)]  // valid through the end of the expiry month
    [InlineData(8, 2026, true)]
    [InlineData(1, 2027, false)]
    [InlineData(12, 2025, true)]
    public void Expiry(int month, int year, bool expired) =>
        Assert.Equal(expired, CardRules.IsExpired(month, year, new DateTimeOffset(2026, 9, 26, 0, 0, 0, TimeSpan.Zero)));

    [Fact]
    public void Card_ToString_never_exposes_pan_or_cvv()
    {
        var text = new CardDetails("ANA", "4111111111111111", 12, 2030, "123").ToString();

        Assert.Equal("VISA ****1111", text);
    }
}
