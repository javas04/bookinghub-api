using System.Linq.Expressions;
using BookingHub.Domain.Bookings;
using FluentValidation;

namespace BookingHub.Application.Bookings;

public sealed record CreateBookingRequest(
    Guid SpaceId,
    DateTime StartUtc,
    DateTime EndUtc,
    int Attendees,
    string? Notes);

public sealed record BookingDto(
    Guid Id,
    Guid SpaceId,
    string SpaceName,
    Guid UserId,
    DateTime StartUtc,
    DateTime EndUtc,
    int Attendees,
    string? Notes,
    BookingStatus Status,
    DateTime CreatedAtUtc,
    DateTime? CancelledAtUtc)
{
    internal static readonly Expression<Func<Booking, BookingDto>> Projection = b => new BookingDto(
        b.Id,
        b.SpaceId,
        b.Space.Name,
        b.UserId,
        b.StartUtc,
        b.EndUtc,
        b.Attendees,
        b.Notes,
        b.Status,
        b.CreatedAtUtc,
        b.CancelledAtUtc);
}

public sealed class MyBookingsQuery
{
    /// <summary>Only confirmed bookings that have not ended yet. Defaults to true.</summary>
    public bool? UpcomingOnly { get; init; }

    public int? Page { get; init; }
    public int? PageSize { get; init; }
}

public sealed class BookingQuery
{
    public Guid? SpaceId { get; init; }
    public Guid? UserId { get; init; }
    public BookingStatus? Status { get; init; }

    /// <summary>Bookings ending after this instant.</summary>
    public DateTime? FromUtc { get; init; }

    /// <summary>Bookings starting before this instant.</summary>
    public DateTime? ToUtc { get; init; }

    public int? Page { get; init; }
    public int? PageSize { get; init; }
}

internal sealed class CreateBookingRequestValidator : AbstractValidator<CreateBookingRequest>
{
    public CreateBookingRequestValidator()
    {
        RuleFor(x => x.SpaceId).NotEmpty();
        RuleFor(x => x.StartUtc).NotEmpty();
        RuleFor(x => x.EndUtc).NotEmpty().GreaterThan(x => x.StartUtc);
        RuleFor(x => x.Attendees).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Notes).MaximumLength(Booking.MaxNotesLength);
    }
}
