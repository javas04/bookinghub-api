using BookingHub.Api.Infrastructure;
using BookingHub.Application.Bookings;

namespace BookingHub.Api.Endpoints;

public static class BookingEndpoints
{
    public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/bookings").WithTags("Bookings").RequireAuthorization();

        group.MapPost("/", async (CreateBookingRequest request, BookingService bookings, CancellationToken ct) =>
            {
                var booking = await bookings.CreateAsync(request, ct);
                return TypedResults.Created($"/api/bookings/{booking.Id}", booking);
            })
            .WithValidation<CreateBookingRequest>()
            .WithSummary("Book a space for the authenticated user")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/me", async ([AsParameters] MyBookingsQuery query, BookingService bookings, CancellationToken ct) =>
                TypedResults.Ok(await bookings.ListMineAsync(query, ct)))
            .WithSummary("Bookings of the authenticated user");

        group.MapGet("/{id:guid}", async (Guid id, BookingService bookings, CancellationToken ct) =>
                TypedResults.Ok(await bookings.GetAsync(id, ct)))
            .WithSummary("Get one of your bookings (admins can get any)")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/cancel", async (Guid id, BookingService bookings, CancellationToken ct) =>
                TypedResults.Ok(await bookings.CancelAsync(id, ct)))
            .WithSummary("Cancel a booking that hasn't started yet")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/", async ([AsParameters] BookingQuery query, BookingService bookings, CancellationToken ct) =>
                TypedResults.Ok(await bookings.ListAsync(query, ct)))
            .RequireAuthorization(Policies.Admin)
            .WithSummary("List every booking (admin)");

        return app;
    }
}
