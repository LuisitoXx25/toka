namespace Toka.Application.Common;

public enum ErrorType { Validation, NotFound, Conflict, BusinessRule }

/// <summary>Expected business error. <see cref="Code"/> is a stable identifier for clients; <see cref="Message"/> is shown to the user.</summary>
public sealed record Error(string Code, string Message, ErrorType Type, IReadOnlyDictionary<string, string[]>? Details = null)
{
    public static Error NotFound(string entityDisplayName, Guid id) =>
        new("not_found", $"No se encontró {entityDisplayName} con id '{id}'.", ErrorType.NotFound);
}

/// <summary>Outcome of a use case: either a value or an <see cref="Common.Error"/>. Exceptions are kept for unexpected faults.</summary>
public sealed class Result<T>
{
    private readonly T? _value;

    public Error? Error { get; }
    public bool IsSuccess => Error is null;
    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("Result has no value.");

    private Result(T? value, Error? error) { _value = value; Error = error; }

    public static implicit operator Result<T>(T value) => new(value, null);
    public static implicit operator Result<T>(Error error) => new(default, error);
}
