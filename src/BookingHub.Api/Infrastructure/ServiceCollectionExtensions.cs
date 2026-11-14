using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using BookingHub.Application.Abstractions;
using BookingHub.Domain.Users;
using BookingHub.Infrastructure.Security;
using System.Text.RegularExpressions;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace BookingHub.Api.Infrastructure;

public static class Policies
{
    public const string Admin = nameof(Admin);
    public const string AuthRateLimit = "auth";
}

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>Requests per minute and client IP allowed on login/register.</summary>
    public int AuthPermitLimit { get; set; } = 10;
}

public static partial class ServiceCollectionExtensions
{
    [GeneratedRegex("(?<=[a-z0-9])([A-Z])")]
    private static partial Regex PascalCaseBoundary();

    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();

        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        // Validation error keys match the camelCase JSON the client sent ("fullName"),
        // while messages keep a readable name ("'Full Name' must not be empty.").
        ValidatorOptions.Global.PropertyNameResolver = (_, member, _) =>
            member is null ? null : JsonNamingPolicy.CamelCase.ConvertName(member.Name);
        ValidatorOptions.Global.DisplayNameResolver = (_, member, _) =>
            member is null ? null : PascalCaseBoundary().Replace(member.Name, " $1");

        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Configured through options so the key is read from the validated JwtOptions at runtime.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = jwt.GetSecurityKey(),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = JwtRegisteredClaimNames.Name,
                    RoleClaimType = JwtOptions.RoleClaim,
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.Admin, policy => policy.RequireRole(nameof(UserRole.Admin)));

        return services;
    }

    public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services)
    {
        services.AddOptions<RateLimitingOptions>().BindConfiguration(RateLimitingOptions.SectionName);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(Policies.AuthRateLimit, context =>
            {
                var limits = context.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value;
                return RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = limits.AuthPermitLimit,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    });
            });
        });

        return services;
    }
}
