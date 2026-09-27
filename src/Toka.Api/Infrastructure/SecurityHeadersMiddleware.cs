namespace Toka.Api.Infrastructure;

/// <summary>Baseline OWASP headers. API responses are JSON only, so they get the strictest CSP and no caching.</summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers.XContentTypeOptions = "nosniff";
        headers.XFrameOptions = "DENY";
        headers["Referrer-Policy"] = "no-referrer";

        if (context.Request.Path.StartsWithSegments("/api"))
        {
            headers.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";
            headers.CacheControl = "no-store";
        }

        return next(context);
    }
}
