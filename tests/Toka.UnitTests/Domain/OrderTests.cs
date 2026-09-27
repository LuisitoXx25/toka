using Toka.Domain.Common;
using Toka.Domain.Orders;
using Toka.UnitTests.Fakes;

namespace Toka.UnitTests.Domain;

public class OrderTests
{
    private readonly Guid _customerId = Guid.NewGuid();

    [Fact]
    public void Place_reserves_stock_and_stores_iva_breakdown()
    {
        var product = TestData.Product(stock: 10, price: 3499m);

        var order = Order.Place(_customerId, product, 2, 1, "key-1", TestData.Now);

        Assert.Equal(8, product.Stock);
        Assert.Equal(OrderStatus.PendingPayment, order.Status);
        Assert.Equal(6998m, order.Total);
        Assert.Equal(6032.76m, order.Subtotal);
        Assert.Equal(965.24m, order.TaxAmount);
        Assert.Equal(0.16m, order.TaxRate);
        Assert.Equal("key-1", order.IdempotencyKey);
    }

    [Fact]
    public void Installments_must_be_at_least_one() =>
        Assert.Throws<DomainException>(() => Order.Place(_customerId, TestData.Product(), 1, 0, null, TestData.Now));

    [Fact]
    public void Monthly_payment_splits_the_total()
    {
        var order = Order.Place(_customerId, TestData.Product(price: 3499m), 1, 3, null, TestData.Now);

        Assert.Equal(1166.33m, order.MonthlyPayment);
    }

    [Fact]
    public void MarkPaid_sets_authorization()
    {
        var order = Order.Place(_customerId, TestData.Product(), 1, 1, null, TestData.Now);

        order.MarkPaid("AUTH-1", TestData.Now);

        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Equal("AUTH-1", order.AuthorizationCode);
        Assert.False(order.CanRetryPayment);
    }

    [Fact]
    public void MarkDeclined_releases_stock_and_allows_retry()
    {
        var product = TestData.Product(stock: 3);
        var order = Order.Place(_customerId, product, 2, 1, null, TestData.Now);

        order.MarkDeclined(product, "Fondos insuficientes.", TestData.Now);

        Assert.Equal(3, product.Stock);
        Assert.Equal(OrderStatus.PaymentDeclined, order.Status);
        Assert.Equal("Fondos insuficientes.", order.FailureReason);
        Assert.True(order.CanRetryPayment);
    }

    [Fact]
    public void ReopenForPayment_reserves_stock_again()
    {
        var product = TestData.Product(stock: 3);
        var order = Order.Place(_customerId, product, 2, 1, null, TestData.Now);
        order.MarkPaymentFailed(product, "Sin respuesta.", TestData.Now);

        order.ReopenForPayment(product, TestData.Now);

        Assert.Equal(1, product.Stock);
        Assert.Equal(OrderStatus.PendingPayment, order.Status);
        Assert.Null(order.FailureReason);
    }

    [Fact]
    public void Paid_order_cannot_be_reopened()
    {
        var product = TestData.Product();
        var order = Order.Place(_customerId, product, 1, 1, null, TestData.Now);
        order.MarkPaid("AUTH-1", TestData.Now);

        var ex = Assert.Throws<DomainException>(() => order.ReopenForPayment(product, TestData.Now));

        Assert.Equal("invalid_order_state", ex.Code);
        Assert.Contains("'pagada'", ex.Message);
    }

    [Fact]
    public void Attempts_are_numbered_sequentially()
    {
        var order = Order.Place(_customerId, TestData.Product(), 1, 1, null, TestData.Now);

        order.RecordAttempt(PaymentOutcome.TransientError, "processing_error", null, null, "1111", "VISA", TestData.Now);
        order.RecordAttempt(PaymentOutcome.Approved, "approved", null, "AUTH-1", "1111", "VISA", TestData.Now);

        Assert.Equal([1, 2], order.Attempts.Select(a => a.AttemptNumber));
    }

    [Fact]
    public void Cannot_record_attempt_after_order_is_closed()
    {
        var order = Order.Place(_customerId, TestData.Product(), 1, 1, null, TestData.Now);
        order.MarkPaid("AUTH-1", TestData.Now);

        Assert.Throws<DomainException>(() =>
            order.RecordAttempt(PaymentOutcome.Approved, "approved", null, "AUTH-2", "1111", "VISA", TestData.Now));
    }
}
