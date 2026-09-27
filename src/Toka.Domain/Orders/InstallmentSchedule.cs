namespace Toka.Domain.Orders;

/// <summary>
/// Split of a total into interest-free monthly payments. Regular payments are truncated to the cent and the
/// first payment absorbs the difference, so the payments always add up to the total exactly
/// (e.g. 100.00 in 3 → 33.34 + 33.33 + 33.33).
/// </summary>
public readonly record struct InstallmentSchedule(int Months, decimal FirstPayment, decimal RegularPayment)
{
    public static InstallmentSchedule Of(decimal total, int months)
    {
        if (months < 1) throw new ArgumentOutOfRangeException(nameof(months));
        var regular = Math.Floor(total * 100 / months) / 100;
        return new InstallmentSchedule(months, total - regular * (months - 1), regular);
    }

    public bool FirstPaymentDiffers => FirstPayment != RegularPayment;
}
