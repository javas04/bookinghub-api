namespace BookingHub.Application.Common;

public static class DateTimeExtensions
{
    /// <summary>
    /// The API works in UTC. Values without an offset (e.g. "2026-12-01T10:00:00") are taken as UTC,
    /// values with an offset are converted to UTC.
    /// </summary>
    public static DateTime ToUtc(this DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };
}
