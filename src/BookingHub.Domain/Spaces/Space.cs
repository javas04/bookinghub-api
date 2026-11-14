using BookingHub.Domain.Common;

namespace BookingHub.Domain.Spaces;

public enum SpaceType
{
    MeetingRoom,
    Desk,
    PhoneBooth,
}

/// <summary>
/// A bookable place: a meeting room, a hot desk or a phone booth.
/// </summary>
public sealed class Space
{
    public const int MaxNameLength = 100;
    public const int MaxDescriptionLength = 500;

    private Space() { } // Required by EF Core.

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public SpaceType Type { get; private set; }
    public int Capacity { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static Space Create(string name, string? description, SpaceType type, int capacity, DateTime nowUtc)
    {
        var space = new Space
        {
            Id = Guid.CreateVersion7(),
            IsActive = true,
            CreatedAtUtc = nowUtc,
        };
        space.Update(name, description, type, capacity);
        return space;
    }

    public void Update(string name, string? description, SpaceType type, int capacity)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Space name is required.");
        }

        if (capacity < 1)
        {
            throw new DomainException("Capacity must be at least 1.");
        }

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Type = type;
        Capacity = capacity;
    }

    /// <summary>
    /// Spaces are never hard-deleted so that booking history stays consistent.
    /// </summary>
    public void Deactivate() => IsActive = false;
}
