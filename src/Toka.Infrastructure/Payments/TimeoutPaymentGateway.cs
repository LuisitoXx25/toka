using Microsoft.Extensions.Options;
using Polly;
using Toka.Application.Payments;

namespace Toka.Infrastructure.Payments;

/// <summary>
/// Decorator that bounds how long we wait for the acquirer. A timeout surfaces as an exception, which
/// <see cref="PaymentProcessor"/> treats as a transient error and retries.
/// </summary>
internal sealed class TimeoutPaymentGateway : IPaymentGateway
{
    private readonly IPaymentGateway _inner;
    private readonly ResiliencePipeline _pipeline;

    public TimeoutPaymentGateway(IPaymentGateway inner, IOptions<SimulatorOptions> options, TimeProvider clock)
    {
        _inner = inner;
        _pipeline = new ResiliencePipelineBuilder { TimeProvider = clock }
            .AddTimeout(options.Value.Timeout)
            .Build();
    }

    public async Task<PaymentResponse> AuthorizeAsync(PaymentRequest request, CancellationToken ct) =>
        await _pipeline.ExecuteAsync(async token => await _inner.AuthorizeAsync(request, token), ct);
}
