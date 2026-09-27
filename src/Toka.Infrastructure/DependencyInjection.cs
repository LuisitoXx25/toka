using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Toka.Application.Abstractions;
using Toka.Application.Payments;
using Toka.Infrastructure.Auditing;
using Toka.Infrastructure.Payments;
using Toka.Infrastructure.Persistence;
using Toka.Infrastructure.Persistence.Repositories;

namespace Toka.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers persistence, audit log and the payment simulator. The connection string comes from
    /// <c>ConnectionStrings:Default</c> (env var <c>ConnectionStrings__Default</c> in containers); it is never hardcoded.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

        services.AddDbContext<AppDbContext>(o => ConfigureDbContext(o, connectionString));
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAuditLog, AuditLog>();

        services.Configure<PaymentOptions>(configuration.GetSection(PaymentOptions.Section));
        services.Configure<SimulatorOptions>(configuration.GetSection(SimulatorOptions.Section));
        services.AddSingleton<SimulatedPaymentGateway>();
        services.AddSingleton<IPaymentGateway>(sp => new TimeoutPaymentGateway(
            sp.GetRequiredService<SimulatedPaymentGateway>(),
            sp.GetRequiredService<IOptions<SimulatorOptions>>(),
            sp.GetRequiredService<TimeProvider>()));

        return services;
    }

    internal static void ConfigureDbContext(DbContextOptionsBuilder options, string connectionString) =>
        options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
}
