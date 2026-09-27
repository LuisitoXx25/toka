namespace Toka.Domain.Orders;

public enum OrderStatus
{
    /// <summary>Stock reserved, payment in progress.</summary>
    PendingPayment = 0,
    Paid = 1,
    /// <summary>Issuer declined the card. Stock released; payment can be retried with another card.</summary>
    PaymentDeclined = 2,
    /// <summary>Gateway unavailable after all retries. Stock released; payment can be retried.</summary>
    PaymentFailed = 3,
}
