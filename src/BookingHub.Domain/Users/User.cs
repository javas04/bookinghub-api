using BookingHub.Domain.Common;

namespace BookingHub.Domain.Users;

public enum UserRole
{
    Member,
    Admin,
}

public sealed class User
{
    private User() { } // Required by EF Core.

    public Guid Id { get; private set; }
    public string Email { get; private set; } = null!;
    public string FullName { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public UserRole Role { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static User Create(string email, string fullName, string passwordHash, UserRole role, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new DomainException("Full name is required.");
        }

        return new User
        {
            Id = Guid.CreateVersion7(),
            Email = NormalizeEmail(email),
            FullName = fullName.Trim(),
            PasswordHash = passwordHash,
            Role = role,
            CreatedAtUtc = nowUtc,
        };
    }

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
