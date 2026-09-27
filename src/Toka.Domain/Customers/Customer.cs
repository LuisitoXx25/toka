using Toka.Domain.Common;

namespace Toka.Domain.Customers;

public sealed class Customer : Entity
{
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string? Phone { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Customer() { }

    public static Customer Register(string firstName, string lastName, string email, string? phone, DateTimeOffset now) => new()
    {
        FirstName = Guard.NotBlank(firstName, nameof(FirstName), 100),
        LastName = Guard.NotBlank(lastName, nameof(LastName), 100),
        Email = NormalizeEmail(email),
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
        CreatedAt = now,
    };

    public static string NormalizeEmail(string email) =>
        Guard.NotBlank(email, nameof(Email), 254).ToLowerInvariant();
}
