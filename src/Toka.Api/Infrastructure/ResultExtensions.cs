using Microsoft.AspNetCore.Mvc;
using Toka.Application.Common;

namespace Toka.Api.Infrastructure;

public static class ResultExtensions
{
    /// <summary>
    /// Maps a business <see cref="Error"/> to an RFC 9457 problem response:
    /// Validation → 400, NotFound → 404, Conflict → 409, BusinessRule → 422.
    /// The stable error code is returned in the <c>code</c> extension.
    /// </summary>
    public static ActionResult ToProblem(this ControllerBase controller, Error error)
    {
        var (status, title) = error.Type switch
        {
            ErrorType.Validation => (StatusCodes.Status400BadRequest, "La solicitud contiene datos inválidos."),
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "Recurso no encontrado."),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "La operación no se puede completar en el estado actual."),
            _ => (StatusCodes.Status422UnprocessableEntity, "La operación no cumple las reglas de negocio."),
        };

        ProblemDetails problem = error.Details is { Count: > 0 }
            ? new ValidationProblemDetails(error.Details.ToDictionary(kv => ToJsonPath(kv.Key), kv => kv.Value))
            : new ProblemDetails();
        problem.Status = status;
        problem.Title = title;
        problem.Detail = error.Message;
        problem.Extensions["code"] = error.Code;
        Problems.Enrich(problem, controller.HttpContext);

        return new ObjectResult(problem) { StatusCode = status };
    }

    // "Customer.Email" → "customer.email", so keys match the JSON field names the client sent.
    private static string ToJsonPath(string propertyPath) =>
        string.Join('.', propertyPath.Split('.').Select(p => p.Length == 0 ? p : char.ToLowerInvariant(p[0]) + p[1..]));
}
