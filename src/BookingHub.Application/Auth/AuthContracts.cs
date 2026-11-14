using BookingHub.Domain.Users;
using FluentValidation;

namespace BookingHub.Application.Auth;

public sealed record RegisterRequest(string Email, string FullName, string Password);

public sealed record LoginRequest(string Email, string Password);

public sealed record UserDto(Guid Id, string Email, string FullName, UserRole Role);

public sealed record AuthResponse(string AccessToken, DateTime ExpiresAtUtc, UserDto User);

internal sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128)
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.");
    }
}

internal sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}
