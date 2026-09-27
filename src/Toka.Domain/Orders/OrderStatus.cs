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
    /// <summary>Formal Spanish label for the UI and messages.</summary>
    public static string ToDisplayName(this OrderStatus status) => status switch
    {
        OrderStatus.PendingPayment => "Pago en proceso",
        OrderStatus.Paid => "Pago aprobado",
        OrderStatus.PaymentDeclined => "Pago rechazado",
        OrderStatus.PaymentFailed => "Pago no procesado",
        _ => status.ToString(),
    };
}
