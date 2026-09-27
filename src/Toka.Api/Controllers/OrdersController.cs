using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Toka.Api.Contracts;
using Toka.Api.Infrastructure;
using Toka.Application.Orders;

namespace Toka.Api.Controllers;

[ApiController]
[Route("api/v1/orders")]
public sealed class OrdersController(OrderService orders) : ControllerBase
{
    public const string IdempotencyHeader = "Idempotency-Key";

    /// <summary>
    /// Checkout: registers the customer if new, creates the order, reserves stock and authorizes the card.
    /// Returns 201 whatever the payment outcome; check <c>status</c> (Paid, PaymentDeclined, PaymentFailed).
    /// Sending the same <c>Idempotency-Key</c> again returns 200 with the original order and does not charge twice.
    /// </summary>
    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.Checkout)]
    [ProducesResponseType<OrderDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<OrderDto>> Place(
        PlaceOrderRequest request,
        [FromHeader(Name = IdempotencyHeader)] string? idempotencyKey,
        CancellationToken ct)
    {
        var result = await orders.PlaceAsync(request.ToCommand(NullIfBlank(idempotencyKey)), ct);
        if (!result.IsSuccess) return this.ToProblem(result.Error!);

        var (order, replayed) = result.Value;
        return replayed ? Ok(order) : CreatedAtAction(nameof(Get), new { id = order.Id }, order);
    }

    /// <summary>
    /// Guest lookup by order id and buyer email. A wrong email returns 404, same as an unknown order.
    /// POST keeps the email out of URLs and access logs.
    /// </summary>
    [HttpPost("lookup")]
    [EnableRateLimiting(RateLimitPolicies.Lookup)]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> Lookup(LookupOrderRequest request, CancellationToken ct)
    {
        var result = await orders.LookupAsync(new LookupOrderQuery(request.OrderId, request.Email), ct);
        return result.IsSuccess ? result.Value : this.ToProblem(result.Error!);
    }

    /// <summary>Order status, IVA breakdown, payment attempts and audit trail. For trusted API clients; not routed from the public web.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> Get(Guid id, CancellationToken ct)
    {
        var result = await orders.GetAsync(id, ct);
        return result.IsSuccess ? result.Value : this.ToProblem(result.Error!);
    }

    /// <summary>Retries the payment of a declined or failed order, usually with another card. The buyer's email must match the order.</summary>
    [HttpPost("{id:guid}/payment-retries")]
    [EnableRateLimiting(RateLimitPolicies.Checkout)]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderDto>> RetryPayment(Guid id, RetryPaymentRequest request, CancellationToken ct)
    {
        var result = await orders.RetryPaymentAsync(new RetryPaymentCommand(id, request.Email, request.Card?.ToDetails()!), ct);
        return result.IsSuccess ? result.Value : this.ToProblem(result.Error!);
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
