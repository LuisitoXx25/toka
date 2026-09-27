using Microsoft.AspNetCore.Mvc;

namespace Toka.Api.Infrastructure;

/// <summary>Builds and writes RFC 9457 problem responses with the same shape everywhere: Spanish text, <c>code</c>, <c>correlationId</c>.</summary>
public static class Problems
{
    public const string ContentType = "application/problem+json";

    private static readonly Dictionary<int, string> DefaultTitles = new()
    {
        [StatusCodes.Status400BadRequest] = "La solicitud no es válida.",
        [StatusCodes.Status401Unauthorized] = "No autenticado.",
        [StatusCodes.Status403Forbidden] = "No tienes permiso para realizar esta operación.",
        [StatusCodes.Status404NotFound] = "Recurso no encontrado.",
        [StatusCodes.Status405MethodNotAllowed] = "Método no permitido.",
        [StatusCodes.Status413PayloadTooLarge] = "La solicitud es demasiado grande.",
        [StatusCodes.Status415UnsupportedMediaType] = "Tipo de contenido no soportado. Usa application/json.",
        [StatusCodes.Status429TooManyRequests] = "Demasiadas solicitudes.",
        [StatusCodes.Status500InternalServerError] = "Ocurrió un error inesperado.",
    };

    /// <summary>Adds instance and correlation id, and replaces framework-generated English titles with Spanish ones.</summary>
    public static void Enrich(ProblemDetails problem, HttpContext context)
    {
        problem.Instance ??= context.Request.Path;
        if (context.Response.Headers.TryGetValue(CorrelationIdMiddleware.HeaderName, out var correlationId))
            problem.Extensions["correlationId"] = correlationId.ToString();

        // Framework problems (unknown route, wrong method...) come without detail and with an English title.
        if (problem.Detail is null && problem.Status is { } status && DefaultTitles.TryGetValue(status, out var title))
        {
            problem.Title = title;
            problem.Type = null;
        }
    }

    /// <summary>Writes a problem response directly, for code outside MVC (auth challenge, rate limiter).</summary>
    public static Task WriteAsync(HttpContext context, int status, string title, string detail, string code, CancellationToken ct = default)
    {
        var problem = new ProblemDetails { Status = status, Title = title, Detail = detail, Extensions = { ["code"] = code } };
        Enrich(problem, context);
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(problem, options: null, contentType: ContentType, cancellationToken: ct);
    }
}
