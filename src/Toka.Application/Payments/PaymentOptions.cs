namespace Toka.Application.Payments;

public sealed class PaymentOptions
{
    public const string Section = "Payments";

    /// <summary>Total authorization attempts, including the first. Only transient errors are retried.</summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>Exponential backoff base: waits base, 2×base, 4×base... between attempts.</summary>
    public TimeSpan BaseRetryDelay { get; set; } = TimeSpan.FromMilliseconds(200);
}
