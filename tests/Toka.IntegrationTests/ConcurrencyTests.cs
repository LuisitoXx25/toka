using System.Net;

namespace Toka.IntegrationTests;

[Collection(ApiCollection.Name)]
public class ConcurrencyTests(ApiFactory factory)
{
    [Fact]
    public async Task Concurrent_checkouts_never_oversell_stock()
    {
        var client = factory.CreateAuthenticatedClient();
        var stockBefore = await client.StockAsync(Checkout.Monitor);

        var responses = await Task.WhenAll(Enumerable.Range(0, 8)
            .Select(_ => client.PlaceAsync(Checkout.Order(productId: Checkout.Monitor))));

        var created = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        Assert.All(responses, r => Assert.Contains(r.StatusCode, new[] { HttpStatusCode.Created, HttpStatusCode.Conflict }));
        Assert.InRange(created, 1, stockBefore);
        Assert.Equal(stockBefore - created, await client.StockAsync(Checkout.Monitor));
    }
}
