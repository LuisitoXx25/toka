using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Toka.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    /// <summary>Applies pending EF Core migrations (schema + seed catalog). Safe to run on every startup.</summary>
    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync(ct);
    }
}
