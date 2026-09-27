using Toka.Application.Payments;
using Toka.Domain.Orders;
using Toka.UnitTests.Fakes;

namespace Toka.UnitTests.Application;

public class InstallmentPolicyTests
{
    [Theory]
    [InlineData(1000, new[] { 1 })]
    [InlineData(1500, new[] { 1, 3 })]
    [InlineData(3499, new[] { 1, 3, 6 })]
    public void Offers_plans_whose_minimum_is_met(decimal amount, int[] months) =>
        Assert.Equal(months, Installments.Policy.PlansFor(amount, CardType.Credit).Select(p => p.Months));

    [Fact]
    public void Debit_cards_only_get_a_single_payment() =>
        Assert.Equal([1], Installments.Policy.PlansFor(3499m, CardType.Debit).Select(p => p.Months));

    [Fact]
    public void Labels_state_the_first_payment_when_the_split_is_not_exact()
    {
        var plans = Installments.Policy.PlansFor(3499m, CardType.Credit);

        Assert.Equal("1 pago de $3,499.00", plans[0].Label);
        Assert.Equal("3 pagos de $1,166.33 sin intereses; el primero de $1,166.34", plans[1].Label);
        Assert.Equal((1166.34m, 1166.33m), (plans[1].FirstPayment, plans[1].MonthlyPayment));
    }

    [Fact]
    public void Exact_split_has_a_single_amount_in_the_label() =>
        Assert.Equal("6 pagos de $500.00 sin intereses", InstallmentPolicy.Describe(InstallmentSchedule.Of(3000m, 6)));

    [Theory]
    [InlineData(1, 100, CardType.Debit, true)]
    [InlineData(3, 1500, CardType.Credit, true)]
    [InlineData(3, 1500, CardType.Debit, false)]
    [InlineData(6, 2999.99, CardType.Credit, false)]
    [InlineData(12, 50000, CardType.Credit, false)]
    public void Validates_requested_plan(int months, decimal amount, CardType cardType, bool allowed) =>
        Assert.Equal(allowed, Installments.Policy.Reject(months, amount, cardType) is null);
}
