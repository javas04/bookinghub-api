using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BookingHub.Application.Auth;
using BookingHub.Application.Bookings;
using BookingHub.Application.Spaces;
using BookingHub.Domain.Spaces;

namespace BookingHub.IntegrationTests.Infrastructure;

public abstract class ApiTestBase(BookingHubApiFactory factory) : IClassFixture<BookingHubApiFactory>
{
    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>A whole hour, a few days ahead, so tests never collide with "now".</summary>
    protected static DateTime FutureSlot(int daysAhead = 3, int hour = 10) =>
        DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(daysAhead).AddHours(hour), DateTimeKind.Utc);

    protected HttpClient CreateClient(string? token = null)
    {
        var client = factory.CreateClient();
        if (token is not null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return client;
    }

    protected async Task<(HttpClient Client, UserDto User)> CreateMemberAsync()
    {
        var request = new RegisterRequest($"user-{Guid.NewGuid():N}@example.com", "Test User", "Secret123");
        var response = await CreateClient().PostAsJsonAsync("/api/auth/register", request, Json);
        var auth = await ReadAsync<AuthResponse>(response, HttpStatusCode.Created);
        return (CreateClient(auth.AccessToken), auth.User);
    }

    protected async Task<HttpClient> CreateAdminAsync()
    {
        var request = new LoginRequest(BookingHubApiFactory.AdminEmail, BookingHubApiFactory.AdminPassword);
        var response = await CreateClient().PostAsJsonAsync("/api/auth/login", request, Json);
        var auth = await ReadAsync<AuthResponse>(response, HttpStatusCode.OK);
        return CreateClient(auth.AccessToken);
    }

    /// <summary>Each test books its own space so tests sharing the database don't interfere.</summary>
    protected async Task<SpaceDto> CreateSpaceAsync(int capacity = 6, SpaceType type = SpaceType.MeetingRoom)
    {
        var admin = await CreateAdminAsync();
        var request = new SpaceRequest($"Room {Guid.NewGuid():N}"[..20], "Created by a test", type, capacity);
        var response = await admin.PostAsJsonAsync("/api/spaces", request, Json);
        return await ReadAsync<SpaceDto>(response, HttpStatusCode.Created);
    }

    protected static Task<HttpResponseMessage> BookAsync(
        HttpClient client, Guid spaceId, DateTime start, DateTime end, int attendees = 1) =>
        client.PostAsJsonAsync("/api/bookings", new CreateBookingRequest(spaceId, start, end, attendees, null), Json);

    /// <summary>Asserts the status code (showing the body on failure) and deserializes the body.</summary>
    protected static async Task<T> ReadAsync<T>(HttpResponseMessage response, HttpStatusCode expected)
    {
        await AssertStatusAsync(response, expected);
        var body = await response.Content.ReadFromJsonAsync<T>(Json);
        Assert.NotNull(body);
        return body;
    }

    protected static async Task AssertStatusAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        if (response.StatusCode != expected)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.Fail($"Expected {(int)expected} {expected} but got {(int)response.StatusCode} {response.StatusCode}: {body}");
        }
    }
}
