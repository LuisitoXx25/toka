using Microsoft.Extensions.Options;

namespace Toka.Application.Payments;

public sealed class InstallmentOptions
{
    public const string Section = "Installments";

    /// <summary>Available MSI plans and the minimum purchase for each. Applies to any card; there are no per-bank rules.</summary>
    public List<InstallmentPlanOption> Plans { get; set; } = [];
}

public sealed class InstallmentPlanOption
{
    public int Months { get; set; }
    public decimal MinimumAmount { get; set; }
}

/// <param name="Months">1 means a single payment.</param>
public sealed record InstallmentPlan(int Months, string Label, decimal MonthlyPayment, decimal MinimumAmount);

public sealed class InstallmentPolicy(IOptions<InstallmentOptions> options)
{
    private IEnumerable<InstallmentPlanOption> Configured =>
        options.Value.Plans.Where(p => p.Months > 1).OrderBy(p => p.Months);

    /// <summary>Plans available for a purchase amount: single payment plus every MSI plan whose minimum is met.</summary>
    public IReadOnlyList<InstallmentPlan> PlansFor(decimal amount) =>
        Configured
            .Where(p => amount >= p.MinimumAmount)
            .Select(p => Plan(p.Months, amount, p.MinimumAmount))
            .Prepend(Plan(1, amount, 0))
            .ToList();

    /// <summary>Null when the plan is allowed; otherwise the reason in Spanish.</summary>
    public string? Reject(int months, decimal amount)
    {
        if (months == 1) return null;
        var plan = Configured.FirstOrDefault(p => p.Months == months);
        if (plan is null) return $"El pago a {months} meses sin intereses no está disponible.";
        return amount < plan.MinimumAmount
            ? $"El pago a {months} meses sin intereses requiere una compra mínima de ${plan.MinimumAmount:N2}."
            : null;
    }

    /// <summary>User-facing label, e.g. "3 pagos de $1,166.33 sin intereses" or "1 pago de $3,499.00".</summary>
    public static string Describe(int months, decimal monthlyPayment) =>
        months == 1 ? $"1 pago de ${monthlyPayment:N2}" : $"{months} pagos de ${monthlyPayment:N2} sin intereses";

    private static InstallmentPlan Plan(int months, decimal amount, decimal minimum)
    {
        var monthly = Split(amount, months);
        return new InstallmentPlan(months, Describe(months, monthly), monthly, minimum);
    }

    private static decimal Split(decimal amount, int months) => Math.Round(amount / months, 2, MidpointRounding.AwayFromZero);
}
