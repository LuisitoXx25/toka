using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Toka.Domain.Auditing;

namespace Toka.Infrastructure.Persistence.Configurations;

internal sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> b)
    {
        b.ToTable("audit_events");
        b.HasKey(e => e.Id);
        b.Property(e => e.EventType).HasMaxLength(100).IsRequired();
        b.Property(e => e.Description).HasMaxLength(500).IsRequired();
        b.Property(e => e.EntityType).HasMaxLength(50).IsRequired();
        b.Property(e => e.CorrelationId).HasMaxLength(100);
        b.Property(e => e.Data).HasColumnType("jsonb");
        b.HasIndex(e => new { e.EntityId, e.OccurredAt });
    }
}
