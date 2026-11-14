using BookingHub.Application.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace BookingHub.Infrastructure.Security;

/// <summary>
/// PBKDF2 hashing from ASP.NET Core Identity, without pulling in the rest of Identity.
/// </summary>
internal sealed class PasswordHasher : IPasswordHasher
{
    private static readonly object HashContext = new();
    private readonly PasswordHasher<object> _inner = new();

    public string Hash(string password) => _inner.HashPassword(HashContext, password);

    public bool Verify(string passwordHash, string password) =>
        _inner.VerifyHashedPassword(HashContext, passwordHash, password) != PasswordVerificationResult.Failed;
}
