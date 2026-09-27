using Toka.Domain.Orders;

namespace Toka.UnitTests.Domain;

public class InstallmentScheduleTests
{
    [Theory]
    [InlineData(100.00, 3, 33.34, 33.33)]
    [InlineData(3499.00, 3, 1166.34, 1166.33)]
    [InlineData(3499.00, 6, 583.20, 583.16)]
    [InlineData(3000.00, 6, 500.00, 500.00)]
    [InlineData(3499.00, 1, 3499.00, 3499.00)]
    public void First_payment_absorbs_the_rounding(decimal total, int months, decimal first, decimal regular)
    {
        var schedule = InstallmentSchedule.Of(total, months);

        Assert.Equal(first, schedule.FirstPayment);
        Assert.Equal(regular, schedule.RegularPayment);
    }

    [Fact]
    public void Payments_always_add_up_to_the_total()
    {
        foreach (var months in new[] { 3, 6, 9, 12 })
        {
            for (var cents = 150_000; cents <= 400_000; cents += 13)
            {
                var total = cents / 100m;
                var schedule = InstallmentSchedule.Of(total, months);
                Assert.Equal(total, schedule.FirstPayment + schedule.RegularPayment * (months - 1));
                Assert.InRange(schedule.FirstPayment - schedule.RegularPayment, 0m, (months - 1) / 100m);
            }
        }
    }
}
