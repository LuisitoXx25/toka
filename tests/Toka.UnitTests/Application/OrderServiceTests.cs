using System.Globalization;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Toka.Application.Common;
using Toka.Application.Customers;
using Toka.Application.Orders;
using Toka.Application.Payments;
using Toka.UnitTests.Fakes;

namespace Toka.UnitTests.Application;

public class OrderServiceTests
{
    private readonly InMemoryStore _store = new();
    private readonly ScriptedGateway _gateway = new(ScriptedGateway.Approved);
    private readonly OrderService _service;

    public OrderServiceTests()
    {
        ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("es");
        var clock = new FakeTimeProvider(TestData.Now);
        var customerValidator = new CustomerInputValidator();
        var cardValidator = new CardValidator(clock);
        var customers = new CustomerService(_store, _store, _store, customerValidator, clock);
        var processor = new PaymentProcessor(_gateway, new FakeBinLookup(), _store, _store, clock,
            Options.Create(new PaymentOptions { BaseRetryDelay = TimeSpan.Zero }), NullLogger<PaymentProcessor>.Instance);

        _service = new OrderService(_store, _store, _store, customers, processor, Installments.Policy, new FakeBinLookup(), _store, _store,
            new PlaceOrderValidator(customerValidator, cardValidator), new RetryPaymentValidator(cardValidator), new LookupOrderValidator(),
            clock, NullLogger<OrderService>.Instance);
    }

    private PlaceOrderCommand Command(Guid productId, int quantity = 1, string? key = null, int installments = 1, string card = "4111111111111111") => new(
        new CustomerInput("Ana", "López", "Ana@Correo.MX", null), productId, quantity, installments, TestData.Card(card), key);

