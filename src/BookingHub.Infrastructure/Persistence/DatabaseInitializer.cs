using BookingHub.Application.Abstractions;
using BookingHub.Domain.Spaces;
using BookingHub.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BookingHub.Infrastructure.Persistence;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>Apply pending EF Core migrations when the app starts.</summary>
    public bool MigrateOnStartup { get; set; } = true;

    public SeedOptions Seed { get; set; } = new();
}

public sealed class SeedOptions
{
    /// <summary>When both are set, an admin account is created if it doesn't exist yet.</summary>
    public string? AdminEmail { get; set; }

    public string? AdminPassword { get; set; }

    /// <summary>Adds a few demo spaces when the database has none.</summary>
    public bool SampleSpaces { get; set; }
}

public static partial class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var db = provider.GetRequiredService<AppDbContext>();
        var options = provider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        var time = provider.GetRequiredService<TimeProvider>();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseInitializer));

        if (options.MigrateOnStartup)
        {
            await db.Database.MigrateAsync(cancellationToken);
        }

        var now = time.GetUtcNow().UtcDateTime;
        var seed = options.Seed;

        if (!string.IsNullOrWhiteSpace(seed.AdminEmail) && !string.IsNullOrWhiteSpace(seed.AdminPassword))
        {
            var email = User.NormalizeEmail(seed.AdminEmail);
            if (!await db.Users.AnyAsync(u => u.Email == email, cancellationToken))
            {
                var hasher = provider.GetRequiredService<IPasswordHasher>();
                db.Users.Add(User.Create(email, "Administrator", hasher.Hash(seed.AdminPassword), UserRole.Admin, now));
                LogAdminSeeded(logger, email);
            }
        }

        if (seed.SampleSpaces && !await db.Spaces.AnyAsync(cancellationToken))
        {
            db.Spaces.AddRange(
                Space.Create("Atlas", "Large meeting room with a projector and video conferencing.", SpaceType.MeetingRoom, 12, now),
                Space.Create("Orion", "Small meeting room with a whiteboard.", SpaceType.MeetingRoom, 4, now),
                Space.Create("Desk A1", "Hot desk next to the window, dual monitor.", SpaceType.Desk, 1, now),
                Space.Create("Desk A2", "Hot desk, standing desk.", SpaceType.Desk, 1, now),
                Space.Create("Booth 1", "Soundproof phone booth for calls.", SpaceType.PhoneBooth, 1, now));
            LogSampleSpacesSeeded(logger);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeded admin account {Email}")]
    private static partial void LogAdminSeeded(ILogger logger, string email);

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeded sample spaces")]
    private static partial void LogSampleSpacesSeeded(ILogger logger);
}
