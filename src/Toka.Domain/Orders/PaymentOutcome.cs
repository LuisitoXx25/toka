namespace Toka.Domain.Orders;

public enum PaymentOutcome
{
    Approved = 0,
    Declined = 1,
    /// <summary>Timeout or gateway error; safe to retry.</summary>
    TransientError = 2,
}
