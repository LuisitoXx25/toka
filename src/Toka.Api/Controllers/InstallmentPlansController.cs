using Microsoft.AspNetCore.Mvc;
using Toka.Api.Infrastructure;
using Toka.Application.Common;
using Toka.Application.Payments;
using Toka.Domain.Orders;

namespace Toka.Api.Controllers;

/// <summary>Plans for an amount. <c>CardType</c> is Credit, Debit or Unknown (no BIN sent).</summary>
public sealed record InstallmentPlansResponse(string CardType, string? CardTypeDisplay, IReadOnlyList<InstallmentPlan> Plans);

[ApiController]
[Route("api/v1/installment-plans")]
public sealed class InstallmentPlansController(InstallmentPolicy policy, ICardBinLookup binLookup) : ControllerBase
{
    /// <summary>
    /// Payment options for a purchase amount: single payment plus the MSI plans whose minimum is met.
    /// Send the card BIN (first 6 digits) to resolve credit/debit; debit cards only get the single payment.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<InstallmentPlansResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public ActionResult<InstallmentPlansResponse> Get([FromQuery] decimal amount, [FromQuery] string? bin)
    {
        var errors = new Dictionary<string, string[]>();
        if (amount is <= 0 or > 10_000_000) errors["amount"] = ["El monto debe ser mayor que cero."];
        if (bin is not null && (bin.Length != 6 || !bin.All(char.IsAsciiDigit))) errors["bin"] = ["El BIN debe tener 6 dígitos."];
        if (errors.Count > 0)
            return this.ToProblem(new Error("validation", "Uno o más campos no son válidos.", ErrorType.Validation, errors));

        var cardType = bin is null ? CardType.Unknown : binLookup.Lookup(bin);
        return Ok(new InstallmentPlansResponse(
            cardType.ToString(),
            cardType == CardType.Unknown ? null : cardType.ToDisplayName(),
            policy.PlansFor(amount, cardType)));
    }
}
