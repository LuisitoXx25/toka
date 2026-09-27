using Toka.Application.Abstractions;
using Toka.Application.Customers;
using Toka.Application.Payments;
using Toka.Domain.Orders;

namespace Toka.Application.Orders;

/// <param name="Installments">MSI months; 1 is a single payment.</param>
public sealed record PlaceOrderCommand(CustomerInput Customer, Guid ProductId, int Quantity, int Installments, CardDetails Card, string? IdempotencyKey);

/// <param name="CustomerEmail">Must match the order's customer; proves the caller owns the order.</param>
public sealed record RetryPaymentCommand(Guid OrderId, string CustomerEmail, CardDetails Card);

/// <param name="CustomerEmail">Must match the order's customer.</param>
public sealed record LookupOrderQuery(Guid OrderId, string CustomerEmail);

public sealed record PaymentAttemptDto(int AttemptNumber, string Outcome, string ResponseCode, string? Message,
    string CardBrand, string CardLast4, DateTimeOffset OccurredAt);

/// <param name="Status">Technical status code (e.g. PaymentDeclined).</param>
/// <param name="StatusDisplay">Status label in Spanish for the UI.</param>
public sealed record OrderDto(
    Guid Id,
    Guid CustomerId,
    Guid ProductId,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal,
    decimal TaxRate,
    decimal TaxAmount,
    decimal Total,
    string Currency,
    int Installments,
    decimal MonthlyPayment,
    string Status,
    string StatusDisplay,
    string? AuthorizationCode,
    string? FailureReason,
    bool CanRetryPayment,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<PaymentAttemptDto> Attempts,
    IReadOnlyList<AuditEntry> Events)
{
    public static OrderDto From(Order o, IReadOnlyList<AuditEntry> events) => new(
        o.Id, o.CustomerId, o.ProductId, o.Quantity, o.UnitPrice, o.Subtotal, o.TaxRate, o.TaxAmount, o.Total, o.Currency, o.Installments, o.MonthlyPayment,
        o.Status.ToString(), o.Status.ToDisplayName(),
        o.AuthorizationCode, o.FailureReason, o.CanRetryPayment, o.CreatedAt, o.UpdatedAt,
        o.Attempts
            .OrderBy(a => a.AttemptNumber)
            .Select(a => new PaymentAttemptDto(a.AttemptNumber, a.Outcome.ToString(), a.ResponseCode, a.Message,
                a.CardBrand, a.CardLast4, a.OccurredAt))
            .ToList(),
        events);
}

/// <param name="Replayed">True when a previous request with the same idempotency key already created this order.</param>
public sealed record PlaceOrderResult(OrderDto Order, bool Replayed);
