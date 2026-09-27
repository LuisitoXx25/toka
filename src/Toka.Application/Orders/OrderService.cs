using FluentValidation;
using Microsoft.Extensions.Logging;
using Toka.Application.Abstractions;
using Toka.Application.Common;
using Toka.Application.Customers;
using Toka.Application.Payments;
using Toka.Domain.Common;
using Toka.Domain.Orders;
using Toka.Domain.Products;

namespace Toka.Application.Orders;

public sealed class OrderService(
    IOrderRepository orders,
    IProductRepository products,
    CustomerService customerService,
    PaymentProcessor paymentProcessor,
    IUnitOfWork unitOfWork,
    IAuditLog audit,
    IValidator<PlaceOrderCommand> placeValidator,
    IValidator<RetryPaymentCommand> retryValidator,
    TimeProvider clock,
    ILogger<OrderService> logger)
{
    /// <summary>
    /// Checkout: registers the customer (if new), creates the order reserving stock, and authorizes the payment.
    /// A declined or failed payment is not an error: the order is returned with that status.
    /// With an idempotency key, repeating the request returns the original order instead of charging again.
    /// </summary>
    public async Task<Result<PlaceOrderResult>> PlaceAsync(PlaceOrderCommand command, CancellationToken ct)
    {
        if (await placeValidator.ValidateToErrorAsync(command, ct) is { } validationError) return validationError;

        if (command.IdempotencyKey is { } key && await orders.GetByIdempotencyKeyAsync(key, ct) is { } existing)
        {
            logger.LogInformation("Idempotent replay for key {IdempotencyKey}, returning order {OrderId}", key, existing.Id);
            return new PlaceOrderResult(await ToDtoAsync(existing, ct), Replayed: true);
        }

        if (await products.GetByIdAsync(command.ProductId, ct) is not { } product)
            return Error.NotFound("el producto", command.ProductId);

        // Customer, order and stock reservation are committed together, before talking to the payment gateway.
        Order order;
        try
        {
            var customer = await customerService.FindOrAddAsync(command.Customer, ct);
            order = Order.Place(customer.Id, product, command.Quantity, command.IdempotencyKey, clock.GetUtcNow());
            orders.Add(order);
            audit.Record(AuditEvents.OrderCreated,
                $"Orden creada: {order.Quantity} × {product.Name}, total {order.Total:N2} {order.Currency}.",
                nameof(Order), order.Id, new { order.CustomerId, order.ProductId, order.Quantity, order.Total });
            RecordStockReserved(order, product);
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (DomainException ex)
        {
            return ToError(ex);
        }
        catch (ConcurrencyConflictException)
        {
            return ConcurrencyError();
        }

        await paymentProcessor.ProcessAsync(order, product, command.Card, ct);
        return new PlaceOrderResult(await ToDtoAsync(order, ct), Replayed: false);
    }

    /// <summary>Retries the payment of a declined or failed order, usually with another card. Stock is reserved again first.</summary>
    public async Task<Result<OrderDto>> RetryPaymentAsync(RetryPaymentCommand command, CancellationToken ct)
    {
        if (await retryValidator.ValidateToErrorAsync(command, ct) is { } validationError) return validationError;

        if (await orders.GetByIdAsync(command.OrderId, ct) is not { } order)
            return Error.NotFound("la orden", command.OrderId);
        var product = await products.GetByIdAsync(order.ProductId, ct)
            ?? throw new InvalidOperationException($"Product {order.ProductId} of order {order.Id} is missing.");

        try
        {
            order.ReopenForPayment(product, clock.GetUtcNow());
            audit.Record(AuditEvents.PaymentRetryRequested, "Se solicitó reintentar el pago.", nameof(Order), order.Id);
            RecordStockReserved(order, product);
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (DomainException ex)
        {
            return ToError(ex);
        }
        catch (ConcurrencyConflictException)
        {
            return ConcurrencyError();
        }

        await paymentProcessor.ProcessAsync(order, product, command.Card, ct);
        return await ToDtoAsync(order, ct);
    }

    /// <summary>Order status with its payment attempts and audit trail.</summary>
    public async Task<Result<OrderDto>> GetAsync(Guid id, CancellationToken ct) =>
        await orders.GetByIdAsync(id, ct) is { } order ? await ToDtoAsync(order, ct) : Error.NotFound("la orden", id);

    private void RecordStockReserved(Order order, Product product) =>
        audit.Record(AuditEvents.StockReserved,
            $"Se apartaron {order.Quantity} unidad(es); quedan {product.Stock} en inventario.",
            nameof(Product), product.Id, new { OrderId = order.Id, order.Quantity, RemainingStock = product.Stock });

    private async Task<OrderDto> ToDtoAsync(Order order, CancellationToken ct) =>
        OrderDto.From(order, await audit.GetForEntityAsync(order.Id, ct));

    private static Error ConcurrencyError() =>
        new("concurrency_conflict", "El inventario cambió mientras se procesaba la orden. Intenta de nuevo.", ErrorType.Conflict);

    private static Error ToError(DomainException ex) => ex.Code switch
    {
        "validation" => new Error(ex.Code, ex.Message, ErrorType.Validation),
        "insufficient_stock" or "invalid_order_state" => new Error(ex.Code, ex.Message, ErrorType.Conflict),
        _ => new Error(ex.Code, ex.Message, ErrorType.BusinessRule),
    };
}
