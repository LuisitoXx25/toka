using Toka.Application.Abstractions;
using Toka.Application.Common;
using Toka.Domain.Products;

namespace Toka.Application.Products;

public sealed record ProductDto(Guid Id, string Sku, string Name, string Description, decimal Price, string Currency, int Stock)
{
    public static ProductDto From(Product p) => new(p.Id, p.Sku, p.Name, p.Description, p.Price, p.Currency, p.Stock);
}

public sealed class ProductService(IProductRepository products)
{
    public async Task<IReadOnlyList<ProductDto>> ListAsync(CancellationToken ct) =>
        (await products.ListAsync(ct)).Select(ProductDto.From).ToList();

    public async Task<Result<ProductDto>> GetAsync(Guid id, CancellationToken ct) =>
        await products.GetByIdAsync(id, ct) is { } p ? ProductDto.From(p) : Error.NotFound("el producto", id);
}
