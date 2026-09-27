namespace Toka.Domain.Orders;

/// <summary>IVA breakdown of a price that already includes tax (consumer prices in Mexico must show the final amount).</summary>
public readonly record struct TaxBreakdown(decimal Subtotal, decimal TaxRate, decimal TaxAmount, decimal Total)
{
    /// <summary>General IVA rate in Mexico.</summary>
    public const decimal StandardIvaRate = 0.16m;

    /// <summary>
    /// Splits a tax-included total. The subtotal is rounded to cents and the tax is the remainder,
    /// so <c>Subtotal + TaxAmount == Total</c> always holds exactly.
    /// </summary>
    public static TaxBreakdown FromTaxIncludedTotal(decimal total, decimal taxRate = StandardIvaRate)
    {
        var subtotal = Math.Round(total / (1 + taxRate), 2, MidpointRounding.AwayFromZero);
        return new TaxBreakdown(subtotal, taxRate, total - subtotal, total);
    }
}
