using AssignmentSubmissionSystem.Application.Assignments.Dtos;
using AssignmentSubmissionSystem.Application.Common.Paging;
using AssignmentSubmissionSystem.Domain.Entities;

namespace AssignmentSubmissionSystem.Application.Abstractions;

public interface IAssignmentRepository
{
    Task<Assignment?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Admin view — every assignment, one page at a time.</summary>
    Task<PagedResult<Assignment>> FindAllAsync(AssignmentQuery query, CancellationToken cancellationToken);

    /// <summary>Teacher view — only assignments the teacher owns.</summary>
    Task<PagedResult<Assignment>> FindByTeacherAsync(Guid teacherId, AssignmentQuery query, CancellationToken cancellationToken);

    /// <summary>Student view — only Published assignments for the student's enrolled class(es).</summary>
    Task<PagedResult<Assignment>> FindPublishedForStudentAsync(Guid studentId, AssignmentQuery query, CancellationToken cancellationToken);

    Task<bool> IsTeacherAssignedToSubjectAsync(Guid teacherId, Guid subjectId, CancellationToken cancellationToken);

    Task<bool> IsStudentEnrolledInClassAsync(Guid studentId, Guid classId, CancellationToken cancellationToken);

    /// <summary>Every student enrolled in a class — the audience for a newly published assignment.</summary>
    Task<IReadOnlyList<Guid>> FindStudentIdsInClassAsync(Guid classId, CancellationToken cancellationToken);

    /// <summary>
    /// Published assignments whose deadline falls in a window, for the deadline reminder worker.
    /// </summary>
    Task<IReadOnlyList<Assignment>> FindPublishedDueBetweenAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken);

    Task AddAsync(Assignment assignment, CancellationToken cancellationToken);

    Task UpdateAsync(Assignment assignment, CancellationToken cancellationToken);

    Task DeleteAsync(Assignment assignment, CancellationToken cancellationToken);
}
