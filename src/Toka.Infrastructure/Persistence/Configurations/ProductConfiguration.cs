using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Toka.Domain.Products;

namespace Toka.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.ToTable("products", t => t.HasCheckConstraint("ck_products_stock_non_negative", "stock >= 0"));
        b.HasKey(p => p.Id);
        b.Property(p => p.Sku).HasMaxLength(50).IsRequired();
        b.Property(p => p.Name).HasMaxLength(200).IsRequired();
        b.Property(p => p.Description).HasMaxLength(1000);
        b.Property(p => p.Price).HasPrecision(12, 2);
        b.Property(p => p.Currency).HasMaxLength(3).IsFixedLength();
        // UPDATE ... WHERE version = @original: a concurrent stock change makes the update affect 0 rows.
        b.Property(p => p.Version).IsConcurrencyToken();
        b.HasIndex(p => p.Sku).IsUnique();
        b.HasData(SeedData.Products);
    }
}
