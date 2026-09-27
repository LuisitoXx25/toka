using System.Globalization;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Toka.Application.Customers;
using Toka.Application.Orders;
using Toka.Application.Payments;
using Toka.Application.Products;

namespace Toka.Application;

public static class DependencyInjection
{
    /// <summary>Registers use-case services and validators. Validation messages are always in Spanish.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("es");

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<CustomerService>();
        services.AddScoped<ProductService>();
        services.AddScoped<OrderService>();
        services.AddScoped<PaymentProcessor>();
        return services;
    }
}
