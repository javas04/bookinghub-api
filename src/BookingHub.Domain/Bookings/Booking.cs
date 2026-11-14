using System.Linq.Expressions;
using BookingHub.Domain.Common;
using BookingHub.Domain.Spaces;
using BookingHub.Domain.Users;

namespace BookingHub.Domain.Bookings;

public enum BookingStatus
{
    Confirmed,
    Cancelled,
}

public sealed class Booking
{
    public static readonly TimeSpan MinDuration = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan MaxDuration = TimeSpan.FromHours(8);
    public const int MaxNotesLength = 500;

    private Booking() { } // Required by EF Core.

    public Guid Id { get; private set; }
    public Guid SpaceId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime StartUtc { get; private set; }
    public DateTime EndUtc { get; private set; }
    public int Attendees { get; private set; }
    public string? Notes { get; private set; }
    public BookingStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }

    public Space Space { get; private set; } = null!;
    public User User { get; private set; } = null!;

    /// <summary>
    /// Creates a confirmed booking after checking every rule that only depends on the
    /// booking itself. Overlap with other bookings is checked by the caller, which has
    /// access to the rest of the space's schedule.
    /// </summary>
    public static Booking Create(
        Space space,
        Guid userId,
        DateTime startUtc,
        DateTime endUtc,
        int attendees,
        string? notes,
        DateTime nowUtc)
    {
        if (!space.IsActive)
        {
            throw new DomainException("This space is not available for booking.");
        }

        if (endUtc <= startUtc)
        {
            throw new DomainException("The booking must end after it starts.");
        }

        if (startUtc <= nowUtc)
        {
            throw new DomainException("Bookings must start in the future.");
        }

        var duration = endUtc - startUtc;
        if (duration < MinDuration || duration > MaxDuration)
        {
            throw new DomainException(
                $"A booking must last between {MinDuration.TotalMinutes:0} minutes and {MaxDuration.TotalHours:0} hours.");
        }

        if (attendees < 1 || attendees > space.Capacity)
        {
            throw new DomainException($"Attendees must be between 1 and the space capacity ({space.Capacity}).");
        }

        return new Booking
        {
            Id = Guid.CreateVersion7(),
            SpaceId = space.Id,
            UserId = userId,
            StartUtc = startUtc,
            EndUtc = endUtc,
            Attendees = attendees,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            Status = BookingStatus.Confirmed,
            CreatedAtUtc = nowUtc,
        };
    }

    /// <summary>
    /// Confirmed bookings of <paramref name="spaceId"/> that overlap the given interval.
    /// Intervals are half-open, so a booking ending at 10:00 does not clash with one starting at 10:00.
    /// Exposed as an expression so the same rule runs in SQL (EF Core) and in unit tests.
    /// </summary>
    public static Expression<Func<Booking, bool>> ConflictsWith(Guid spaceId, DateTime startUtc, DateTime endUtc) =>
        b => b.SpaceId == spaceId
             && b.Status == BookingStatus.Confirmed
             && b.StartUtc < endUtc
             && startUtc < b.EndUtc;

    public void Cancel(DateTime nowUtc)
    {
        if (Status == BookingStatus.Cancelled)
        {
            throw new DomainException("The booking is already cancelled.");
        }

        if (StartUtc <= nowUtc)
        {
            throw new DomainException("A booking that has already started cannot be cancelled.");
        }

        Status = BookingStatus.Cancelled;
        CancelledAtUtc = nowUtc;
    }
}
