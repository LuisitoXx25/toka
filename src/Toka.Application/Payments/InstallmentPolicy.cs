using Microsoft.Extensions.Options;
using Toka.Domain.Orders;

namespace Toka.Application.Payments;

public sealed class InstallmentOptions
{
    public const string Section = "Installments";

    /// <summary>Available MSI plans and the minimum purchase for each. Any credit card qualifies; there are no per-bank rules.</summary>
    public List<InstallmentPlanOption> Plans { get; set; } = [];
}

public sealed class InstallmentPlanOption
{
    public int Months { get; set; }
    public decimal MinimumAmount { get; set; }
}

/// <param name="Months">1 means a single payment.</param>
/// <param name="FirstPayment">Absorbs the rounding difference; equal to <paramref name="MonthlyPayment"/> when the split is exact.</param>
public sealed record InstallmentPlan(int Months, string Label, decimal FirstPayment, decimal MonthlyPayment, decimal MinimumAmount);

public sealed class InstallmentPolicy(IOptions<InstallmentOptions> options)
{
    public const string DebitNotEligible = "Los meses sin intereses solo aplican con tarjeta de crédito. Con tarjeta de débito el pago es de contado.";

    private IEnumerable<InstallmentPlanOption> Configured =>
        options.Value.Plans.Where(p => p.Months > 1).OrderBy(p => p.Months);

    /// <summary>
    /// Plans available for a purchase: single payment plus every MSI plan whose minimum is met.
    /// Debit cards only get the single payment.
    /// </summary>
    public IReadOnlyList<InstallmentPlan> PlansFor(decimal amount, CardType cardType = CardType.Unknown) =>
        Configured
            .Where(p => cardType != CardType.Debit && amount >= p.MinimumAmount)
            .Select(p => Plan(p.Months, amount, p.MinimumAmount))
            .Prepend(Plan(1, amount, 0))
            .ToList();

    /// <summary>Null when the plan is allowed; otherwise the reason in Spanish.</summary>
    public string? Reject(int months, decimal amount, CardType cardType)
    {
        if (months == 1) return null;
        if (cardType == CardType.Debit) return DebitNotEligible;
        var plan = Configured.FirstOrDefault(p => p.Months == months);
        if (plan is null) return $"El pago a {months} meses sin intereses no está disponible.";
        return amount < plan.MinimumAmount
            ? $"El pago a {months} meses sin intereses requiere una compra mínima de ${plan.MinimumAmount:N2}."
            : null;
    }

    /// <summary>
    /// User-facing label: "1 pago de $3,499.00", "6 pagos de $583.17 sin intereses" or, when the split is not exact,
    /// "3 pagos de $1,166.33 sin intereses; el primero de $1,166.34".
    /// </summary>
    public static string Describe(InstallmentSchedule schedule)
    {
        if (schedule.Months == 1) return $"1 pago de ${schedule.FirstPayment:N2}";
        var text = $"{schedule.Months} pagos de ${schedule.RegularPayment:N2} sin intereses";
        return schedule.FirstPaymentDiffers ? $"{text}; el primero de ${schedule.FirstPayment:N2}" : text;
    }

    private static InstallmentPlan Plan(int months, decimal amount, decimal minimum)
    {
        var schedule = InstallmentSchedule.Of(amount, months);
        return new InstallmentPlan(months, Describe(schedule), schedule.FirstPayment, schedule.RegularPayment, minimum);
    }
}
