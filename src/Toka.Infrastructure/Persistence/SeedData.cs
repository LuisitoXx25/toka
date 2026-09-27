using Toka.Domain.Products;

namespace Toka.Infrastructure.Persistence;

/// <summary>Demo catalog. Fixed ids so migrations are deterministic and tests can reference them.</summary>
public static class SeedData
{
    public static readonly Guid LaptopId = Guid.Parse("0199a0d4-0000-7000-8000-000000000001");
    public static readonly Guid HeadphonesId = Guid.Parse("0199a0d4-0000-7000-8000-000000000002");
    public static readonly Guid KeyboardId = Guid.Parse("0199a0d4-0000-7000-8000-000000000003");
    public static readonly Guid MonitorId = Guid.Parse("0199a0d4-0000-7000-8000-000000000004");

    public static Product[] Products =>
    [
        Product.Create(LaptopId, "LAP-001", "Laptop Pro 14\"", "Laptop de 14 pulgadas, 16 GB RAM, 512 GB SSD.", 28999.00m, "MXN", 15),
        Product.Create(HeadphonesId, "AUD-002", "Audífonos inalámbricos", "Cancelación de ruido, 30 h de batería.", 3499.00m, "MXN", 40),
        Product.Create(KeyboardId, "KEY-003", "Teclado mecánico", "Switches táctiles, retroiluminado.", 1899.00m, "MXN", 25),
        Product.Create(MonitorId, "MON-004", "Monitor 27\" 4K", "Panel IPS, USB-C 65 W.", 8999.00m, "MXN", 2),
    ];
}
