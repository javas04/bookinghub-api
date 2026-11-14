using BookingHub.Application.Abstractions;
using BookingHub.Application.Common;
using BookingHub.Domain.Bookings;
using Microsoft.EntityFrameworkCore;

namespace BookingHub.Application.Bookings;

public sealed class BookingService(IAppDbContext db, ICurrentUser currentUser, TimeProvider timeProvider)
{
    private DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    public async Task<BookingDto> CreateAsync(CreateBookingRequest request, CancellationToken cancellationToken)
    {
        var space = await db.Spaces.SingleOrDefaultAsync(s => s.Id == request.SpaceId, cancellationToken)
                    ?? throw new NotFoundException("Space", request.SpaceId);

        var start = request.StartUtc.ToUtc();
        var end = request.EndUtc.ToUtc();

        // Business rules that only depend on the booking itself (raises DomainException -> 422).
        var booking = Booking.Create(space, currentUser.UserId, start, end, request.Attendees, request.Notes, UtcNow);

        // Rule that depends on the rest of the schedule (-> 409).
        if (await db.Bookings.AnyAsync(Booking.ConflictsWith(space.Id, start, end), cancellationToken))
        {
            throw new ConflictException("The space is already booked for part of the requested time.");
        }

        db.Bookings.Add(booking);
        await db.SaveChangesAsync(cancellationToken);

        return await GetAsync(booking.Id, cancellationToken);
    }

    public Task<PagedResult<BookingDto>> ListMineAsync(MyBookingsQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var bookings = db.Bookings.AsNoTracking().Where(b => b.UserId == userId);

        if (query.UpcomingOnly ?? true)
        {
            var now = UtcNow;
            bookings = bookings.Where(b => b.Status == BookingStatus.Confirmed && b.EndUtc > now);
        }

        return bookings
            .OrderBy(b => b.StartUtc)
            .Select(BookingDto.Projection)
            .ToPagedResultAsync(query.Page, query.PageSize, cancellationToken);
    }

    /// <summary>Admin view of every booking.</summary>
    public Task<PagedResult<BookingDto>> ListAsync(BookingQuery query, CancellationToken cancellationToken)
    {
        var bookings = db.Bookings.AsNoTracking();

        if (query.SpaceId is { } spaceId)
        {
            bookings = bookings.Where(b => b.SpaceId == spaceId);
        }

        if (query.UserId is { } userId)
        {
            bookings = bookings.Where(b => b.UserId == userId);
        }

        if (query.Status is { } status)
        {
            bookings = bookings.Where(b => b.Status == status);
        }

        if (query.FromUtc is { } from)
        {
            var fromUtc = from.ToUtc();
            bookings = bookings.Where(b => b.EndUtc > fromUtc);
        }

        if (query.ToUtc is { } to)
        {
            var toUtc = to.ToUtc();
            bookings = bookings.Where(b => b.StartUtc < toUtc);
        }

        return bookings
            .OrderBy(b => b.StartUtc)
            .Select(BookingDto.Projection)
            .ToPagedResultAsync(query.Page, query.PageSize, cancellationToken);
    }

    public async Task<BookingDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        await VisibleBookings()
            .AsNoTracking()
            .Where(b => b.Id == id)
            .Select(BookingDto.Projection)
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException("Booking", id);

    public async Task<BookingDto> CancelAsync(Guid id, CancellationToken cancellationToken)
    {
        var booking = await VisibleBookings().SingleOrDefaultAsync(b => b.Id == id, cancellationToken)
                      ?? throw new NotFoundException("Booking", id);

        booking.Cancel(UtcNow);
        await db.SaveChangesAsync(cancellationToken);

        return await GetAsync(id, cancellationToken);
    }

    /// <summary>
    /// Members only see their own bookings; someone else's booking answers 404 rather than 403
    /// so its existence is not revealed.
    /// </summary>
    private IQueryable<Booking> VisibleBookings()
    {
        if (currentUser.IsAdmin)
        {
            return db.Bookings;
        }

        var userId = currentUser.UserId;
        return db.Bookings.Where(b => b.UserId == userId);
    }
}
