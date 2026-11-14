using BookingHub.Application.Abstractions;
using BookingHub.Application.Common;
using BookingHub.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace BookingHub.Application.Auth;

public sealed class AuthService(
    IAppDbContext db,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    TimeProvider timeProvider)
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = User.NormalizeEmail(request.Email);
        if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            throw new ConflictException("An account with this email already exists.");
        }

        var user = User.Create(
            email,
            request.FullName,
            passwordHasher.Hash(request.Password),
            UserRole.Member,
            timeProvider.GetUtcNow().UtcDateTime);

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        return CreateResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = User.NormalizeEmail(request.Email);
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email, cancellationToken);

        // Same error for "unknown email" and "wrong password" so accounts can't be enumerated.
        if (user is null || !passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            throw new InvalidCredentialsException();
        }

        return CreateResponse(user);
    }

    public async Task<UserDto> GetUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == userId, cancellationToken)
                   ?? throw new NotFoundException("User", userId);
        return ToDto(user);
    }

    private AuthResponse CreateResponse(User user)
    {
        var token = tokenService.CreateToken(user);
        return new AuthResponse(token.Token, token.ExpiresAtUtc, ToDto(user));
    }

    private static UserDto ToDto(User user) => new(user.Id, user.Email, user.FullName, user.Role);
}
