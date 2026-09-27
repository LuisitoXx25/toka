using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Toka.Api.Infrastructure;

public static class RateLimitPolicies
{
    public const string Checkout = "checkout";
    public const string Lookup = "lookup";

    /// <summary>
    /// Global limit per client IP plus a tighter one for endpoints that hit the payment gateway,
    /// which slows down card-testing attacks. Limits come from the <c>RateLimiting</c> config section.
    /// </summary>
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var globalPerMinute = configuration.GetValue("RateLimiting:GlobalPerMinute", 120);
        var checkoutPerMinute = configuration.GetValue("RateLimiting:CheckoutPerMinute", 10);
        var lookupPerMinute = configuration.GetValue("RateLimiting:LookupPerMinute", 20);

        return services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                RateLimitPartition.GetFixedWindowLimiter(ClientKey(ctx), _ => Window(globalPerMinute)));

            options.AddPolicy(Checkout, ctx =>
                RateLimitPartition.GetFixedWindowLimiter(ClientKey(ctx), _ => Window(checkoutPerMinute)));

            // Slows down guessing of order id + email pairs.
            options.AddPolicy(Lookup, ctx =>
                RateLimitPartition.GetFixedWindowLimiter(ClientKey(ctx), _ => Window(lookupPerMinute)));

            options.OnRejected = async (context, ct) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();

                await Problems.WriteAsync(context.HttpContext, StatusCodes.Status429TooManyRequests,
                    "Demasiadas solicitudes.", "Espera un momento antes de intentarlo de nuevo.", "rate_limited", ct);
            };
        });
    }

    private static string ClientKey(HttpContext ctx) => ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static FixedWindowRateLimiterOptions Window(int permitsPerMinute) => new()
    {
        PermitLimit = permitsPerMinute,
        Window = TimeSpan.FromMinutes(1),
        QueueLimit = 0,
    };
}
