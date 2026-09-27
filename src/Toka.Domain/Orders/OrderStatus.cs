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

public static class OrderStatusExtensions
{
    /// <summary>Spanish label for user-facing messages.</summary>
    public static string ToDisplayName(this OrderStatus status) => status switch
    {
        OrderStatus.PendingPayment => "pendiente de pago",
        OrderStatus.Paid => "pagada",
        OrderStatus.PaymentDeclined => "pago rechazado",
        OrderStatus.PaymentFailed => "pago fallido",
        _ => status.ToString(),
    };
}
