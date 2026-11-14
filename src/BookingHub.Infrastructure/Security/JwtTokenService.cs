using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using BookingHub.Application.Abstractions;
using BookingHub.Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace BookingHub.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public const string RoleClaim = "role";

    [Required]
    public string Issuer { get; set; } = "BookingHub";

    [Required]
    public string Audience { get; set; } = "BookingHub";

    /// <summary>HMAC-SHA256 key. Must come from configuration/secrets, never from source control in production.</summary>
    [Required]
    [MinLength(32, ErrorMessage = "Jwt:SigningKey must be at least 32 characters long.")]
    public string SigningKey { get; set; } = string.Empty;

    [Range(1, 1440)]
    public int ExpirationMinutes { get; set; } = 60;

    public SymmetricSecurityKey GetSecurityKey() => new(Encoding.UTF8.GetBytes(SigningKey));
}

internal sealed class JwtTokenService(IOptions<JwtOptions> options, TimeProvider timeProvider) : ITokenService
{
    private readonly JsonWebTokenHandler _handler = new();

    public AccessToken CreateToken(User user)
    {
        var jwt = options.Value;
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var expires = now.AddMinutes(jwt.ExpirationMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Name, user.FullName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtOptions.RoleClaim, user.Role.ToString()),
            ]),
            SigningCredentials = new SigningCredentials(jwt.GetSecurityKey(), SecurityAlgorithms.HmacSha256),
        };

        return new AccessToken(_handler.CreateToken(descriptor), expires);
    }
}
