using AssignmentSubmissionSystem.Domain.Entities;

namespace AssignmentSubmissionSystem.Application.Abstractions;

public interface IClassRepository
{
    Task<SchoolClass?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<SchoolClass>> FindAllAsync(CancellationToken cancellationToken);

    /// <summary>Students enrolled in a class, ordered by name.</summary>
    Task<IReadOnlyList<User>> FindStudentsAsync(Guid classId, CancellationToken cancellationToken);

    /// <summary>Every enrollment row for one student. Normally 0 or 1 (see plan §11).</summary>
    Task<IReadOnlyList<StudentClass>> FindEnrollmentsByStudentAsync(Guid studentId, CancellationToken cancellationToken);

    /// <summary>Replaces a student's enrollments with a single row for <paramref name="classId"/>.</summary>
    Task EnrollStudentAsync(Guid classId, Guid studentId, CancellationToken cancellationToken);

    Task RemoveEnrollmentAsync(StudentClass enrollment, CancellationToken cancellationToken);

    Task AddAsync(SchoolClass schoolClass, CancellationToken cancellationToken);

    Task UpdateAsync(SchoolClass schoolClass, CancellationToken cancellationToken);

    Task DeleteAsync(SchoolClass schoolClass, CancellationToken cancellationToken);
}
