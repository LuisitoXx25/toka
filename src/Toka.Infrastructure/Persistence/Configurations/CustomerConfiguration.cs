using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Toka.Domain.Customers;

namespace Toka.Infrastructure.Persistence.Configurations;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> b)
    {
        b.ToTable("customers");
        b.HasKey(c => c.Id);
        b.Property(c => c.FirstName).HasMaxLength(100).IsRequired();
        b.Property(c => c.LastName).HasMaxLength(100).IsRequired();
        b.Property(c => c.Email).HasMaxLength(254).IsRequired();
        b.Property(c => c.Phone).HasMaxLength(20);
        b.HasIndex(c => c.Email).IsUnique();
    }
}
