using BookingHub.Domain.Bookings;
using BookingHub.Domain.Spaces;
using BookingHub.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace BookingHub.Application.Abstractions;

/// <summary>
/// The persistence port used by the application services. DbSet already behaves as a
/// repository + unit of work, so no extra repository layer is added on top of it.
/// </summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Space> Spaces { get; }
    DbSet<Booking> Bookings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
