namespace BookingHub.Domain.Common;

/// <summary>
/// Raised when an operation would break a business rule of the domain model.
/// </summary>
public sealed class DomainException(string message) : Exception(message);
