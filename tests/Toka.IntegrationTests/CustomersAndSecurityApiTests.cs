using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Toka.IntegrationTests;

[Collection(ApiCollection.Name)]
public class CustomersAndSecurityApiTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateAuthenticatedClient();

    [Fact]
    public async Task Requests_without_api_key_are_rejected()
    {
        var response = await factory.CreateClient().GetAsync("/api/v1/products");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("No autenticado.", (string?)(await response.JsonAsync())["title"]);
    }

    [Fact]
    public async Task Wrong_api_key_is_rejected()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "not-the-key");

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/products")).StatusCode);
    }

    [Fact]
    public async Task Health_checks_are_public()
    {
        var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        Assert.Equal("Healthy", await client.GetStringAsync("/health/ready"));
    }

    [Fact]
    public async Task Catalog_lists_seeded_products_with_security_headers()
    {
        var response = await _client.GetAsync("/api/v1/products");

        var products = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        Assert.Equal(4, products!.Length);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Customer_can_be_registered_once_per_email()
    {
        var customer = new { firstName = "Luis", lastName = "Pérez", email = $"Luis.{Guid.NewGuid():N}@Correo.MX" };

        var created = await _client.PostAsJsonAsync("/api/v1/customers", customer);
        var duplicate = await _client.PostAsJsonAsync("/api/v1/customers", customer);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var body = await created.JsonAsync();
        Assert.Equal(customer.email.ToLowerInvariant(), (string?)body["email"]);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync(created.Headers.Location)).StatusCode);

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("customer_exists", (string?)(await duplicate.JsonAsync())["code"]);
    }
}
