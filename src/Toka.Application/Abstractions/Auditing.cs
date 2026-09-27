namespace Toka.Application.Abstractions;

public sealed record AuditEntry(DateTimeOffset OccurredAt, string EventType, string Description, string? CorrelationId, string? Data);

public interface IAuditLog
{
    /// <summary>
    /// Queues an audit event; it is saved with the next <see cref="IUnitOfWork.SaveChangesAsync"/>,
    /// so it commits or rolls back together with the business change. <paramref name="data"/> must never contain card data.
    /// </summary>
    void Record(string eventType, string description, string entityType, Guid entityId, object? data = null);

    Task<IReadOnlyList<AuditEntry>> GetForEntityAsync(Guid entityId, CancellationToken ct);
}

/// <summary>Correlation id of the current request, used to link logs and audit events.</summary>
public interface ICorrelationContext
{
    string? CorrelationId { get; }
}

public static class AuditEvents
{
    public const string CustomerRegistered = "customer.registered";
    public const string OrderCreated = "order.created";
    public const string StockReserved = "inventory.reserved";
    public const string StockReleased = "inventory.released";
    public const string PaymentAttempted = "payment.attempted";
    public const string OrderPaid = "order.paid";
    public const string OrderDeclined = "order.payment_declined";
    public const string OrderPaymentFailed = "order.payment_failed";
    public const string PaymentRetryRequested = "order.payment_retry_requested";
}
