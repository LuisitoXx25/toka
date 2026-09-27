using FluentValidation;

namespace Toka.Application.Payments;

public sealed class CardValidator : AbstractValidator<CardDetails>
{
    public CardValidator(TimeProvider clock)
    {
        RuleFor(c => c.HolderName).NotEmpty().MaximumLength(100).WithName("Titular");
        RuleFor(c => c.Number)
            .NotEmpty().WithName("Número de tarjeta")
            .Matches("^[0-9]{13,19}$").WithMessage("El número de tarjeta debe tener entre 13 y 19 dígitos.")
            .Must(CardRules.PassesLuhn).WithMessage("El número de tarjeta no es válido.");
        RuleFor(c => c.ExpiryMonth).InclusiveBetween(1, 12).WithName("Mes de expiración");
        RuleFor(c => c.ExpiryYear).InclusiveBetween(2000, 2100).WithName("Año de expiración");
        RuleFor(c => c)
            .Must(c => !CardRules.IsExpired(c.ExpiryMonth, c.ExpiryYear, clock.GetUtcNow()))
            .WithName("Expiry").OverridePropertyName("Expiry")
            .WithMessage("La tarjeta está vencida.")
            .When(c => c.ExpiryMonth is >= 1 and <= 12);
        RuleFor(c => c.Cvv)
            .NotEmpty().WithName("CVV")
            .Matches("^[0-9]{3,4}$").WithMessage("El CVV debe tener 3 o 4 dígitos.");
    }
}
