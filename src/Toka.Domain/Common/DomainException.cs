namespace Toka.Domain.Common;

/// <summary>Raised when an operation would violate a domain invariant.</summary>
public class DomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
