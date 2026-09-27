using System.Text.RegularExpressions;
using Serilog.Context;
using Toka.Application.Abstractions;

namespace Toka.Api.Infrastructure;

/// <summary>
/// Reads <c>X-Correlation-Id</c> (or creates one), returns it in the response and attaches it to every log line
/// and audit event of the request, so a single operation can be traced end to end.
/// </summary>
public sealed partial class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context, HttpCorrelationContext correlation)
    {
        var incoming = context.Request.Headers[HeaderName].ToString();
        // Client-supplied values end up in logs; accept only a short, safe charset to avoid log injection.
        var correlationId = SafeId().IsMatch(incoming) ? incoming : Guid.NewGuid().ToString("N");

        correlation.CorrelationId = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }

    [GeneratedRegex("^[A-Za-z0-9._-]{8,64}$")]
    private static partial Regex SafeId();
}

public sealed class HttpCorrelationContext : ICorrelationContext
{
    public string? CorrelationId { get; set; }
}
