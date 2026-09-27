using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Toka.Infrastructure.Persistence;

/// <summary>Used only by <c>dotnet ef</c> to generate migrations; no database connection is opened.</summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>();
        DependencyInjection.ConfigureDbContext(options, "Host=localhost;Database=toka;Username=design;Password=design");
        return new AppDbContext(options.Options);
    }
}
