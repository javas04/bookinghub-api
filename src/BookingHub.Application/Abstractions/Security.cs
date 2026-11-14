using BookingHub.Domain.Users;

namespace BookingHub.Application.Abstractions;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string passwordHash, string password);
}

public sealed record AccessToken(string Token, DateTime ExpiresAtUtc);

public interface ITokenService
{
    AccessToken CreateToken(User user);
}

/// <summary>
/// The authenticated caller of the current request.
/// </summary>
public interface ICurrentUser
{
    /// <summary>Throws if the request is not authenticated.</summary>
    Guid UserId { get; }

    bool IsAdmin { get; }
}
