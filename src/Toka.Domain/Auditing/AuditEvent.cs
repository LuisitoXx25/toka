using Toka.Domain.Common;

namespace Toka.Domain.Auditing;

/// <summary>Append-only business event log (bitácora).</summary>
public sealed class AuditEvent : Entity
{
    public DateTimeOffset OccurredAt { get; private set; }
    /// <summary>Stable technical code, e.g. <c>order.paid</c>.</summary>
    public string EventType { get; private set; } = null!;
    /// <summary>Human-readable description in Spanish, shown to users.</summary>
    public string Description { get; private set; } = null!;
    public string EntityType { get; private set; } = null!;
    public Guid EntityId { get; private set; }
    public string? CorrelationId { get; private set; }
    /// <summary>JSON payload. Never contains card data.</summary>
    public string? Data { get; private set; }

    private AuditEvent() { }

    public AuditEvent(string eventType, string description, string entityType, Guid entityId, string? correlationId, string? data, DateTimeOffset occurredAt)
    {
        EventType = eventType;
        Description = description;
        EntityType = entityType;
        EntityId = entityId;
        CorrelationId = correlationId;
        Data = data;
        OccurredAt = occurredAt;
    }
}
