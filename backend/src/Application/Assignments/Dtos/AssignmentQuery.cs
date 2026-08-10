using AssignmentSubmissionSystem.Application.Common.Paging;
using AssignmentSubmissionSystem.Domain.Enums;

namespace AssignmentSubmissionSystem.Application.Assignments.Dtos;

/// <summary>
/// Filters for <c>GET /assignments</c>: `?status=Published&amp;subjectId=…&amp;search=essay`.
/// </summary>
/// <remarks>
/// These filters narrow what the caller's role already allows — they never widen it. A student
/// passing <c>status=Draft</c> gets an empty page, not another student's drafts, because the
/// role-scoped query is applied first (business rule §7.3).
/// </remarks>
public sealed class AssignmentQuery : PageQuery
{
    public AssignmentStatus? Status { get; set; }

    public Guid? SubjectId { get; set; }

    public Guid? ClassId { get; set; }

    /// <summary>Case-insensitive substring match against the assignment title.</summary>
    public string? Search { get; set; }
}
