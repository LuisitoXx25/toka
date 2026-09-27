using System.Net;
using System.Net.Http.Json;

namespace Toka.IntegrationTests;

[Collection(ApiCollection.Name)]
public class CheckoutApiTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateAuthenticatedClient();

    [Fact]
    public async Task Approved_payment_creates_paid_order_and_decrements_stock()
    {
        var stockBefore = await _client.StockAsync(Checkout.Headphones);

        var response = await _client.PlaceAsync(Checkout.Order(quantity: 2));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await response.JsonAsync();
        Assert.Equal("Paid", (string?)order["status"]);
        Assert.Equal("pagada", (string?)order["statusDisplay"]);
        Assert.Equal(6998m, (decimal)order["total"]!);
        Assert.Equal(6032.76m, (decimal)order["subtotal"]!);
        Assert.Equal(965.24m, (decimal)order["taxAmount"]!);
        Assert.StartsWith("AUTH-", (string?)order["authorizationCode"]);
        Assert.Equal($"/api/v1/orders/{order["id"]}", response.Headers.Location?.AbsolutePath);
        Assert.Equal(stockBefore - 2, await _client.StockAsync(Checkout.Headphones));
    }

    [Fact]
    public async Task Order_status_includes_attempts_and_audit_trail()
    {
        var created = await (await _client.PlaceAsync(Checkout.Order())).JsonAsync();

        var order = await (await _client.GetAsync($"/api/v1/orders/{created["id"]}")).JsonAsync();

        var events = order["events"]!.AsArray().Select(e => (string?)e!["eventType"]).ToList();
        Assert.Equal(["order.created", "inventory.reserved", "payment.attempted", "order.paid"], events);
        Assert.Single(order["attempts"]!.AsArray());
        Assert.Equal("1111", (string?)order["attempts"]![0]!["cardLast4"]);
    }

    [Fact]
    public async Task Declined_payment_releases_stock_and_can_be_retried_with_another_card()
    {
        var stockBefore = await _client.StockAsync(Checkout.Headphones);

        var declined = await _client.PlaceAsync(Checkout.Order(Checkout.DeclinedCard));

        Assert.Equal(HttpStatusCode.Created, declined.StatusCode);
        var order = await declined.JsonAsync();
        Assert.Equal("PaymentDeclined", (string?)order["status"]);
        Assert.Equal("Fondos insuficientes.", (string?)order["failureReason"]);
        Assert.True((bool)order["canRetryPayment"]!);
        Assert.Equal(stockBefore, await _client.StockAsync(Checkout.Headphones));

        var retry = await _client.PostAsJsonAsync($"/api/v1/orders/{order["id"]}/payment-retries",
            new { card = Checkout.Card("5555555555554444") });

        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        var paid = await retry.JsonAsync();
        Assert.Equal("Paid", (string?)paid["status"]);
        Assert.Equal(2, paid["attempts"]!.AsArray().Count);
        Assert.Equal(stockBefore - 1, await _client.StockAsync(Checkout.Headphones));
    }

    [Fact]
    public async Task Paid_order_cannot_be_retried()
    {
        var order = await (await _client.PlaceAsync(Checkout.Order())).JsonAsync();

        var retry = await _client.PostAsJsonAsync($"/api/v1/orders/{order["id"]}/payment-retries",
            new { card = Checkout.Card(Checkout.ApprovedCard) });

        Assert.Equal(HttpStatusCode.Conflict, retry.StatusCode);
        Assert.Equal("invalid_order_state", (string?)(await retry.JsonAsync())["code"]);
    }

    [Fact]
    public async Task Transient_error_is_retried_automatically()
    {
        var order = await (await _client.PlaceAsync(Checkout.Order(Checkout.FailsOnceCard))).JsonAsync();

        Assert.Equal("Paid", (string?)order["status"]);
        Assert.Equal(["TransientError", "Approved"], order["attempts"]!.AsArray().Select(a => (string?)a!["outcome"]));
    }

    [Theory]
    [InlineData(Checkout.AlwaysFailsCard)]
    [InlineData(Checkout.TimeoutCard)]
    public async Task Gateway_unavailable_ends_as_payment_failed_after_all_attempts(string card)
    {
        var stockBefore = await _client.StockAsync(Checkout.Headphones);

        var order = await (await _client.PlaceAsync(Checkout.Order(card))).JsonAsync();

        Assert.Equal("PaymentFailed", (string?)order["status"]);
        Assert.Equal(3, order["attempts"]!.AsArray().Count);
        Assert.Equal(stockBefore, await _client.StockAsync(Checkout.Headphones));
    }

    [Fact]
    public async Task Same_idempotency_key_returns_the_original_order()
    {
        var key = Guid.NewGuid().ToString();
        var body = Checkout.Order();

        var first = await _client.PlaceAsync(body, key);
        var second = await _client.PlaceAsync(body, key);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var (a, b) = (await first.JsonAsync(), await second.JsonAsync());
        Assert.Equal((string?)a["id"], (string?)b["id"]);
        Assert.Single(b["attempts"]!.AsArray());
    }

    [Fact]
    public async Task Invalid_request_returns_field_errors_in_spanish()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/orders", new
        {
            customer = new { firstName = "", lastName = "López", email = "no-es-correo" },
            productId = Checkout.Headphones,
            quantity = 0,
            card = new { holderName = "ANA", number = "4111111111111112", expiryMonth = 1, expiryYear = 2020, cvv = "1" },
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.JsonAsync();
        Assert.Equal("validation", (string?)problem["code"]);
        var errors = problem["errors"]!.AsObject();
        Assert.Equal(["card.cvv", "card.expiry", "card.number", "customer.email", "customer.firstName", "quantity"],
            errors.Select(e => e.Key).Order());
        Assert.Equal("El número de tarjeta no es válido.", (string?)errors["card.number"]![0]);
    }

    [Fact]
    public async Task Malformed_json_is_rejected_without_internal_details()
    {
        var response = await _client.PostAsync("/api/v1/orders",
            new StringContent("{\"quantity\": \"muchos\"", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("malformed_request", body);
        Assert.DoesNotContain("System.Text.Json", body);
    }

    [Fact]
    public async Task Quantity_above_stock_is_a_conflict()
    {
        var response = await _client.PlaceAsync(Checkout.Order(productId: Checkout.Monitor, quantity: 10));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.JsonAsync();
        Assert.Equal("insufficient_stock", (string?)problem["code"]);
        Assert.StartsWith("Solo hay", (string?)problem["detail"]);
    }

    [Fact]
    public async Task Unknown_product_and_order_return_404()
    {
        var product = await _client.PlaceAsync(Checkout.Order(productId: Guid.NewGuid()));
        var order = await _client.GetAsync($"/api/v1/orders/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, product.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, order.StatusCode);
    }

    [Fact]
    public async Task Correlation_id_is_echoed_and_stored_in_the_audit_trail()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders") { Content = JsonContent.Create(Checkout.Order()) };
        request.Headers.Add("X-Correlation-Id", "it-correlation-0001");

        var response = await _client.SendAsync(request);

        Assert.Equal("it-correlation-0001", response.Headers.GetValues("X-Correlation-Id").Single());
        var events = (await response.JsonAsync())["events"]!.AsArray();
        Assert.All(events, e => Assert.Equal("it-correlation-0001", (string?)e!["correlationId"]));
    }
}
