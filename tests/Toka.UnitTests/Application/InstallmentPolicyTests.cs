using Toka.UnitTests.Fakes;

namespace Toka.UnitTests.Application;

public class InstallmentPolicyTests
{
    [Theory]
    [InlineData(1000, new[] { 1 })]
    [InlineData(1500, new[] { 1, 3 })]
    [InlineData(3499, new[] { 1, 3, 6 })]
    public void Offers_plans_whose_minimum_is_met(decimal amount, int[] months) =>
        Assert.Equal(months, Installments.Policy.PlansFor(amount).Select(p => p.Months));

    [Fact]
    public void Plans_show_the_monthly_payment_in_spanish()
    {
        var plan = Installments.Policy.PlansFor(3499m).Single(p => p.Months == 3);

        Assert.Equal("3 pagos de $1,166.33 sin intereses", plan.Label);
        Assert.Equal("1 pago de $3,499.00", Installments.Policy.PlansFor(3499m)[0].Label);
        Assert.Equal(1166.33m, plan.MonthlyPayment);
    }

    [Theory]
    [InlineData(1, 100, true)]
    [InlineData(3, 1500, true)]
    [InlineData(6, 2999.99, false)]
    [InlineData(12, 50000, false)]
    public void Validates_requested_plan(int months, decimal amount, bool allowed) =>
        Assert.Equal(allowed, Installments.Policy.Reject(months, amount) is null);
}
