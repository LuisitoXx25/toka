using FluentValidation;
using Toka.Application.Abstractions;
using Toka.Application.Common;
using Toka.Domain.Customers;

namespace Toka.Application.Customers;

public sealed class CustomerService(
    ICustomerRepository customers,
    IUnitOfWork unitOfWork,
    IAuditLog audit,
    IValidator<CustomerInput> validator,
    TimeProvider clock)
{
    /// <summary>Registers a new customer. Fails with Conflict if the email is already registered.</summary>
    public async Task<Result<CustomerDto>> RegisterAsync(CustomerInput input, CancellationToken ct)
    {
        if (await validator.ValidateToErrorAsync(input, ct) is { } error) return error;

        if (await customers.GetByEmailAsync(Customer.NormalizeEmail(input.Email), ct) is not null)
            return new Error("customer_exists", "Ya existe un cliente registrado con este correo electrónico.", ErrorType.Conflict);

        var customer = AddNew(input);
        await unitOfWork.SaveChangesAsync(ct);
        return CustomerDto.From(customer);
    }

    public async Task<Result<CustomerDto>> GetAsync(Guid id, CancellationToken ct) =>
        await customers.GetByIdAsync(id, ct) is { } c ? CustomerDto.From(c) : Error.NotFound("el cliente", id);

    /// <summary>
    /// Returns the customer with that email or registers a new one. Used by checkout so a returning buyer is not
    /// rejected. Existing customer data is not overwritten. The caller must save changes.
    /// </summary>
    internal async Task<Customer> FindOrAddAsync(CustomerInput input, CancellationToken ct) =>
        await customers.GetByEmailAsync(Customer.NormalizeEmail(input.Email), ct) ?? AddNew(input);

    private Customer AddNew(CustomerInput input)
    {
        var customer = Customer.Register(input.FirstName, input.LastName, input.Email, input.Phone, clock.GetUtcNow());
        customers.Add(customer);
        audit.Record(AuditEvents.CustomerRegistered, "Cliente registrado.", nameof(Customer), customer.Id);
        return customer;
    }
}
