using Toka.Application.Customers;
using Toka.Application.Orders;
using Toka.Application.Payments;

namespace Toka.Api.Contracts;

public sealed record CustomerRequest(string FirstName, string LastName, string Email, string? Phone)
{
    public CustomerInput ToInput() => new(FirstName, LastName, Email, Phone);
}

public sealed record CardRequest(string HolderName, string Number, int ExpiryMonth, int ExpiryYear, string Cvv)
{
    public CardDetails ToDetails() => new(HolderName, Number, ExpiryMonth, ExpiryYear, Cvv);

    // Records print every property by default; keep the PAN and CVV out of any log.
    public override string ToString() => "CardRequest { *** }";
}

public sealed record PlaceOrderRequest(CustomerRequest Customer, Guid ProductId, int Quantity, CardRequest Card)
{
    public PlaceOrderCommand ToCommand(string? idempotencyKey) =>
        new(Customer?.ToInput()!, ProductId, Quantity, Card?.ToDetails()!, idempotencyKey);
}

public sealed record RetryPaymentRequest(CardRequest Card);
