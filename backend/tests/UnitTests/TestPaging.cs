using AssignmentSubmissionSystem.Application.Common.Paging;

namespace AssignmentSubmissionSystem.UnitTests;

/// <summary>
/// Wraps entities in a single-page <see cref="PagedResult{T}"/> so repository mocks stay readable.
/// </summary>
public static class TestPaging
{
    public static PagedResult<T> Page<T>(params T[] items) =>
        new(items, items.Length, 1, PageQuery.DefaultPageSize);
}
