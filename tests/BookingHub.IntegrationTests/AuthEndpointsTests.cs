using System.Net;
using System.Net.Http.Json;
using BookingHub.Application.Auth;
using BookingHub.Domain.Users;
using BookingHub.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace BookingHub.IntegrationTests;

public sealed class AuthEndpointsTests(BookingHubApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Register_returns_a_token_that_authenticates_the_user()
    {
        var email = $"Ana.{Guid.NewGuid():N}@Example.com";
        var response = await CreateClient().PostAsJsonAsync(
            "/api/auth/register", new RegisterRequest(email, "Ana García", "Secret123"), Json);

        var auth = await ReadAsync<AuthResponse>(response, HttpStatusCode.Created);
        Assert.Equal(UserRole.Member, auth.User.Role);

        var me = await ReadAsync<UserDto>(await CreateClient(auth.AccessToken).GetAsync("/api/auth/me"), HttpStatusCode.OK);
        Assert.Equal(email.ToLowerInvariant(), me.Email);
        Assert.Equal("Ana García", me.FullName);
    }

    [Fact]
    public async Task Register_with_existing_email_returns_409()
    {
        var request = new RegisterRequest($"dup-{Guid.NewGuid():N}@example.com", "Dup", "Secret123");
        var client = CreateClient();
        await AssertStatusAsync(await client.PostAsJsonAsync("/api/auth/register", request, Json), HttpStatusCode.Created);

        var second = await client.PostAsJsonAsync("/api/auth/register", request with { Email = request.Email.ToUpperInvariant() }, Json);

        await AssertStatusAsync(second, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_with_invalid_payload_returns_400_with_field_errors()
    {
        var response = await CreateClient().PostAsJsonAsync(
            "/api/auth/register", new RegisterRequest("not-an-email", "", "weak"), Json);

        var problem = await ReadAsync<ValidationProblemDetails>(response, HttpStatusCode.BadRequest);
        Assert.Contains("email", problem.Errors.Keys);
        Assert.Contains("fullName", problem.Errors.Keys);
        Assert.Contains("password", problem.Errors.Keys);
        Assert.Contains(problem.Errors["fullName"], message => message.Contains("'Full Name'"));
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401()
    {
        var response = await CreateClient().PostAsJsonAsync(
            "/api/auth/login", new LoginRequest(BookingHubApiFactory.AdminEmail, "WrongPassword1"), Json);

        await AssertStatusAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_without_token_returns_401() =>
        await AssertStatusAsync(await CreateClient().GetAsync("/api/auth/me"), HttpStatusCode.Unauthorized);

    [Fact]
    public async Task Me_with_tampered_token_returns_401()
    {
        var response = await CreateClient("eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJ4In0.invalid-signature").GetAsync("/api/auth/me");

        await AssertStatusAsync(response, HttpStatusCode.Unauthorized);
    }
}
