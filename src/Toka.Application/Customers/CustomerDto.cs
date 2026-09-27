using Toka.Domain.Customers;

namespace Toka.Application.Customers;

public sealed record CustomerInput(string FirstName, string LastName, string Email, string? Phone);

public sealed record CustomerDto(Guid Id, string FirstName, string LastName, string Email, string? Phone, DateTimeOffset CreatedAt)
{
    public static CustomerDto From(Customer c) => new(c.Id, c.FirstName, c.LastName, c.Email, c.Phone, c.CreatedAt);
}
