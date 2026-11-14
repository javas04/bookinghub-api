using System.Linq.Expressions;
using BookingHub.Domain.Spaces;
using FluentValidation;

namespace BookingHub.Application.Spaces;

/// <summary>Body for both creating and updating a space.</summary>
public sealed record SpaceRequest(string Name, string? Description, SpaceType Type, int Capacity);

public sealed record SpaceDto(Guid Id, string Name, string? Description, SpaceType Type, int Capacity, bool IsActive)
{
    internal static readonly Expression<Func<Space, SpaceDto>> Projection =
        s => new SpaceDto(s.Id, s.Name, s.Description, s.Type, s.Capacity, s.IsActive);
}

public sealed class SpaceQuery
{
    /// <summary>Filter by space type.</summary>
    public SpaceType? Type { get; init; }

    /// <summary>Only spaces that fit at least this many people.</summary>
    public int? MinCapacity { get; init; }

    /// <summary>Case-insensitive search on the space name.</summary>
    public string? Search { get; init; }

    public int? Page { get; init; }
    public int? PageSize { get; init; }
}

public sealed record TimeSlot(DateTime StartUtc, DateTime EndUtc);

public sealed record SpaceAvailabilityDto(Guid SpaceId, DateOnly Date, IReadOnlyList<TimeSlot> BusySlots);

internal sealed class SpaceRequestValidator : AbstractValidator<SpaceRequest>
{
    public SpaceRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Space.MaxNameLength);
        RuleFor(x => x.Description).MaximumLength(Space.MaxDescriptionLength);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Capacity).InclusiveBetween(1, 500);
    }
}
