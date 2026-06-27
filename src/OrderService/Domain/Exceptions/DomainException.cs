namespace OrderService.Domain.Exceptions;

/// <summary>
/// SRP: Represents domain-level rule violations only.
/// Thrown when business invariants are broken (e.g., invalid quantity or negative price).
/// </summary>
public sealed class DomainException : Exception
{
    /// <summary>Initializes a domain exception with the violated rule message.</summary>
    public DomainException(string message) : base(message) { }
}
