using AssignmentSubmissionSystem.Application.Common.Paging;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSubmissionSystem.Infrastructure.Persistence;

/// <summary>
/// Turns an ordered <see cref="IQueryable{T}"/> into a single page, translated to
/// SQL <c>COUNT</c> + <c>OFFSET</c>/<c>LIMIT</c> rather than filtering in memory.
/// </summary>
public static class QueryablePagingExtensions
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        PageQuery page,
        CancellationToken cancellationToken)
    {
        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip(page.Skip)
            .Take(page.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(items, total, page.NormalizedPage, page.NormalizedPageSize);
    }
}
