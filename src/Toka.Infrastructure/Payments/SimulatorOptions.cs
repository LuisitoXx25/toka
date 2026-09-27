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
}
