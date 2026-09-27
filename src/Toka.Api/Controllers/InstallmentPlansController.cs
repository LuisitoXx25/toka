using Microsoft.AspNetCore.Mvc;
using Toka.Api.Infrastructure;
using Toka.Application.Common;
using Toka.Application.Payments;

namespace Toka.Api.Controllers;

[ApiController]
[Route("api/v1/installment-plans")]
public sealed class InstallmentPlansController(InstallmentPolicy policy) : ControllerBase
{
    /// <summary>Payment options for a purchase amount: single payment plus the MSI plans whose minimum is met.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<InstallmentPlan>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public ActionResult<IReadOnlyList<InstallmentPlan>> Get([FromQuery] decimal amount)
    {
        if (amount is <= 0 or > 10_000_000)
        {
            return this.ToProblem(new Error("validation", "Uno o más campos no son válidos.", ErrorType.Validation,
                new Dictionary<string, string[]> { ["amount"] = ["El monto debe ser mayor que cero."] }));
        }
        return Ok(policy.PlansFor(amount));
    }
}
