using Toka.Domain.Customers;
using Toka.Domain.Orders;
using Toka.Domain.Products;

namespace Toka.Application.Abstractions;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Customer?> GetByEmailAsync(string normalizedEmail, CancellationToken ct);
    void Add(Customer customer);
}

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Product>> ListAsync(CancellationToken ct);
}

public interface IOrderRepository
{
    /// <summary>Loads the order with its payment attempts.</summary>
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Order?> GetByIdempotencyKeyAsync(string key, CancellationToken ct);
    void Add(Order order);
}

public interface IUnitOfWork
{
    /// <summary>Persists all pending changes in a single transaction.</summary>
    /// <exception cref="Common.ConcurrencyConflictException">Another request changed the same data.</exception>
    Task SaveChangesAsync(CancellationToken ct);
}
