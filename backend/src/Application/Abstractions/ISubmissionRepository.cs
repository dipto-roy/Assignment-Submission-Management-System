using AssignmentSubmissionSystem.Application.Common.Paging;
using AssignmentSubmissionSystem.Application.Submissions.Dtos;
using AssignmentSubmissionSystem.Domain.Entities;

namespace AssignmentSubmissionSystem.Application.Abstractions;

public interface ISubmissionRepository
{
    Task<Submission?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Submission?> FindByAssignmentAndStudentAsync(Guid assignmentId, Guid studentId, CancellationToken cancellationToken);

    /// <summary>Student's own submissions only (business rule §7.4 — never another student's).</summary>
    Task<PagedResult<Submission>> FindByStudentAsync(Guid studentId, SubmissionQuery query, CancellationToken cancellationToken);

    /// <summary>Teacher/Admin review view — every submission against one assignment, with student details.</summary>
    Task<PagedResult<Submission>> FindByAssignmentAsync(Guid assignmentId, SubmissionQuery query, CancellationToken cancellationToken);

    /// <summary>
    /// Students who have already submitted against one assignment. Subtracted from the class
    /// roster so a deadline reminder only reaches those who still owe work.
    /// </summary>
    Task<IReadOnlyList<Guid>> FindStudentIdsWithSubmissionAsync(Guid assignmentId, CancellationToken cancellationToken);

    Task AddAsync(Submission submission, CancellationToken cancellationToken);

    Task UpdateAsync(Submission submission, CancellationToken cancellationToken);
}
