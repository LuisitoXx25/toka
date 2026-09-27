using Toka.Domain.Common;
using Toka.Domain.Products;

namespace Toka.Domain.Orders;

public sealed class Order : Entity
{
    private readonly List<PaymentAttempt> _attempts = [];

    public Guid CustomerId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    /// <summary>Amount before IVA.</summary>
    public decimal Subtotal { get; private set; }
    public decimal TaxRate { get; private set; }
    public decimal TaxAmount { get; private set; }
    /// <summary>Amount charged to the card, IVA included.</summary>
    public decimal Total { get; private set; }
    public string Currency { get; private set; } = null!;
    public OrderStatus Status { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public string? AuthorizationCode { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyList<PaymentAttempt> Attempts => _attempts;

    public bool CanRetryPayment => Status is OrderStatus.PaymentDeclined or OrderStatus.PaymentFailed;

    private Order() { }

    /// <summary>
    /// Creates an order and reserves stock on the product in one step so both change together.
    /// Product prices include IVA; the breakdown is stored so the order keeps the rate in force when it was placed.
    /// Amounts never change after creation.
    /// </summary>
    public static Order Place(Guid customerId, Product product, int quantity, string? idempotencyKey, DateTimeOffset now)
    {
        product.Reserve(quantity);
        var tax = TaxBreakdown.FromTaxIncludedTotal(product.Price * quantity);
        return new Order
        {
            CustomerId = customerId,
            ProductId = product.Id,
            Quantity = quantity,
            UnitPrice = product.Price,
            Subtotal = tax.Subtotal,
            TaxRate = tax.TaxRate,
            TaxAmount = tax.TaxAmount,
            Total = tax.Total,
            Currency = product.Currency,
            Status = OrderStatus.PendingPayment,
            IdempotencyKey = idempotencyKey,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public int NextAttemptNumber => _attempts.Count + 1;

    public PaymentAttempt RecordAttempt(PaymentOutcome outcome, string responseCode, string? message,
        string? authorizationCode, string cardLast4, string cardBrand, DateTimeOffset now)
    {
        EnsureStatus(OrderStatus.PendingPayment);
        var attempt = new PaymentAttempt(Id, NextAttemptNumber, outcome, responseCode, message, authorizationCode, cardLast4, cardBrand, now);
        _attempts.Add(attempt);
        UpdatedAt = now;
        return attempt;
    }

    public void MarkPaid(string authorizationCode, DateTimeOffset now)
    {
        EnsureStatus(OrderStatus.PendingPayment);
        Status = OrderStatus.Paid;
        AuthorizationCode = authorizationCode;
        FailureReason = null;
        UpdatedAt = now;
    }

    public void MarkDeclined(Product product, string reason, DateTimeOffset now) =>
        Close(product, OrderStatus.PaymentDeclined, reason, now);

    public void MarkPaymentFailed(Product product, string reason, DateTimeOffset now) =>
        Close(product, OrderStatus.PaymentFailed, reason, now);

    /// <summary>Re-reserves stock so a declined/failed order can be paid again.</summary>
    public void ReopenForPayment(Product product, DateTimeOffset now)
    {
        if (!CanRetryPayment)
            throw new DomainException("invalid_order_state", $"No se puede reintentar el pago de una orden en estado '{Status.ToDisplayName()}'.");
        EnsureProduct(product);
        product.Reserve(Quantity);
        Status = OrderStatus.PendingPayment;
        FailureReason = null;
        UpdatedAt = now;
    }

    private void Close(Product product, OrderStatus status, string reason, DateTimeOffset now)
    {
        EnsureStatus(OrderStatus.PendingPayment);
        EnsureProduct(product);
        product.Release(Quantity);
        Status = status;
        FailureReason = reason;
        UpdatedAt = now;
    }

    private void EnsureProduct(Product product)
    {
        if (product.Id != ProductId)
            throw new DomainException("validation", "El producto no corresponde a esta orden.");
    }

    private void EnsureStatus(OrderStatus expected)
    {
        if (Status != expected)
            throw new DomainException("invalid_order_state", $"La orden está en estado '{Status.ToDisplayName()}'; se esperaba '{expected.ToDisplayName()}'.");
    }
}
