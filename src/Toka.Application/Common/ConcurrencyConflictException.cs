namespace Toka.Application.Common;

/// <summary>Thrown by <see cref="Abstractions.IUnitOfWork"/> when another request modified the same rows first.</summary>
public sealed class ConcurrencyConflictException(string message, Exception? inner = null) : Exception(message, inner);
