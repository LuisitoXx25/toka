namespace Toka.Infrastructure.Payments;

public sealed class SimulatorOptions
{
    public const string Section = "PaymentSimulator";

    /// <summary>Artificial latency of every authorization call.</summary>
    public TimeSpan Latency { get; set; } = TimeSpan.FromMilliseconds(150);

    /// <summary>Max time to wait for the acquirer before treating the call as a transient error.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>Amounts above this are declined as exceeding the card limit.</summary>
    public decimal CardLimit { get; set; } = 100_000m;

    /// <summary>BINs (first 6 digits) of debit cards. Test cards: 4000 0566 5566 5556 (Visa), 5200 8282 8282 8210 (Mastercard).</summary>
    public HashSet<string> DebitBins { get; set; } = ["400005", "520082"];
}
