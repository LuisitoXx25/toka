using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Toka.Domain.Orders;

namespace Toka.Infrastructure.Persistence.Configurations;

internal sealed class PaymentAttemptConfiguration : IEntityTypeConfiguration<PaymentAttempt>
{
    public void Configure(EntityTypeBuilder<PaymentAttempt> b)
    {
        b.ToTable("payment_attempts");
        b.HasKey(a => a.Id);
        b.Property(a => a.Outcome).HasConversion<string>().HasMaxLength(20);
        b.Property(a => a.ResponseCode).HasMaxLength(50).IsRequired();
        b.Property(a => a.Message).HasMaxLength(500);
        b.Property(a => a.AuthorizationCode).HasMaxLength(50);
        b.Property(a => a.CardLast4).HasMaxLength(4).IsFixedLength();
        b.Property(a => a.CardBrand).HasMaxLength(20);
        b.Property(a => a.CardType).HasConversion<string>().HasMaxLength(10);
        b.HasIndex(a => new { a.OrderId, a.AttemptNumber }).IsUnique();
    }
}
