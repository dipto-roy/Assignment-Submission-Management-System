namespace AssignmentSubmissionSystem.Application.Common.Paging;

/// <summary>
/// One page of results plus the totals the client needs to render pagination controls.
/// </summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);

    /// <summary>Projects the entities in this page to DTOs while preserving the page totals.</summary>
    public PagedResult<TOut> Map<TOut>(Func<T, TOut> selector) =>
        new(Items.Select(selector).ToList(), Total, Page, PageSize);

    /// <summary>
    /// Page totals for <see cref="ApiResponse{T}.Meta"/>. Keeping them in <c>meta</c> rather than
    /// wrapping <c>data</c> means paginated endpoints stay shape-compatible with clients that
    /// only read <c>data</c>.
    /// </summary>
    public PageMeta ToMeta() => new(Total, Page, PageSize, TotalPages);
}

public sealed record PageMeta(int Total, int Page, int PageSize, int TotalPages);
