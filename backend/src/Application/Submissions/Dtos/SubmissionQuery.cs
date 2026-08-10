using AssignmentSubmissionSystem.Application.Common.Paging;
using AssignmentSubmissionSystem.Domain.Enums;

namespace AssignmentSubmissionSystem.Application.Submissions.Dtos;

/// <summary>
/// Filters for the two submission list endpoints: `?status=Graded&amp;page=1&amp;pageSize=20`.
/// </summary>
public sealed class SubmissionQuery : PageQuery
{
    public SubmissionStatus? Status { get; set; }
}
