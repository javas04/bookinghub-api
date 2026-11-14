using BookingHub.Domain.Bookings;
using BookingHub.Domain.Common;
using BookingHub.Domain.Spaces;

namespace BookingHub.UnitTests.Domain;

public sealed class BookingTests
{
    private static readonly DateTime Now = new(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime TenAm = Now.Date.AddDays(1).AddHours(10);
    private static readonly Guid UserId = Guid.NewGuid();

    private static Space ARoom(int capacity = 6) =>
        Space.Create("Room", null, SpaceType.MeetingRoom, capacity, Now);

    private static Booking ABooking(Space? space = null, DateTime? start = null, TimeSpan? duration = null)
    {
        var from = start ?? TenAm;
        return Booking.Create(space ?? ARoom(), UserId, from, from + (duration ?? TimeSpan.FromHours(1)), 2, null, Now);
    }

    [Fact]
    public void Create_with_valid_data_returns_confirmed_booking()
    {
        var space = ARoom();

        var booking = Booking.Create(space, UserId, TenAm, TenAm.AddHours(1), 3, "  Sprint review  ", Now);

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.Equal(space.Id, booking.SpaceId);
        Assert.Equal(UserId, booking.UserId);
        Assert.Equal("Sprint review", booking.Notes);
        Assert.Equal(Now, booking.CreatedAtUtc);
    }

    [Fact]
    public void Create_rejects_end_before_start() =>
        Assert.Throws<DomainException>(() =>
            Booking.Create(ARoom(), UserId, TenAm, TenAm.AddMinutes(-30), 1, null, Now));

    [Fact]
    public void Create_rejects_bookings_in_the_past() =>
        Assert.Throws<DomainException>(() =>
            Booking.Create(ARoom(), UserId, Now.AddHours(-2), Now.AddHours(-1), 1, null, Now));

    [Theory]
    [InlineData(10)] // shorter than 15 minutes
    [InlineData(9 * 60)] // longer than 8 hours
    public void Create_rejects_durations_outside_limits(int minutes) =>
        Assert.Throws<DomainException>(() =>
            Booking.Create(ARoom(), UserId, TenAm, TenAm.AddMinutes(minutes), 1, null, Now));

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void Create_rejects_attendees_outside_capacity(int attendees) =>
        Assert.Throws<DomainException>(() =>
            Booking.Create(ARoom(capacity: 6), UserId, TenAm, TenAm.AddHours(1), attendees, null, Now));

    [Fact]
    public void Create_rejects_inactive_space()
    {
        var space = ARoom();
        space.Deactivate();

        Assert.Throws<DomainException>(() => ABooking(space));
    }

    [Fact]
    public void Cancel_marks_booking_as_cancelled()
    {
        var booking = ABooking();

        booking.Cancel(Now);

        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.Equal(Now, booking.CancelledAtUtc);
    }

    [Fact]
    public void Cancel_twice_is_rejected()
    {
        var booking = ABooking();
        booking.Cancel(Now);

        Assert.Throws<DomainException>(() => booking.Cancel(Now));
    }

    [Fact]
    public void Cancel_after_start_is_rejected()
    {
        var booking = ABooking();

        Assert.Throws<DomainException>(() => booking.Cancel(TenAm.AddMinutes(5)));
    }

    // Existing booking: 10:00-11:00.
    [Theory]
    [InlineData(9, 0, 10, 30, true)] // overlaps the start
    [InlineData(10, 30, 12, 0, true)] // overlaps the end
    [InlineData(10, 15, 10, 45, true)] // inside
    [InlineData(9, 0, 12, 0, true)] // surrounds
    [InlineData(9, 0, 10, 0, false)] // ends exactly when it starts
    [InlineData(11, 0, 12, 0, false)] // starts exactly when it ends
    public void ConflictsWith_uses_half_open_intervals(
        int startHour, int startMinute, int endHour, int endMinute, bool expected)
    {
        var space = ARoom();
        var existing = ABooking(space);
        var day = TenAm.Date;

        var conflicts = Booking.ConflictsWith(
                space.Id,
                day.AddHours(startHour).AddMinutes(startMinute),
                day.AddHours(endHour).AddMinutes(endMinute))
            .Compile()(existing);

        Assert.Equal(expected, conflicts);
    }

    [Fact]
    public void ConflictsWith_ignores_cancelled_bookings_and_other_spaces()
    {
        var space = ARoom();
        var cancelled = ABooking(space);
        cancelled.Cancel(Now);
        var otherSpace = ABooking(ARoom());

        var conflicts = Booking.ConflictsWith(space.Id, TenAm, TenAm.AddHours(1)).Compile();

        Assert.False(conflicts(cancelled));
        Assert.False(conflicts(otherSpace));
    }
}
