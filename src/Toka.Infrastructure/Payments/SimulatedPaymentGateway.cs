using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Toka.Application.Payments;
using Toka.Domain.Orders;

namespace Toka.Infrastructure.Payments;

/// <summary>
/// Deterministic acquirer simulator. Behavior depends on the card number so every scenario can be reproduced:
/// <list type="table">
///   <item><term>4000000000000002</term><description>Declined: insufficient funds.</description></item>
///   <item><term>4000000000000119</term><description>Always a transient processing error (order ends as PaymentFailed).</description></item>
///   <item><term>4000000000000259</term><description>Transient error on the first attempt, approved on the retry.</description></item>
///   <item><term>4000000000000341</term><description>Never answers; the timeout turns it into a transient error.</description></item>
///   <item><term>Any other valid card</term><description>Approved, unless the amount exceeds the card limit.</description></item>
/// </list>
/// </summary>
public sealed class SimulatedPaymentGateway(IOptions<SimulatorOptions> options, TimeProvider clock) : IPaymentGateway
{
    public const string DeclinedCard = "4000000000000002";
    public const string AlwaysFailsCard = "4000000000000119";
    public const string FailsOnceCard = "4000000000000259";
    public const string TimeoutCard = "4000000000000341";

    private readonly SimulatorOptions _options = options.Value;

    public async Task<PaymentResponse> AuthorizeAsync(PaymentRequest request, CancellationToken ct)
    {
        if (request.Card.Number == TimeoutCard)
            await Task.Delay(Timeout.InfiniteTimeSpan, clock, ct);

        if (_options.Latency > TimeSpan.Zero)
            await Task.Delay(_options.Latency, clock, ct);

        return request.Card.Number switch
        {
            DeclinedCard => Declined("insufficient_funds", "Fondos insuficientes."),
            AlwaysFailsCard => Transient(),
            FailsOnceCard when request.AttemptNumber == 1 => Transient(),
            _ when request.Amount > _options.CardLimit => Declined("limit_exceeded", "El monto excede el límite de la tarjeta."),
            _ => new PaymentResponse(PaymentOutcome.Approved, "approved", "Pago aprobado.", NewAuthorizationCode()),
        };
    }

    private static PaymentResponse Declined(string code, string message) => new(PaymentOutcome.Declined, code, message, null);

    private static PaymentResponse Transient() =>
        new(PaymentOutcome.TransientError, "processing_error", "Error temporal del procesador de pagos.", null);

    private static string NewAuthorizationCode() => $"AUTH-{RandomNumberGenerator.GetHexString(8)}";
}
