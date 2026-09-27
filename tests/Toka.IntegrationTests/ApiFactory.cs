using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Toka.IntegrationTests;

/// <summary>
/// Runs the real API against a disposable PostgreSQL container. Mirrors production access:
/// migrations run as the schema owner, the API connects as the restricted <c>toka_app</c> role.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string ApiKey = "integration-test-key";
    private const string AppPassword = "app-test-pw";

    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("toka")
        .Build();

    public string OwnerConnectionString => _db.GetConnectionString();

    public string ConnectionStringFor(string user, string password) =>
        new NpgsqlConnectionStringBuilder(OwnerConnectionString) { Username = user, Password = password }.ConnectionString;

    public string AppConnectionString => ConnectionStringFor("toka_app", AppPassword);

    public async Task InitializeAsync()
    {
        await _db.StartAsync();
        // Same roles the Docker init script creates (deploy/postgres/init/01-roles.sh).
        await ExecuteAsOwnerAsync($"""
            CREATE ROLE toka_app_role NOLOGIN;
            CREATE ROLE toka_corrections NOLOGIN;
            CREATE ROLE toka_app LOGIN PASSWORD '{AppPassword}' IN ROLE toka_app_role;
            """);
        _ = Server; // starts the host, which applies migrations
    }

    Task IAsyncLifetime.DisposeAsync() => Task.WhenAll(base.DisposeAsync().AsTask(), _db.DisposeAsync().AsTask());

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", AppConnectionString);
        builder.UseSetting("ConnectionStrings:Migrations", OwnerConnectionString);
        builder.UseSetting("Database:MigrateOnStartup", "true");
        builder.UseSetting("Security:ApiKeys:0", ApiKey);
        builder.UseSetting("Payments:BaseRetryDelay", "00:00:00");
        builder.UseSetting("PaymentSimulator:Latency", "00:00:00");
        builder.UseSetting("PaymentSimulator:Timeout", "00:00:00.300");
        builder.UseSetting("RateLimiting:GlobalPerMinute", "10000");
        builder.UseSetting("RateLimiting:CheckoutPerMinute", "10000");
    }

    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ApiKey);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    public async Task ExecuteAsOwnerAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(OwnerConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
