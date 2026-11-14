using System.Net;
using System.Text.Json;
using BookingHub.IntegrationTests.Infrastructure;

namespace BookingHub.IntegrationTests;

public sealed class PlatformEndpointsTests(BookingHubApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Health_check_reports_healthy()
    {
        var response = await CreateClient().GetAsync("/health");

        await AssertStatusAsync(response, HttpStatusCode.OK);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task OpenApi_document_describes_the_api_and_bearer_auth()
    {
        var response = await CreateClient().GetAsync("/openapi/v1.json");
        await AssertStatusAsync(response, HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        Assert.Equal("BookingHub API", root.GetProperty("info").GetProperty("title").GetString());
        Assert.True(root.GetProperty("paths").TryGetProperty("/api/bookings", out _));
        Assert.True(root.GetProperty("components").GetProperty("securitySchemes").TryGetProperty("Bearer", out _));
    }
}
