using BookingHub.Application.Auth;
using BookingHub.Application.Bookings;

namespace BookingHub.UnitTests.Application;

public sealed class ValidatorTests
{
    [Theory]
    [InlineData("short1A")] // too short
    [InlineData("alllowercase1")] // no uppercase
    [InlineData("ALLUPPERCASE1")] // no lowercase
    [InlineData("NoDigitsHere")] // no digit
    public void Register_rejects_weak_passwords(string password)
    {
        var result = new RegisterRequestValidator().Validate(new RegisterRequest("ana@example.com", "Ana", password));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterRequest.Password));
    }

    [Fact]
    public void Register_accepts_valid_request()
    {
        var result = new RegisterRequestValidator().Validate(new RegisterRequest("ana@example.com", "Ana", "Secret123"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateBooking_requires_end_after_start()
    {
        var start = new DateTime(2030, 1, 1, 10, 0, 0, DateTimeKind.Utc);

        var result = new CreateBookingRequestValidator().Validate(
            new CreateBookingRequest(Guid.NewGuid(), start, start, 1, null));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateBookingRequest.EndUtc));
    }
}
