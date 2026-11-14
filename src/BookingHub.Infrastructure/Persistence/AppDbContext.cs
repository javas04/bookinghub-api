using BookingHub.Application.Abstractions;
using BookingHub.Domain.Bookings;
using BookingHub.Domain.Spaces;
using BookingHub.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BookingHub.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Space> Spaces => Set<Space>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Every DateTime is stored and read back as UTC.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<UtcDateTimeConverter>();

        // Enums are stored as readable text instead of magic numbers.
        configurationBuilder.Properties<UserRole>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<SpaceType>().HaveConversion<string>().HaveMaxLength(20);
        configurationBuilder.Properties<BookingStatus>().HaveConversion<string>().HaveMaxLength(20);
    }

    private sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
        v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
}
