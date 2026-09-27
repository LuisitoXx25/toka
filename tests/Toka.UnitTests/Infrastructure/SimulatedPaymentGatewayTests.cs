using Microsoft.Extensions.Options;
using Toka.Application.Payments;
using Toka.Domain.Orders;
using Toka.Infrastructure.Payments;
using Toka.UnitTests.Fakes;

namespace Toka.UnitTests.Infrastructure;

public class SimulatedPaymentGatewayTests
{
    private static readonly IOptions<SimulatorOptions> NoLatency =
        Options.Create(new SimulatorOptions { Latency = TimeSpan.Zero, Timeout = TimeSpan.FromMilliseconds(100), CardLimit = 10_000m });

    private readonly SimulatedPaymentGateway _gateway = new(NoLatency, TimeProvider.System);

    private static PaymentRequest Request(string card, int attempt = 1, decimal amount = 100m) =>
        new(Guid.NewGuid(), attempt, amount, "MXN", 1, TestData.Card(card));

    [Theory]
    [InlineData("4111111111111111", 1, PaymentOutcome.Approved)]
    [InlineData(SimulatedPaymentGateway.DeclinedCard, 1, PaymentOutcome.Declined)]
    [InlineData(SimulatedPaymentGateway.AlwaysFailsCard, 3, PaymentOutcome.TransientError)]
    [InlineData(SimulatedPaymentGateway.FailsOnceCard, 1, PaymentOutcome.TransientError)]
    [InlineData(SimulatedPaymentGateway.FailsOnceCard, 2, PaymentOutcome.Approved)]
    public async Task Card_number_drives_the_outcome(string card, int attempt, PaymentOutcome expected)
    {
        var response = await _gateway.AuthorizeAsync(Request(card, attempt), default);

        Assert.Equal(expected, response.Outcome);
    }

    [Fact]
    public async Task Approved_payments_get_an_authorization_code()
    {
        var response = await _gateway.AuthorizeAsync(Request("4111111111111111"), default);

        Assert.Matches("^AUTH-[0-9A-F]{8}$", response.AuthorizationCode);
    }

    [Fact]
    public async Task Amount_over_card_limit_is_declined()
    {
        var response = await _gateway.AuthorizeAsync(Request("4111111111111111", amount: 10_000.01m), default);

        Assert.Equal("limit_exceeded", response.ResponseCode);
    }

    [Fact]
    public async Task Unresponsive_acquirer_is_cut_by_the_timeout()
    {
        var gateway = new TimeoutPaymentGateway(_gateway, NoLatency, TimeProvider.System);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            gateway.AuthorizeAsync(Request(SimulatedPaymentGateway.TimeoutCard), default));
    }
}
