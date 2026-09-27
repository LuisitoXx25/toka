using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Toka.Domain.Customers;
using Toka.Domain.Orders;
using Toka.Domain.Products;

namespace Toka.Infrastructure.Persistence.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.ToTable("orders", t =>
        {
            t.HasCheckConstraint("ck_orders_tax_breakdown", "subtotal + tax_amount = total");
            t.HasCheckConstraint("ck_orders_installments_positive", "installments >= 1");
        });
        b.HasKey(o => o.Id);
        b.Property(o => o.UnitPrice).HasPrecision(12, 2);
        b.Property(o => o.Subtotal).HasPrecision(12, 2);
        b.Property(o => o.TaxRate).HasPrecision(5, 4);
        b.Property(o => o.TaxAmount).HasPrecision(12, 2);
        b.Property(o => o.Total).HasPrecision(12, 2);
        b.Property(o => o.Currency).HasMaxLength(3).IsFixedLength();
        b.Property(o => o.Status).HasConversion<string>().HasMaxLength(30);
        b.Property(o => o.IdempotencyKey).HasMaxLength(100);
        b.Property(o => o.AuthorizationCode).HasMaxLength(50);
        b.Property(o => o.FailureReason).HasMaxLength(500);
        b.Ignore(o => o.CanRetryPayment);
        b.Ignore(o => o.NextAttemptNumber);
        b.Ignore(o => o.MonthlyPayment);
        b.Property(o => o.Installments).HasDefaultValue(1);

        // Unique key also protects against two concurrent requests with the same key.
        b.HasIndex(o => o.IdempotencyKey).IsUnique().HasFilter("idempotency_key IS NOT NULL");
        b.HasIndex(o => o.CustomerId);

        b.HasOne<Customer>().WithMany().HasForeignKey(o => o.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Product>().WithMany().HasForeignKey(o => o.ProductId).OnDelete(DeleteBehavior.Restrict);

        b.HasMany(o => o.Attempts).WithOne().HasForeignKey(a => a.OrderId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(o => o.Attempts).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
