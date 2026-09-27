using Toka.Domain.Orders;

namespace Toka.UnitTests.Domain;

public class TaxBreakdownTests
{
    [Theory]
    [InlineData(3499.00, 3016.38, 482.62)]
    [InlineData(116.00, 100.00, 16.00)]
    [InlineData(0.01, 0.01, 0.00)]
    [InlineData(28999.00, 24999.14, 3999.86)]
    public void Splits_tax_included_total(decimal total, decimal subtotal, decimal tax)
    {
        var breakdown = TaxBreakdown.FromTaxIncludedTotal(total);

        Assert.Equal(subtotal, breakdown.Subtotal);
        Assert.Equal(tax, breakdown.TaxAmount);
        Assert.Equal(TaxBreakdown.StandardIvaRate, breakdown.TaxRate);
    }

    [Fact]
    public void Subtotal_plus_tax_always_equals_total()
    {
        for (var cents = 1; cents <= 100_000; cents += 7)
        {
            var total = cents / 100m;
            var breakdown = TaxBreakdown.FromTaxIncludedTotal(total);
            Assert.Equal(total, breakdown.Subtotal + breakdown.TaxAmount);
        }
    }
}
