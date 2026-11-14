using BookingHub.Application.Abstractions;
using BookingHub.Application.Common;
using BookingHub.Domain.Bookings;
using BookingHub.Domain.Spaces;
using Microsoft.EntityFrameworkCore;

namespace BookingHub.Application.Spaces;

public sealed class SpaceService(IAppDbContext db, TimeProvider timeProvider)
{
    public Task<PagedResult<SpaceDto>> ListAsync(SpaceQuery query, CancellationToken cancellationToken)
    {
        var spaces = db.Spaces.AsNoTracking().Where(s => s.IsActive);

        if (query.Type is { } type)
        {
            spaces = spaces.Where(s => s.Type == type);
        }

        if (query.MinCapacity is { } minCapacity)
        {
            spaces = spaces.Where(s => s.Capacity >= minCapacity);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            spaces = spaces.Where(s => EF.Functions.Like(s.Name, $"%{query.Search.Trim()}%"));
        }

        return spaces
            .OrderBy(s => s.Name)
            .Select(SpaceDto.Projection)
            .ToPagedResultAsync(query.Page, query.PageSize, cancellationToken);
    }

    public async Task<SpaceDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Spaces.AsNoTracking()
            .Where(s => s.Id == id)
            .Select(SpaceDto.Projection)
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException("Space", id);

    public async Task<SpaceDto> CreateAsync(SpaceRequest request, CancellationToken cancellationToken)
    {
        var space = Space.Create(
            request.Name,
            request.Description,
            request.Type,
            request.Capacity,
            timeProvider.GetUtcNow().UtcDateTime);

        db.Spaces.Add(space);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(space.Id, cancellationToken);
    }

    public async Task<SpaceDto> UpdateAsync(Guid id, SpaceRequest request, CancellationToken cancellationToken)
    {
        var space = await FindAsync(id, cancellationToken);
        space.Update(request.Name, request.Description, request.Type, request.Capacity);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var space = await FindAsync(id, cancellationToken);
        space.Deactivate();
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Confirmed bookings that touch the given UTC day.</summary>
    public async Task<SpaceAvailabilityDto> GetAvailabilityAsync(
        Guid id,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        if (!await db.Spaces.AnyAsync(s => s.Id == id, cancellationToken))
        {
            throw new NotFoundException("Space", id);
        }

        var dayStart = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dayEnd = dayStart.AddDays(1);

        var busy = await db.Bookings.AsNoTracking()
            .Where(Booking.ConflictsWith(id, dayStart, dayEnd))
            .OrderBy(b => b.StartUtc)
            .Select(b => new TimeSlot(b.StartUtc, b.EndUtc))
            .ToListAsync(cancellationToken);

        return new SpaceAvailabilityDto(id, date, busy);
    }

    private async Task<Space> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Spaces.SingleOrDefaultAsync(s => s.Id == id, cancellationToken)
        ?? throw new NotFoundException("Space", id);
}
