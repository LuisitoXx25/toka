using Microsoft.EntityFrameworkCore;
using Toka.Domain.Auditing;
using Toka.Domain.Customers;
using Toka.Domain.Orders;
using Toka.Domain.Products;

namespace Toka.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<PaymentAttempt> PaymentAttempts => Set<PaymentAttempt>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Ids are generated in the domain (Guid v7). Without this, EF assumes a non-empty key means an existing
        // row and issues an UPDATE for children added to a loaded aggregate (e.g. a new payment attempt).
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
            entity.FindPrimaryKey()?.Properties.Single().ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never;
    }
}
