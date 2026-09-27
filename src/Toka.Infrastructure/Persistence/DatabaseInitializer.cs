using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Toka.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    /// <summary>
    /// Applies pending EF Core migrations. Uses <c>ConnectionStrings:Migrations</c> (schema owner) when present,
    /// because the runtime app user has no DDL rights; falls back to <c>ConnectionStrings:Default</c> for local dev and tests.
    /// </summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        var configuration = services.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString("Migrations")
            ?? configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("No connection string configured for migrations.");

        var options = new DbContextOptionsBuilder<AppDbContext>();
        DependencyInjection.ConfigureDbContext(options, connectionString);
        await using var db = new AppDbContext(options.Options);
        await db.Database.MigrateAsync(ct);
    }
}
