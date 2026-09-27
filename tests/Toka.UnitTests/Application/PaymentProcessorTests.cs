using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Toka.Application.Abstractions;
using Toka.Application.Payments;
using Toka.Domain.Orders;
using Toka.Domain.Products;
using Toka.UnitTests.Fakes;

namespace Toka.UnitTests.Application;

public class PaymentProcessorTests
{
    private readonly InMemoryStore _store = new();
    private readonly Product _product = TestData.Product(stock: 5);
    private readonly Order _order;

    public PaymentProcessorTests() => _order = Order.Place(Guid.NewGuid(), _product, 2, 1, null, TestData.Now);

    private PaymentProcessor Processor(IPaymentGateway gateway) => new(
        gateway, new FakeBinLookup(), _store, _store, new FakeTimeProvider(TestData.Now),
        Options.Create(new PaymentOptions { MaxAttempts = 3, BaseRetryDelay = TimeSpan.Zero }),
        NullLogger<PaymentProcessor>.Instance);

    [Fact]
    public async Task Approved_marks_order_paid_and_keeps_stock_reserved()
    {
        await Processor(new ScriptedGateway(ScriptedGateway.Approved)).ProcessAsync(_order, _product, TestData.Card(), default);

        Assert.Equal(OrderStatus.Paid, _order.Status);
        Assert.Equal("AUTH-1", _order.AuthorizationCode);
        Assert.Equal(3, _product.Stock);
        Assert.Contains(_store.Events, e => e.EventType == AuditEvents.OrderPaid);
    }

    [Fact]
    public async Task Declined_is_not_retried_and_releases_stock()
    {
        var gateway = new ScriptedGateway(ScriptedGateway.Declined);

        await Processor(gateway).ProcessAsync(_order, _product, TestData.Card(), default);

        Assert.Single(gateway.Requests);
        Assert.Equal(OrderStatus.PaymentDeclined, _order.Status);
        Assert.Equal("Fondos insuficientes.", _order.FailureReason);
        Assert.Equal(5, _product.Stock);
        Assert.Contains(_store.Events, e => e.EventType == AuditEvents.StockReleased);
    }

    [Fact]
    public async Task Transient_error_is_retried_until_approved()
    {
        var gateway = new ScriptedGateway(ScriptedGateway.Transient, ScriptedGateway.Approved);

        await Processor(gateway).ProcessAsync(_order, _product, TestData.Card(), default);

        Assert.Equal([1, 2], gateway.Requests.Select(r => r.AttemptNumber));
        Assert.Equal(OrderStatus.Paid, _order.Status);
        Assert.Equal([PaymentOutcome.TransientError, PaymentOutcome.Approved], _order.Attempts.Select(a => a.Outcome));
    }

    [Fact]
    public async Task Exhausted_retries_end_as_payment_failed()
    {
        var gateway = new ScriptedGateway(ScriptedGateway.Transient);

        await Processor(gateway).ProcessAsync(_order, _product, TestData.Card(), default);

        Assert.Equal(3, gateway.Requests.Count);
        Assert.Equal(OrderStatus.PaymentFailed, _order.Status);
        Assert.Equal(5, _product.Stock);
    }

    [Fact]
    public async Task Gateway_exception_is_treated_as_transient()
    {
        var gateway = new ScriptedGateway(ScriptedGateway.Throws, ScriptedGateway.Approved);

        await Processor(gateway).ProcessAsync(_order, _product, TestData.Card(), default);

        Assert.Equal("gateway_error", _order.Attempts[0].ResponseCode);
        Assert.Equal(OrderStatus.Paid, _order.Status);
    }

    [Fact]
    public async Task Every_attempt_is_audited_with_masked_card()
    {
        await Processor(new ScriptedGateway(ScriptedGateway.Transient, ScriptedGateway.Declined))
            .ProcessAsync(_order, _product, TestData.Card(), default);

        var attempts = _store.Events.Where(e => e.EventType == AuditEvents.PaymentAttempted).ToList();
        Assert.Equal(2, attempts.Count);
        Assert.All(attempts, e => Assert.Contains("****1111", e.Description));
        Assert.DoesNotContain(_store.Events, e => e.Description.Contains("4111111111111111"));
    }
}
