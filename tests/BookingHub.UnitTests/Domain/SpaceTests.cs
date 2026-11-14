using BookingHub.Domain.Common;
using BookingHub.Domain.Spaces;

namespace BookingHub.UnitTests.Domain;

public sealed class SpaceTests
{
    private static readonly DateTime Now = new(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_trims_text_and_starts_active()
    {
        var space = Space.Create("  Atlas  ", "   ", SpaceType.MeetingRoom, 8, Now);

        Assert.Equal("Atlas", space.Name);
        Assert.Null(space.Description);
        Assert.True(space.IsActive);
    }

    [Theory]
    [InlineData("", 4)]
    [InlineData("Atlas", 0)]
    public void Create_rejects_invalid_data(string name, int capacity) =>
        Assert.Throws<DomainException>(() => Space.Create(name, null, SpaceType.Desk, capacity, Now));

    [Fact]
    public void Deactivate_makes_space_inactive()
    {
        var space = Space.Create("Desk", null, SpaceType.Desk, 1, Now);

        space.Deactivate();

        Assert.False(space.IsActive);
    }
}
