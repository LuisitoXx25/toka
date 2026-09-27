using Microsoft.AspNetCore.Mvc;
using Toka.Api.Contracts;
using Toka.Api.Infrastructure;
using Toka.Application.Customers;

namespace Toka.Api.Controllers;

[ApiController]
[Route("api/v1/customers")]
public sealed class CustomersController(CustomerService customers) : ControllerBase
{
    /// <summary>Registers a new customer.</summary>
    [HttpPost]
    [ProducesResponseType<CustomerDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CustomerDto>> Register(CustomerRequest request, CancellationToken ct)
    {
        var result = await customers.RegisterAsync(request.ToInput(), ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value.Id }, result.Value)
            : this.ToProblem(result.Error!);
    }

    /// <summary>Customer by id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<CustomerDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDto>> Get(Guid id, CancellationToken ct)
    {
        var result = await customers.GetAsync(id, ct);
        return result.IsSuccess ? result.Value : this.ToProblem(result.Error!);
    }
}
