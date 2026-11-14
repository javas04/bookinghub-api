using System.Net;
using System.Net.Http.Json;
using BookingHub.Application.Common;
using BookingHub.Application.Spaces;
using BookingHub.Domain.Spaces;
using BookingHub.IntegrationTests.Infrastructure;

namespace BookingHub.IntegrationTests;

public sealed class SpaceEndpointsTests(BookingHubApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task List_is_public_and_includes_seeded_spaces()
    {
        var page = await ReadAsync<PagedResult<SpaceDto>>(
            await CreateClient().GetAsync("/api/spaces?pageSize=100"), HttpStatusCode.OK);

        Assert.Contains(page.Items, s => s.Name == "Atlas");
        Assert.True(page.TotalCount >= 5);
    }

    [Fact]
    public async Task List_filters_by_type_capacity_and_search()
    {
        var page = await ReadAsync<PagedResult<SpaceDto>>(
            await CreateClient().GetAsync("/api/spaces?type=MeetingRoom&minCapacity=10&search=atl"), HttpStatusCode.OK);

        var space = Assert.Single(page.Items);
        Assert.Equal("Atlas", space.Name);
    }

    [Fact]
    public async Task List_clamps_page_size()
    {
        var page = await ReadAsync<PagedResult<SpaceDto>>(
            await CreateClient().GetAsync("/api/spaces?page=0&pageSize=5000"), HttpStatusCode.OK);

        Assert.Equal(1, page.Page);
        Assert.Equal(PagingExtensions.MaxPageSize, page.PageSize);
    }

    [Fact]
    public async Task Create_as_member_returns_403()
    {
        var (member, _) = await CreateMemberAsync();

        var response = await member.PostAsJsonAsync(
            "/api/spaces", new SpaceRequest("Nope", null, SpaceType.Desk, 1), Json);

        await AssertStatusAsync(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_anonymously_returns_401()
    {
        var response = await CreateClient().PostAsJsonAsync(
            "/api/spaces", new SpaceRequest("Nope", null, SpaceType.Desk, 1), Json);

        await AssertStatusAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Admin_can_create_update_and_fetch_a_space()
    {
        var created = await CreateSpaceAsync(capacity: 3);
        var admin = await CreateAdminAsync();

        var update = new SpaceRequest("Renamed room", "Now with a TV", SpaceType.MeetingRoom, 5);
        await AssertStatusAsync(await admin.PutAsJsonAsync($"/api/spaces/{created.Id}", update, Json), HttpStatusCode.OK);

        var fetched = await ReadAsync<SpaceDto>(await CreateClient().GetAsync($"/api/spaces/{created.Id}"), HttpStatusCode.OK);
        Assert.Equal("Renamed room", fetched.Name);
        Assert.Equal(5, fetched.Capacity);
    }

    [Fact]
    public async Task Create_with_invalid_data_returns_400()
    {
        var admin = await CreateAdminAsync();

        var response = await admin.PostAsJsonAsync("/api/spaces", new SpaceRequest("", null, SpaceType.Desk, 0), Json);

        await AssertStatusAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_unknown_space_returns_404() =>
        await AssertStatusAsync(await CreateClient().GetAsync($"/api/spaces/{Guid.NewGuid()}"), HttpStatusCode.NotFound);

    [Fact]
    public async Task Deactivated_space_is_hidden_and_cannot_be_booked()
    {
        var space = await CreateSpaceAsync();
        var admin = await CreateAdminAsync();
        var (member, _) = await CreateMemberAsync();

        await AssertStatusAsync(await admin.DeleteAsync($"/api/spaces/{space.Id}"), HttpStatusCode.NoContent);

        var list = await ReadAsync<PagedResult<SpaceDto>>(
            await CreateClient().GetAsync("/api/spaces?pageSize=100"), HttpStatusCode.OK);
        Assert.DoesNotContain(list.Items, s => s.Id == space.Id);

        var start = FutureSlot();
        await AssertStatusAsync(await BookAsync(member, space.Id, start, start.AddHours(1)), HttpStatusCode.UnprocessableEntity);
    }
}
