namespace BookingHub.Application.Common;

/// <summary>Maps to 404 Not Found.</summary>
public sealed class NotFoundException(string resource, object key)
    : Exception($"{resource} '{key}' was not found.");

/// <summary>Maps to 409 Conflict.</summary>
public sealed class ConflictException(string message) : Exception(message);

/// <summary>Maps to 401 Unauthorized.</summary>
public sealed class InvalidCredentialsException() : Exception("Invalid email or password.");