    [Fact]
    public async Task Place_registers_customer_creates_order_and_charges()
    {
        var product = TestData.Product(stock: 5);
        _store.Products.Add(product);

        var result = await _service.PlaceAsync(Command(product.Id, 2), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("Paid", result.Value.Order.Status);
        Assert.False(result.Value.Replayed);
        Assert.Equal("ana@correo.mx", Assert.Single(_store.Customers).Email);
        Assert.Equal(3, product.Stock);
    }

    [Fact]
    public async Task Returning_customer_is_reused()
    {
        var product = TestData.Product();
        _store.Products.Add(product);

        await _service.PlaceAsync(Command(product.Id), default);
        await _service.PlaceAsync(Command(product.Id), default);

        Assert.Single(_store.Customers);
        Assert.Equal(2, _store.Orders.Count);
    }

    [Fact]
    public async Task Same_idempotency_key_returns_original_order_without_charging_again()
    {
        var product = TestData.Product();
        _store.Products.Add(product);

        var first = await _service.PlaceAsync(Command(product.Id, key: "abc"), default);
        var second = await _service.PlaceAsync(Command(product.Id, key: "abc"), default);

        Assert.True(second.Value.Replayed);
        Assert.Equal(first.Value.Order.Id, second.Value.Order.Id);
        Assert.Single(_gateway.Requests);
        Assert.Single(_store.Orders);
    }

    [Fact]
    public async Task Unknown_product_is_not_found()
    {
        var result = await _service.PlaceAsync(Command(Guid.NewGuid()), default);

        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
        Assert.StartsWith("No se encontró el producto", result.Error.Message);
    }

    [Fact]
    public async Task Insufficient_stock_is_a_conflict_and_nothing_is_saved()
    {
        var product = TestData.Product(stock: 1);
        _store.Products.Add(product);

        var result = await _service.PlaceAsync(Command(product.Id, 2), default);

        Assert.Equal(ErrorType.Conflict, result.Error!.Type);
        Assert.Equal("insufficient_stock", result.Error.Code);
        Assert.Equal(0, _store.SaveCount);
        Assert.Empty(_gateway.Requests);
    }

    [Fact]
    public async Task Invalid_command_returns_validation_error_without_side_effects()
    {
        var result = await _service.PlaceAsync(Command(Guid.NewGuid(), quantity: 0), default);

        Assert.Equal(ErrorType.Validation, result.Error!.Type);
        Assert.Contains("Quantity", result.Error.Details!.Keys);
        Assert.Empty(_store.Customers);
    }

    [Fact]
    public async Task Installment_plan_is_stored_with_the_monthly_payment()
    {
        var product = TestData.Product(price: 3499m);
        _store.Products.Add(product);

        var result = await _service.PlaceAsync(Command(product.Id, installments: 6), default);

        var order = result.Value.Order;
        Assert.Equal(6, order.Installments);
        Assert.Equal((583.20m, 583.16m), (order.FirstPayment, order.MonthlyPayment));
        Assert.Equal("6 pagos de $583.16 sin intereses; el primero de $583.20", order.PaymentPlan);
        Assert.Equal(6, _gateway.Requests.Single().Installments);
    }

    [Fact]
    public async Task Installment_plan_below_its_minimum_is_rejected_before_reserving_stock()
    {
        var product = TestData.Product(stock: 5, price: 1899m);
        _store.Products.Add(product);

        var result = await _service.PlaceAsync(Command(product.Id, installments: 6), default);

        Assert.Equal("installments_not_available", result.Error!.Code);
        Assert.Contains("compra mínima de $3,000.00", result.Error.Message);
        Assert.Equal(5, product.Stock);
    }

    [Fact]
    public async Task Debit_card_cannot_use_installments()
    {
        var product = TestData.Product(stock: 5, price: 3499m);
        _store.Products.Add(product);

        var result = await _service.PlaceAsync(Command(product.Id, installments: 3, card: FakeBinLookup.DebitCard), default);

        Assert.Equal("installments_not_available", result.Error!.Code);
        Assert.Equal(InstallmentPolicy.DebitNotEligible, result.Error.Message);
        Assert.Equal(5, product.Stock);
    }

    [Fact]
    public async Task Debit_card_pays_single_payment_and_is_recorded_as_debit()
    {
        var product = TestData.Product();
        _store.Products.Add(product);

        var result = await _service.PlaceAsync(Command(product.Id, card: FakeBinLookup.DebitCard), default);

        Assert.Equal("Debit", result.Value.Order.Attempts.Single().CardType);
        Assert.Equal("Débito", result.Value.Order.Attempts.Single().CardTypeDisplay);
    }

    [Fact]
    public async Task Lookup_requires_the_buyer_email()
    {
        var product = TestData.Product();
        _store.Products.Add(product);
        var placed = await _service.PlaceAsync(Command(product.Id), default);
        var id = placed.Value.Order.Id;

        var owner = await _service.LookupAsync(new LookupOrderQuery(id, " ANA@correo.mx "), default);
        var stranger = await _service.LookupAsync(new LookupOrderQuery(id, "otra@correo.mx"), default);
        var unknown = await _service.LookupAsync(new LookupOrderQuery(Guid.NewGuid(), "ana@correo.mx"), default);

        Assert.True(owner.IsSuccess);
        Assert.Equal(ErrorType.NotFound, stranger.Error!.Type);
        Assert.Equal(unknown.Error!.Code, stranger.Error.Code);
    }

    [Fact]
    public async Task Retry_payment_with_another_email_is_not_found()
    {
        var product = TestData.Product();
        _store.Products.Add(product);
        var placed = await _service.PlaceAsync(Command(product.Id), default);

        var result = await _service.RetryPaymentAsync(new RetryPaymentCommand(placed.Value.Order.Id, "otra@correo.mx", TestData.Card()), default);

        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
        Assert.Single(_gateway.Requests);
    }

    [Fact]
    public async Task Retry_payment_of_unknown_order_is_not_found()
    {
        var result = await _service.RetryPaymentAsync(new RetryPaymentCommand(Guid.NewGuid(), "ana@correo.mx", TestData.Card()), default);

        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }
}
