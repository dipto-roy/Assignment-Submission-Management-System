namespace AssignmentSubmissionSystem.Application.Common.Paging;

/// <summary>
/// Base for query-string filters on collection endpoints: `?page=2&amp;pageSize=50`.
/// </summary>
/// <remarks>
/// Properties are settable because ASP.NET Core's complex-type query binder writes through
/// setters; the normalized values below are what callers should read, so an out-of-range
/// <c>page</c> or <c>pageSize</c> is clamped instead of rejected. Clamping keeps a stray
/// `?pageSize=100000` from turning into an unbounded query.
/// </remarks>
public class PageQuery
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = DefaultPageSize;

    public int NormalizedPage => Page < 1 ? 1 : Page;

    public int NormalizedPageSize => PageSize switch
    {
        < 1 => DefaultPageSize,
        > MaxPageSize => MaxPageSize,
        _ => PageSize
    };

    public int Skip => (NormalizedPage - 1) * NormalizedPageSize;
}
