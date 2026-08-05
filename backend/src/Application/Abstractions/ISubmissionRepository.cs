using AssignmentSubmissionSystem.Domain.Entities;

namespace AssignmentSubmissionSystem.Application.Abstractions;

public interface ISubmissionRepository
{
    Task<Submission?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Submission?> FindByAssignmentAndStudentAsync(Guid assignmentId, Guid studentId, CancellationToken cancellationToken);

    /// <summary>Student's own submissions only (business rule §7.4 — never another student's).</summary>
    Task<IReadOnlyList<Submission>> FindByStudentAsync(Guid studentId, CancellationToken cancellationToken);

    /// <summary>Teacher/Admin review view — every submission against one assignment, with student details.</summary>
    Task<IReadOnlyList<Submission>> FindByAssignmentAsync(Guid assignmentId, CancellationToken cancellationToken);

    Task AddAsync(Submission submission, CancellationToken cancellationToken);

    Task UpdateAsync(Submission submission, CancellationToken cancellationToken);
}
