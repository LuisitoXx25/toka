using Microsoft.Extensions.Options;
using Toka.Application.Abstractions;
using Toka.Application.Payments;
using Toka.Domain.Customers;
using Toka.Domain.Orders;
using Toka.Domain.Products;

namespace Toka.UnitTests.Fakes;

internal sealed class InMemoryStore : ICustomerRepository, IProductRepository, IOrderRepository, IUnitOfWork, IAuditLog
{
    public List<Customer> Customers { get; } = [];
    public List<Product> Products { get; } = [];
    public List<Order> Orders { get; } = [];
    public List<(string EventType, string Description, Guid EntityId)> Events { get; } = [];
    public int SaveCount { get; private set; }

    Task<Customer?> ICustomerRepository.GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Customers.FirstOrDefault(c => c.Id == id));
    public Task<Customer?> GetByEmailAsync(string email, CancellationToken ct) => Task.FromResult(Customers.FirstOrDefault(c => c.Email == email));
    public void Add(Customer customer) => Customers.Add(customer);

    Task<Product?> IProductRepository.GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Products.FirstOrDefault(p => p.Id == id));
    public Task<IReadOnlyList<Product>> ListAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<Product>>(Products);

    Task<Order?> IOrderRepository.GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Orders.FirstOrDefault(o => o.Id == id));
    public Task<Order?> GetByIdempotencyKeyAsync(string key, CancellationToken ct) => Task.FromResult(Orders.FirstOrDefault(o => o.IdempotencyKey == key));
    public void Add(Order order) => Orders.Add(order);

    public Task SaveChangesAsync(CancellationToken ct)
    {
        SaveCount++;
        return Task.CompletedTask;
    }

    public void Record(string eventType, string description, string entityType, Guid entityId, object? data = null) =>
        Events.Add((eventType, description, entityId));

    public Task<IReadOnlyList<AuditEntry>> GetForEntityAsync(Guid entityId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<AuditEntry>>(Events
            .Select(e => new AuditEntry(DateTimeOffset.UnixEpoch, e.EventType, e.Description, null, null))
            .ToList());
}

/// <summary>Gateway that replies with a scripted sequence of outcomes, one per call.</summary>
internal sealed class ScriptedGateway(params Func<PaymentRequest, PaymentResponse>[] script) : IPaymentGateway
{
    public List<PaymentRequest> Requests { get; } = [];

    public Task<PaymentResponse> AuthorizeAsync(PaymentRequest request, CancellationToken ct)
    {
        Requests.Add(request);
        return Task.FromResult(script[Math.Min(Requests.Count - 1, script.Length - 1)](request));
    }

    public static PaymentResponse Approved(PaymentRequest _) => new(PaymentOutcome.Approved, "approved", "Pago aprobado.", "AUTH-1");
    public static PaymentResponse Declined(PaymentRequest _) => new(PaymentOutcome.Declined, "insufficient_funds", "Fondos insuficientes.", null);
    public static PaymentResponse Transient(PaymentRequest _) => new(PaymentOutcome.TransientError, "processing_error", "Error temporal.", null);
    public static PaymentResponse Throws(PaymentRequest _) => throw new HttpRequestException("connection reset");
}

internal static class TestData
{
    public static readonly DateTimeOffset Now = new(2026, 9, 26, 12, 0, 0, TimeSpan.Zero);

    public static Product Product(int stock = 10, decimal price = 3499m) =>
        Toka.Domain.Products.Product.Create(Guid.NewGuid(), "SKU-1", "Audífonos", "", price, "MXN", stock);

    public static CardDetails Card(string number = "4111111111111111") => new("ANA LOPEZ", number, 12, 2030, "123");
}

internal static class Installments
{
    public static readonly InstallmentPolicy Policy = new(Options.Create(new InstallmentOptions
    {
        Plans = [new() { Months = 3, MinimumAmount = 1500m }, new() { Months = 6, MinimumAmount = 3000m }],
    }));
}
