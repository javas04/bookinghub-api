using BookingHub.Api.Infrastructure;
using BookingHub.Application.Abstractions;
using BookingHub.Application.Auth;

namespace BookingHub.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", async (RegisterRequest request, AuthService auth, CancellationToken ct) =>
                TypedResults.Created("/api/auth/me", await auth.RegisterAsync(request, ct)))
            .WithValidation<RegisterRequest>()
            .RequireRateLimiting(Policies.AuthRateLimit)
            .WithSummary("Create a member account and return an access token")
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/login", async (LoginRequest request, AuthService auth, CancellationToken ct) =>
                TypedResults.Ok(await auth.LoginAsync(request, ct)))
            .WithValidation<LoginRequest>()
            .RequireRateLimiting(Policies.AuthRateLimit)
            .WithSummary("Exchange email and password for an access token")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/me", async (ICurrentUser currentUser, AuthService auth, CancellationToken ct) =>
                TypedResults.Ok(await auth.GetUserAsync(currentUser.UserId, ct)))
            .RequireAuthorization()
            .WithSummary("Profile of the authenticated user");

        return app;
    }
}
