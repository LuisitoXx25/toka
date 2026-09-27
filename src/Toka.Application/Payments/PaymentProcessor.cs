using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Toka.Application.Abstractions;
using Toka.Domain.Orders;
using Toka.Domain.Products;

namespace Toka.Application.Payments;

public sealed class PaymentProcessor(
    IPaymentGateway gateway,
    IUnitOfWork unitOfWork,
    IAuditLog audit,
    TimeProvider clock,
    IOptions<PaymentOptions> options,
    ILogger<PaymentProcessor> logger)
{
    private readonly PaymentOptions _options = options.Value;

    /// <summary>
    /// Authorizes the payment of an order in <see cref="OrderStatus.PendingPayment"/> and leaves it in its final state:
    /// approved → Paid; declined → PaymentDeclined (no retry, retrying the same card won't change the answer);
    /// transient error → retried with backoff up to <see cref="PaymentOptions.MaxAttempts"/>, then PaymentFailed.
    /// Declined and failed orders release their reserved stock.
    /// </summary>
    public async Task ProcessAsync(Order order, Product product, CardDetails card, CancellationToken ct)
    {
        for (var i = 1; i <= _options.MaxAttempts; i++)
        {
            var response = await AuthorizeSafelyAsync(order, card, ct);
            var attempt = order.RecordAttempt(response.Outcome, response.ResponseCode, response.Message,
                response.AuthorizationCode, card.Last4, card.Brand, clock.GetUtcNow());
            audit.Record(AuditEvents.PaymentAttempted,
                $"Intento de pago #{attempt.AttemptNumber}: {DescribeOutcome(attempt.Outcome)} (tarjeta ****{attempt.CardLast4}).",
                nameof(Order), order.Id,
                new { attempt.AttemptNumber, Outcome = attempt.Outcome.ToString(), attempt.ResponseCode });

            logger.LogInformation("Payment attempt {AttemptNumber} for order {OrderId}: {Outcome} ({ResponseCode})",
                attempt.AttemptNumber, order.Id, response.Outcome, response.ResponseCode);

            switch (response.Outcome)
            {
                case PaymentOutcome.Approved:
                    order.MarkPaid(response.AuthorizationCode ?? response.ResponseCode, clock.GetUtcNow());
                    audit.Record(AuditEvents.OrderPaid, $"Pago aprobado. Autorización {order.AuthorizationCode}.",
                        nameof(Order), order.Id, new { order.AuthorizationCode, order.Total });
                    await unitOfWork.SaveChangesAsync(ct);
                    return;

                case PaymentOutcome.Declined:
                    order.MarkDeclined(product, response.Message ?? "El banco emisor rechazó el pago.", clock.GetUtcNow());
                    audit.Record(AuditEvents.OrderDeclined, $"Pago rechazado: {order.FailureReason}",
                        nameof(Order), order.Id, new { response.ResponseCode });
                    RecordStockRelease(order);
                    await unitOfWork.SaveChangesAsync(ct);
                    return;

                case PaymentOutcome.TransientError when i < _options.MaxAttempts:
                    // Persist the failed attempt before waiting so it is visible even if the process stops.
                    await unitOfWork.SaveChangesAsync(ct);
                    var delay = _options.BaseRetryDelay * Math.Pow(2, i - 1);
                    if (delay > TimeSpan.Zero) await Task.Delay(delay, clock, ct);
                    break;
            }
        }

        order.MarkPaymentFailed(product,
            $"El procesador de pagos no respondió después de {_options.MaxAttempts} intento(s). Intenta de nuevo más tarde.",
            clock.GetUtcNow());
        audit.Record(AuditEvents.OrderPaymentFailed, "Pago fallido: el procesador de pagos no estuvo disponible.",
            nameof(Order), order.Id, new { _options.MaxAttempts });
        RecordStockRelease(order);
        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<PaymentResponse> AuthorizeSafelyAsync(Order order, CardDetails card, CancellationToken ct)
    {
        try
        {
            return await gateway.AuthorizeAsync(
                new PaymentRequest(order.Id, order.NextAttemptNumber, order.Total, order.Currency, card), ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Payment gateway error for order {OrderId}", order.Id);
            return new PaymentResponse(PaymentOutcome.TransientError, "gateway_error", "Error de comunicación con el procesador de pagos.", null);
        }
    }

    private void RecordStockRelease(Order order) =>
        audit.Record(AuditEvents.StockReleased, $"Se liberaron {order.Quantity} unidad(es) del inventario.",
            nameof(Product), order.ProductId, new { OrderId = order.Id, order.Quantity });

    private static string DescribeOutcome(PaymentOutcome outcome) => outcome switch
    {
        PaymentOutcome.Approved => "aprobado",
        PaymentOutcome.Declined => "rechazado",
        _ => "error temporal",
    };
}
