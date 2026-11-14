using BookingHub.Api.Infrastructure;
using BookingHub.Application.Spaces;

namespace BookingHub.Api.Endpoints;

public static class SpaceEndpoints
{
    public static IEndpointRouteBuilder MapSpaceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/spaces").WithTags("Spaces");

        group.MapGet("/", async ([AsParameters] SpaceQuery query, SpaceService spaces, CancellationToken ct) =>
                TypedResults.Ok(await spaces.ListAsync(query, ct)))
            .WithSummary("List active spaces (paged, filterable)");

        group.MapGet("/{id:guid}", async (Guid id, SpaceService spaces, CancellationToken ct) =>
                TypedResults.Ok(await spaces.GetAsync(id, ct)))
            .WithSummary("Get a space")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/availability", async (Guid id, DateOnly date, SpaceService spaces, CancellationToken ct) =>
                TypedResults.Ok(await spaces.GetAvailabilityAsync(id, date, ct)))
            .WithSummary("Busy time slots of a space for a UTC day (date=YYYY-MM-DD)")
            .ProducesProblem(StatusCodes.Status404NotFound);

        var admin = group.MapGroup("/").RequireAuthorization(Policies.Admin);

        admin.MapPost("/", async (SpaceRequest request, SpaceService spaces, CancellationToken ct) =>
            {
                var space = await spaces.CreateAsync(request, ct);
                return TypedResults.Created($"/api/spaces/{space.Id}", space);
            })
            .WithValidation<SpaceRequest>()
            .WithSummary("Create a space (admin)");

        admin.MapPut("/{id:guid}", async (Guid id, SpaceRequest request, SpaceService spaces, CancellationToken ct) =>
                TypedResults.Ok(await spaces.UpdateAsync(id, request, ct)))
            .WithValidation<SpaceRequest>()
            .WithSummary("Update a space (admin)")
            .ProducesProblem(StatusCodes.Status404NotFound);

        admin.MapDelete("/{id:guid}", async (Guid id, SpaceService spaces, CancellationToken ct) =>
            {
                await spaces.DeactivateAsync(id, ct);
                return TypedResults.NoContent();
            })
            .WithSummary("Deactivate a space (admin). Existing bookings are kept.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
