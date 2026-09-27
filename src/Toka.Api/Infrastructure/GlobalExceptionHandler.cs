using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Toka.Api.Infrastructure;

/// <summary>
/// Last line of defense: logs the full exception and returns a generic 500 without internal details
/// (no stack traces or SQL errors to the client). The correlation id lets support find the log entry.
/// </summary>
public sealed class GlobalExceptionHandler(IProblemDetailsService problemDetails, ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
        {
            // Client went away; nothing useful to send back.
            context.Response.StatusCode = 499;
            return true;
        }

        logger.LogError(exception, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Ocurrió un error inesperado.",
                Detail = "Intenta de nuevo más tarde. Si el problema persiste, contacta a soporte con el identificador de correlación.",
                Extensions = { ["code"] = "internal_error" },
            },
        });
    }
}
