using BookingHub.Domain.Bookings;
using BookingHub.Domain.Spaces;
using BookingHub.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingHub.Infrastructure.Persistence;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.FullName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.PasswordHash).IsRequired();
    }
}

internal sealed class SpaceConfiguration : IEntityTypeConfiguration<Space>
{
    public void Configure(EntityTypeBuilder<Space> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).HasMaxLength(Space.MaxNameLength).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(Space.MaxDescriptionLength);
        builder.HasIndex(s => s.Name);
    }
}

internal sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Notes).HasMaxLength(Booking.MaxNotesLength);

        builder.HasOne(b => b.Space)
            .WithMany()
            .HasForeignKey(b => b.SpaceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.User)
            .WithMany()
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Serves the overlap check and the availability query.
        builder.HasIndex(b => new { b.SpaceId, b.StartUtc, b.EndUtc });
        builder.HasIndex(b => b.UserId);
    }
}
