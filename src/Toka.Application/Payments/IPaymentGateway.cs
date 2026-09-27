using Toka.Domain.Orders;

namespace Toka.Application.Payments;

/// <summary>Port to the card acquirer. Today the only implementation is a simulator.</summary>
public interface IPaymentGateway
{
    /// <summary>Requests authorization for a charge. May throw on transport failures; callers treat that as transient.</summary>
    Task<PaymentResponse> AuthorizeAsync(PaymentRequest request, CancellationToken ct);
}

public sealed record PaymentRequest(Guid OrderId, int AttemptNumber, decimal Amount, string Currency, CardDetails Card);

public sealed record PaymentResponse(PaymentOutcome Outcome, string ResponseCode, string? Message, string? AuthorizationCode);

/// <summary>Raw card data. Kept in memory only for the current request; never persisted or logged.</summary>
public sealed record CardDetails(string HolderName, string Number, int ExpiryMonth, int ExpiryYear, string Cvv)
{
    public string Last4 => Number.Length >= 4 ? Number[^4..] : Number;
    public string Brand => CardRules.DetectBrand(Number);

    // Records print every property by default; this keeps the PAN and CVV out of logs.
    public override string ToString() => $"{Brand} ****{Last4}";
}
