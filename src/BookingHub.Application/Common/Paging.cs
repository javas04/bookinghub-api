using Microsoft.EntityFrameworkCore;

namespace BookingHub.Application.Common;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public static class PagingExtensions
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    /// <summary>
    /// Runs a COUNT and a page query. Out-of-range values are clamped instead of rejected,
    /// so clients can't request huge pages.
    /// </summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        var safePage = Math.Max(page ?? 1, 1);
        var safeSize = Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((safePage - 1) * safeSize)
            .Take(safeSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(items, safePage, safeSize, total);
    }
}
