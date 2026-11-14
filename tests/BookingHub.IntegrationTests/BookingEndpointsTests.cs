using System.Net;
using BookingHub.Application.Bookings;
using BookingHub.Application.Common;
using BookingHub.Application.Spaces;
using BookingHub.Domain.Bookings;
using BookingHub.IntegrationTests.Infrastructure;

namespace BookingHub.IntegrationTests;

public sealed class BookingEndpointsTests(BookingHubApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Create_returns_201_and_the_booking_shows_up_in_my_bookings()
    {
        var space = await CreateSpaceAsync();
        var (member, user) = await CreateMemberAsync();
        var start = FutureSlot();

        var booking = await ReadAsync<BookingDto>(
            await BookAsync(member, space.Id, start, start.AddHours(1), attendees: 4), HttpStatusCode.Created);

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.Equal(user.Id, booking.UserId);
        Assert.Equal(space.Name, booking.SpaceName);
        Assert.Equal(start, booking.StartUtc);

        var mine = await ReadAsync<PagedResult<BookingDto>>(await member.GetAsync("/api/bookings/me"), HttpStatusCode.OK);
        Assert.Contains(mine.Items, b => b.Id == booking.Id);
    }

    [Fact]
    public async Task Overlapping_booking_returns_409()
    {
        var space = await CreateSpaceAsync();
        var (alice, _) = await CreateMemberAsync();
        var (bob, _) = await CreateMemberAsync();
        var start = FutureSlot();

        await AssertStatusAsync(await BookAsync(alice, space.Id, start, start.AddHours(2)), HttpStatusCode.Created);

        var clash = await BookAsync(bob, space.Id, start.AddMinutes(30), start.AddHours(1));
        await AssertStatusAsync(clash, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Back_to_back_bookings_are_allowed()
    {
        var space = await CreateSpaceAsync();
        var (member, _) = await CreateMemberAsync();
        var start = FutureSlot();

        await AssertStatusAsync(await BookAsync(member, space.Id, start, start.AddHours(1)), HttpStatusCode.Created);
        await AssertStatusAsync(await BookAsync(member, space.Id, start.AddHours(1), start.AddHours(2)), HttpStatusCode.Created);
    }

    [Fact]
    public async Task Booking_in_the_past_returns_422()
    {
        var space = await CreateSpaceAsync();
        var (member, _) = await CreateMemberAsync();
        var start = DateTime.UtcNow.AddDays(-1);

        await AssertStatusAsync(await BookAsync(member, space.Id, start, start.AddHours(1)), HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Booking_over_capacity_returns_422()
    {
        var space = await CreateSpaceAsync(capacity: 2);
        var (member, _) = await CreateMemberAsync();
        var start = FutureSlot();

        await AssertStatusAsync(
            await BookAsync(member, space.Id, start, start.AddHours(1), attendees: 3),
            HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Booking_unknown_space_returns_404()
    {
        var (member, _) = await CreateMemberAsync();
        var start = FutureSlot();

        await AssertStatusAsync(await BookAsync(member, Guid.NewGuid(), start, start.AddHours(1)), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Members_cannot_see_other_members_bookings_but_admins_can()
    {
        var space = await CreateSpaceAsync();
        var (owner, _) = await CreateMemberAsync();
        var (stranger, _) = await CreateMemberAsync();
        var admin = await CreateAdminAsync();
        var start = FutureSlot();
        var booking = await ReadAsync<BookingDto>(await BookAsync(owner, space.Id, start, start.AddHours(1)), HttpStatusCode.Created);

        await AssertStatusAsync(await stranger.GetAsync($"/api/bookings/{booking.Id}"), HttpStatusCode.NotFound);
        await AssertStatusAsync(await stranger.PostAsync($"/api/bookings/{booking.Id}/cancel", null), HttpStatusCode.NotFound);
        await AssertStatusAsync(await admin.GetAsync($"/api/bookings/{booking.Id}"), HttpStatusCode.OK);
    }

    [Fact]
    public async Task Cancelling_frees_the_slot()
    {
        var space = await CreateSpaceAsync();
        var (alice, _) = await CreateMemberAsync();
        var (bob, _) = await CreateMemberAsync();
        var start = FutureSlot();
        var booking = await ReadAsync<BookingDto>(await BookAsync(alice, space.Id, start, start.AddHours(1)), HttpStatusCode.Created);

        var cancelled = await ReadAsync<BookingDto>(
            await alice.PostAsync($"/api/bookings/{booking.Id}/cancel", null), HttpStatusCode.OK);
        Assert.Equal(BookingStatus.Cancelled, cancelled.Status);

        await AssertStatusAsync(await BookAsync(bob, space.Id, start, start.AddHours(1)), HttpStatusCode.Created);
        await AssertStatusAsync(
            await alice.PostAsync($"/api/bookings/{booking.Id}/cancel", null), HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Availability_lists_busy_slots_of_the_day()
    {
        var space = await CreateSpaceAsync();
        var (member, _) = await CreateMemberAsync();
        var morning = FutureSlot(hour: 9);
        var afternoon = FutureSlot(hour: 15);
        await AssertStatusAsync(await BookAsync(member, space.Id, afternoon, afternoon.AddHours(1)), HttpStatusCode.Created);
        await AssertStatusAsync(await BookAsync(member, space.Id, morning, morning.AddMinutes(30)), HttpStatusCode.Created);

        var date = DateOnly.FromDateTime(morning).ToString("yyyy-MM-dd");
        var availability = await ReadAsync<SpaceAvailabilityDto>(
            await CreateClient().GetAsync($"/api/spaces/{space.Id}/availability?date={date}"), HttpStatusCode.OK);

        Assert.Equal([morning, afternoon], availability.BusySlots.Select(s => s.StartUtc));
    }

    [Fact]
    public async Task Listing_all_bookings_requires_admin()
    {
        var space = await CreateSpaceAsync();
        var (member, _) = await CreateMemberAsync();
        var admin = await CreateAdminAsync();
        var start = FutureSlot();
        await AssertStatusAsync(await BookAsync(member, space.Id, start, start.AddHours(1)), HttpStatusCode.Created);

        await AssertStatusAsync(await member.GetAsync("/api/bookings"), HttpStatusCode.Forbidden);

        var all = await ReadAsync<PagedResult<BookingDto>>(
            await admin.GetAsync($"/api/bookings?spaceId={space.Id}"), HttpStatusCode.OK);
        Assert.Single(all.Items);
    }
}
