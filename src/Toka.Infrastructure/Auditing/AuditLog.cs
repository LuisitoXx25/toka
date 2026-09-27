using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Toka.Application.Abstractions;
using Toka.Domain.Auditing;
using Toka.Infrastructure.Persistence;

namespace Toka.Infrastructure.Auditing;

internal sealed class AuditLog(AppDbContext db, ICorrelationContext correlation, TimeProvider clock) : IAuditLog
{
    public void Record(string eventType, string description, string entityType, Guid entityId, object? data = null) =>
        db.AuditEvents.Add(new AuditEvent(
            eventType, description, entityType, entityId, correlation.CorrelationId,
            data is null ? null : JsonSerializer.Serialize(data), clock.GetUtcNow()));

    /// <summary>Events of the entity plus events of other entities that reference it (e.g. stock moves for an order).</summary>
    public async Task<IReadOnlyList<AuditEntry>> GetForEntityAsync(Guid entityId, CancellationToken ct)
    {
        var related = JsonSerializer.Serialize(new { OrderId = entityId });
        return await db.AuditEvents
            .AsNoTracking()
            .Where(e => e.EntityId == entityId || EF.Functions.JsonContains(e.Data!, related))
            .OrderBy(e => e.OccurredAt)
            .Select(e => new AuditEntry(e.OccurredAt, e.EventType, e.Description, e.CorrelationId, e.Data))
            .ToListAsync(ct);
    }
}
