using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Toka.IntegrationTests;

internal static class Checkout
{
    public static readonly Guid Headphones = Guid.Parse("0199a0d4-0000-7000-8000-000000000002");
    public static readonly Guid Keyboard = Guid.Parse("0199a0d4-0000-7000-8000-000000000003");
    public static readonly Guid Monitor = Guid.Parse("0199a0d4-0000-7000-8000-000000000004");

    public const string ApprovedCard = "4111111111111111";
    public const string DeclinedCard = "4000000000000002";
    public const string AlwaysFailsCard = "4000000000000119";
    public const string FailsOnceCard = "4000000000000259";
    public const string TimeoutCard = "4000000000000341";

    public static object Card(string number) => new { holderName = "ANA LOPEZ", number, expiryMonth = 12, expiryYear = 2030, cvv = "123" };

    public static object Order(string card = ApprovedCard, Guid? productId = null, int quantity = 1, string? email = null, int installments = 1) => new
    {
        customer = new { firstName = "Ana", lastName = "López", email = email ?? $"ana.{Guid.NewGuid():N}@correo.mx", phone = "+52 55 1234 5678" },
        productId = productId ?? Headphones,
        quantity,
        card = Card(card),
        installments,
    };

    public static Task<HttpResponseMessage> PlaceAsync(this HttpClient client, object order, string? idempotencyKey = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders") { Content = JsonContent.Create(order) };
        if (idempotencyKey is not null) request.Headers.Add("Idempotency-Key", idempotencyKey);
        return client.SendAsync(request);
    }

    public static async Task<JsonNode> JsonAsync(this HttpResponseMessage response) =>
        JsonNode.Parse(await response.Content.ReadAsStringAsync())!;

    public static async Task<int> StockAsync(this HttpClient client, Guid productId) =>
        (await client.GetFromJsonAsync<JsonElement>($"/api/v1/products/{productId}")).GetProperty("stock").GetInt32();
}
