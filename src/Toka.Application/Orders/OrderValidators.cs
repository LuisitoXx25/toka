using FluentValidation;
using Toka.Application.Customers;
using Toka.Application.Payments;

namespace Toka.Application.Orders;

public sealed class PlaceOrderValidator : AbstractValidator<PlaceOrderCommand>
{
    public const int MaxQuantity = 10;

    public PlaceOrderValidator(IValidator<CustomerInput> customer, IValidator<CardDetails> card)
    {
        RuleFor(c => c.Customer).NotNull().WithName("Cliente").SetValidator(customer);
        RuleFor(c => c.ProductId).NotEmpty().WithName("Producto");
        RuleFor(c => c.Quantity).InclusiveBetween(1, MaxQuantity).WithName("Cantidad");
        RuleFor(c => c.Installments).GreaterThanOrEqualTo(1).WithName("Meses");
        RuleFor(c => c.Card).NotNull().WithName("Tarjeta").SetValidator(card);
        RuleFor(c => c.IdempotencyKey).MaximumLength(100).WithName("Idempotency-Key");
    }
}

public sealed class RetryPaymentValidator : AbstractValidator<RetryPaymentCommand>
{
    public RetryPaymentValidator(IValidator<CardDetails> card)
    {
        RuleFor(c => c.OrderId).NotEmpty().WithName("Orden");
        RuleFor(c => c.CustomerEmail).NotEmpty().MaximumLength(254).EmailAddress().WithName("Correo electrónico");
        RuleFor(c => c.Card).NotNull().WithName("Tarjeta").SetValidator(card);
    }
}

public sealed class LookupOrderValidator : AbstractValidator<LookupOrderQuery>
{
    public LookupOrderValidator()
    {
        RuleFor(q => q.OrderId).NotEmpty().WithName("Número de orden");
        RuleFor(q => q.CustomerEmail).NotEmpty().MaximumLength(254).EmailAddress().WithName("Correo electrónico");
    }
}
