using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace BookingHub.IntegrationTests.Infrastructure;

/// <summary>
/// Boots the real API in memory against a private in-memory SQLite database.
/// Migrations and seeding run exactly as in production.
/// </summary>
public sealed class BookingHubApiFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@tests.local";
    public const string AdminPassword = "Admin123!";

    private readonly string _connectionString =
        $"Data Source=bookinghub-tests-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";

    // An in-memory SQLite database lives as long as at least one connection to it is open.
    private readonly SqliteConnection _keepAlive;

    public BookingHubApiFactory()
    {
        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", _connectionString);
        builder.UseSetting("Jwt:SigningKey", "integration-tests-signing-key-0123456789abcdef");
        builder.UseSetting("Database:Seed:AdminEmail", AdminEmail);
        builder.UseSetting("Database:Seed:AdminPassword", AdminPassword);
        builder.UseSetting("Database:Seed:SampleSpaces", "true");
        builder.UseSetting("RateLimiting:AuthPermitLimit", "10000");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _keepAlive.Dispose();
        }
    }
}
