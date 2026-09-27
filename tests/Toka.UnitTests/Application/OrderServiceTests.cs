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
        var processor = new PaymentProcessor(_gateway, _store, _store, clock,
            Options.Create(new PaymentOptions { BaseRetryDelay = TimeSpan.Zero }), NullLogger<PaymentProcessor>.Instance);

        _service = new OrderService(_store, _store, customers, processor, _store, _store,
            new PlaceOrderValidator(customerValidator, cardValidator), new RetryPaymentValidator(cardValidator),
            clock, NullLogger<OrderService>.Instance);
    }

    private PlaceOrderCommand Command(Guid productId, int quantity = 1, string? key = null) => new(
        new CustomerInput("Ana", "López", "Ana@Correo.MX", null), productId, quantity, TestData.Card(), key);

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
    public async Task Retry_payment_of_unknown_order_is_not_found()
    {
        var result = await _service.RetryPaymentAsync(new RetryPaymentCommand(Guid.NewGuid(), TestData.Card()), default);

        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }
}
