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
        if (price <= 0) throw new DomainException("validation", "El precio debe ser mayor que cero.");
        if (stock < 0) throw new DomainException("validation", "El inventario no puede ser negativo.");
        return new Product
        {
            Id = id,
            Sku = Guard.NotBlank(sku, "SKU", 50),
            Name = Guard.NotBlank(name, "nombre del producto", 200),
            Description = description,
            Price = price,
            Currency = Guard.NotBlank(currency, "moneda", 3),
            Stock = stock,
        };
    }

    public void Reserve(int quantity)
    {
        Guard.Positive(quantity, "cantidad");
        if (quantity > Stock)
            throw new DomainException("insufficient_stock", $"Solo hay {Stock} unidad(es) disponibles del producto '{Name}'.");
        Stock -= quantity;
        Version++;
    }

    public void Release(int quantity)
    {
        Guard.Positive(quantity, "cantidad");
        Stock += quantity;
        Version++;
    }
}
