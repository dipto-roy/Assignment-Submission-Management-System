using AssignmentSubmissionSystem.Domain.Entities;

namespace AssignmentSubmissionSystem.Application.Abstractions;

public interface IAssignmentRepository
{
    Task<Assignment?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Admin view — every assignment.</summary>
    Task<IReadOnlyList<Assignment>> FindAllAsync(CancellationToken cancellationToken);

    /// <summary>Teacher view — only assignments the teacher owns.</summary>
    Task<IReadOnlyList<Assignment>> FindByTeacherAsync(Guid teacherId, CancellationToken cancellationToken);

    /// <summary>Student view — only Published assignments for the student's enrolled class(es).</summary>
    Task<IReadOnlyList<Assignment>> FindPublishedForStudentAsync(Guid studentId, CancellationToken cancellationToken);

    Task<bool> IsTeacherAssignedToSubjectAsync(Guid teacherId, Guid subjectId, CancellationToken cancellationToken);

    Task<bool> IsStudentEnrolledInClassAsync(Guid studentId, Guid classId, CancellationToken cancellationToken);

    Task AddAsync(Assignment assignment, CancellationToken cancellationToken);

    Task UpdateAsync(Assignment assignment, CancellationToken cancellationToken);

    Task DeleteAsync(Assignment assignment, CancellationToken cancellationToken);
}
