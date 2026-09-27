using FluentValidation;

namespace Toka.Application.Customers;

public sealed class CustomerInputValidator : AbstractValidator<CustomerInput>
{
    public CustomerInputValidator()
    {
        RuleFor(c => c.FirstName).NotEmpty().MaximumLength(100).WithName("Nombre");
        RuleFor(c => c.LastName).NotEmpty().MaximumLength(100).WithName("Apellido");
        RuleFor(c => c.Email).NotEmpty().MaximumLength(254).EmailAddress().WithName("Correo electrónico");
        RuleFor(c => c.Phone)
            .Matches(@"^\+?[0-9 ()-]{7,20}$").WithMessage("El teléfono no tiene un formato válido.")
            .When(c => !string.IsNullOrWhiteSpace(c.Phone));
    }
}
