using Microsoft.EntityFrameworkCore;
using Toka.Application.Abstractions;
using Toka.Domain.Customers;
using Toka.Domain.Orders;
using Toka.Domain.Products;

namespace Toka.Infrastructure.Persistence.Repositories;

internal sealed class CustomerRepository(AppDbContext db) : ICustomerRepository
{
    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct) => db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
    public Task<Customer?> GetByEmailAsync(string normalizedEmail, CancellationToken ct) => db.Customers.FirstOrDefaultAsync(c => c.Email == normalizedEmail, ct);
    public void Add(Customer customer) => db.Customers.Add(customer);
}

internal sealed class ProductRepository(AppDbContext db) : IProductRepository
{
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct) => db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
    public async Task<IReadOnlyList<Product>> ListAsync(CancellationToken ct) => await db.Products.AsNoTracking().OrderBy(p => p.Name).ToListAsync(ct);
}

internal sealed class OrderRepository(AppDbContext db) : IOrderRepository
{
    public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct) => db.Orders.Include(o => o.Attempts).FirstOrDefaultAsync(o => o.Id == id, ct);
    public Task<Order?> GetByIdempotencyKeyAsync(string key, CancellationToken ct) => db.Orders.Include(o => o.Attempts).FirstOrDefaultAsync(o => o.IdempotencyKey == key, ct);
    public void Add(Order order) => db.Orders.Add(order);
}
