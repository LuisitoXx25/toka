using FluentValidation;
using Microsoft.Extensions.Logging;
using Toka.Application.Abstractions;
using Toka.Application.Common;
using Toka.Application.Customers;
using Toka.Application.Payments;
using Toka.Domain.Common;
using Toka.Domain.Customers;
using Toka.Domain.Orders;
using Toka.Domain.Products;

namespace Toka.Application.Orders;

public sealed class OrderService(
    IOrderRepository orders,
    ICustomerRepository customers,
    IProductRepository products,
    CustomerService customerService,
    PaymentProcessor paymentProcessor,
    InstallmentPolicy installments,
    ICardBinLookup binLookup,
    IUnitOfWork unitOfWork,
    IAuditLog audit,
    IValidator<PlaceOrderCommand> placeValidator,
    IValidator<RetryPaymentCommand> retryValidator,
    IValidator<LookupOrderQuery> lookupValidator,
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

        if (installments.Reject(command.Installments, product.Price * command.Quantity, binLookup.Lookup(command.Card.Number)) is { } reason)
            return new Error("installments_not_available", reason, ErrorType.BusinessRule);

        // Customer, order and stock reservation are committed together, before talking to the payment gateway.
        Order order;
        try
        {
            var customer = await customerService.FindOrAddAsync(command.Customer, ct);
            order = Order.Place(customer.Id, product, command.Quantity, command.Installments, command.IdempotencyKey, clock.GetUtcNow());
            orders.Add(order);
            audit.Record(AuditEvents.OrderCreated,
                $"Orden creada: {order.Quantity} × {product.Name}. Subtotal {order.Subtotal:N2} + IVA {order.TaxAmount:N2} = {order.Total:N2} {order.Currency}{DescribeInstallments(order)}.",
                nameof(Order), order.Id, new { order.CustomerId, order.ProductId, order.Quantity, order.UnitPrice, order.Subtotal, order.TaxRate, order.TaxAmount, order.Total, order.Currency, order.Installments });
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

        if (await FindOwnedOrderAsync(command.OrderId, command.CustomerEmail, ct) is not { } order)
            return Error.NotFound("la orden", command.OrderId);
        var product = await products.GetByIdAsync(order.ProductId, ct)
            ?? throw new InvalidOperationException($"Product {order.ProductId} of order {order.Id} is missing.");

        // The plan was fixed when the order was placed; a debit card can only retry single-payment orders.
        if (order.Installments > 1 && binLookup.Lookup(command.Card.Number) == CardType.Debit)
            return new Error("installments_not_available", InstallmentPolicy.DebitNotEligible, ErrorType.BusinessRule);

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

    /// <summary>Order status for trusted API clients (back office). Not exposed to the public web.</summary>
    public async Task<Result<OrderDto>> GetAsync(Guid id, CancellationToken ct) =>
        await orders.GetByIdAsync(id, ct) is { } order ? await ToDtoAsync(order, ct) : Error.NotFound("la orden", id);

    /// <summary>
    /// Guest order lookup: requires the order id and the buyer's email. A wrong email returns the same
    /// not-found error as an unknown id, so the response never confirms that an order exists.
    /// </summary>
    public async Task<Result<OrderDto>> LookupAsync(LookupOrderQuery query, CancellationToken ct)
    {
        if (await lookupValidator.ValidateToErrorAsync(query, ct) is { } validationError) return validationError;

        return await FindOwnedOrderAsync(query.OrderId, query.CustomerEmail, ct) is { } order
            ? await ToDtoAsync(order, ct)
            : Error.NotFound("la orden", query.OrderId);
    }

    private async Task<Order?> FindOwnedOrderAsync(Guid orderId, string email, CancellationToken ct)
    {
        if (await orders.GetByIdAsync(orderId, ct) is not { } order) return null;

        var customer = await customers.GetByIdAsync(order.CustomerId, ct);
        if (customer?.Email == Customer.NormalizeEmail(email)) return order;

        // Repeated mismatches for the same order may indicate enumeration; the email itself is not logged.
        logger.LogWarning("Order {OrderId} requested with an email that does not match its customer", orderId);
        return null;
    }

    private void RecordStockReserved(Order order, Product product) =>
        audit.Record(AuditEvents.StockReserved,
            $"Se apartaron {order.Quantity} unidad(es); quedan {product.Stock} en inventario.",
            nameof(Product), product.Id, new { OrderId = order.Id, order.Quantity, RemainingStock = product.Stock });

    private async Task<OrderDto> ToDtoAsync(Order order, CancellationToken ct) =>
        OrderDto.From(order, await audit.GetForEntityAsync(order.Id, ct));

    private static string DescribeInstallments(Order order) =>
        order.Installments > 1 ? $", en {InstallmentPolicy.Describe(order.Schedule)}" : "";

    private static Error ConcurrencyError() =>
        new("concurrency_conflict", "El inventario cambió mientras se procesaba la orden. Intenta de nuevo.", ErrorType.Conflict);

    private static Error ToError(DomainException ex) => ex.Code switch
    {
        "validation" => new Error(ex.Code, ex.Message, ErrorType.Validation),
        "insufficient_stock" or "invalid_order_state" => new Error(ex.Code, ex.Message, ErrorType.Conflict),
        _ => new Error(ex.Code, ex.Message, ErrorType.BusinessRule),
    };
}
