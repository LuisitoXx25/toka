using System.Globalization;
using FluentValidation;
using Microsoft.Extensions.Time.Testing;
using Toka.Application.Customers;
using Toka.Application.Orders;
using Toka.Application.Payments;
using Toka.UnitTests.Fakes;

namespace Toka.UnitTests.Application;

public class ValidatorTests
{
    private readonly PlaceOrderValidator _validator;

    public ValidatorTests()
    {
        ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("es");
        _validator = new PlaceOrderValidator(new CustomerInputValidator(), new CardValidator(new FakeTimeProvider(TestData.Now)));
    }

    private static PlaceOrderCommand Valid() => new(
        new CustomerInput("Ana", "López", "ana@correo.mx", "+52 55 1234 5678"),
        Guid.NewGuid(), 1, TestData.Card(), "key-1");

    [Fact]
    public void Valid_command_passes() => Assert.True(_validator.Validate(Valid()).IsValid);

    [Fact]
    public void Reports_every_invalid_field_in_spanish()
    {
        var command = Valid() with
        {
            Customer = new CustomerInput("", "López", "no-es-correo", "abc"),
            Quantity = 11,
            Card = new CardDetails("", "4111111111111112", 1, 2020, "12"),
        };

        var errors = _validator.Validate(command).Errors.ToDictionary(e => e.PropertyName, e => e.ErrorMessage);

        Assert.Equal("'Nombre' no debería estar vacío.", errors["Customer.FirstName"]);
        Assert.Contains("correo electrónico válida", errors["Customer.Email"]);
        Assert.Equal("El teléfono no tiene un formato válido.", errors["Customer.Phone"]);
        Assert.Contains("entre 1 y 10", errors["Quantity"]);
        Assert.Equal("El número de tarjeta no es válido.", errors["Card.Number"]);
        Assert.Equal("La tarjeta está vencida.", errors["Card.Expiry"]);
        Assert.Equal("El CVV debe tener 3 o 4 dígitos.", errors["Card.Cvv"]);
        Assert.True(errors.ContainsKey("Card.HolderName"));
    }
}
