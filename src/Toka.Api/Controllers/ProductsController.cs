using Microsoft.AspNetCore.Mvc;
using Toka.Api.Infrastructure;
using Toka.Application.Products;

namespace Toka.Api.Controllers;

[ApiController]
[Route("api/v1/products")]
[Produces("application/json")]
public sealed class ProductsController(ProductService products) : ControllerBase
{
    /// <summary>Product catalog. Prices include IVA.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProductDto>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<ProductDto>> List(CancellationToken ct) => await products.ListAsync(ct);

    /// <summary>A single product with its current stock.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> Get(Guid id, CancellationToken ct)
    {
        var result = await products.GetAsync(id, ct);
        return result.IsSuccess ? result.Value : this.ToProblem(result.Error!);
    }
}
