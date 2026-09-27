using Toka.Domain.Common;

namespace Toka.Domain.Products;

public sealed class Product : Entity
{
    public string Sku { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public string Currency { get; private set; } = "MXN";
    public int Stock { get; private set; }

    /// <summary>Optimistic concurrency token; bumped on every stock change.</summary>
    public int Version { get; private set; }

    private Product() { }

    public static Product Create(Guid id, string sku, string name, string description, decimal price, string currency, int stock)
    {
        if (price <= 0) throw new DomainException("validation", "Price must be greater than zero.");
        if (stock < 0) throw new DomainException("validation", "Stock cannot be negative.");
        return new Product
        {
            Id = id,
            Sku = Guard.NotBlank(sku, nameof(Sku), 50),
            Name = Guard.NotBlank(name, nameof(Name), 200),
            Description = description,
            Price = price,
            Currency = Guard.NotBlank(currency, nameof(Currency), 3),
            Stock = stock,
        };
    }

    public void Reserve(int quantity)
    {
        Guard.Positive(quantity, nameof(quantity));
        if (quantity > Stock)
            throw new DomainException("insufficient_stock", $"Only {Stock} unit(s) of '{Sku}' available.");
        Stock -= quantity;
        Version++;
    }

    public void Release(int quantity)
    {
        Guard.Positive(quantity, nameof(quantity));
        Stock += quantity;
        Version++;
    }
}
