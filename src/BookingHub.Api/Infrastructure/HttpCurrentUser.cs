using System.Security.Claims;
using BookingHub.Application.Abstractions;
using BookingHub.Domain.Users;
using Microsoft.IdentityModel.JsonWebTokens;

namespace BookingHub.Api.Infrastructure;

internal sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal Principal =>
        accessor.HttpContext?.User ?? throw new InvalidOperationException("There is no active HTTP request.");

    public Guid UserId =>
        Guid.TryParse(Principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id)
            ? id
            : throw new InvalidOperationException("The request is not authenticated.");

    public bool IsAdmin => Principal.IsInRole(nameof(UserRole.Admin));
}
