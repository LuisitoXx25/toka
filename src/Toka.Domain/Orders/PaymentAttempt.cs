using Toka.Domain.Common;

namespace Toka.Domain.Orders;

public sealed class PaymentAttempt : Entity
{
    public Guid OrderId { get; private set; }
    public int AttemptNumber { get; private set; }
    public PaymentOutcome Outcome { get; private set; }
    public string ResponseCode { get; private set; } = null!;
    public string? Message { get; private set; }
    public string? AuthorizationCode { get; private set; }
    public string CardLast4 { get; private set; } = null!;
    public string CardBrand { get; private set; } = null!;
    public CardType CardType { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }

    private PaymentAttempt() { }

    internal PaymentAttempt(Guid orderId, int attemptNumber, PaymentOutcome outcome, string responseCode, string? message,
        string? authorizationCode, string cardLast4, string cardBrand, CardType cardType, DateTimeOffset occurredAt)
    {
        OrderId = orderId;
        AttemptNumber = attemptNumber;
        Outcome = outcome;
        ResponseCode = responseCode;
        Message = message;
        AuthorizationCode = authorizationCode;
        CardLast4 = cardLast4;
        CardBrand = cardBrand;
        CardType = cardType;
        OccurredAt = occurredAt;
    }
}
