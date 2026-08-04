using AssignmentSubmissionSystem.Domain.Entities;

namespace AssignmentSubmissionSystem.Application.Abstractions;

public interface ISubjectRepository
{
    Task<Subject?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Subject>> FindAllAsync(CancellationToken cancellationToken);

    Task AddAsync(Subject subject, CancellationToken cancellationToken);

    Task UpdateAsync(Subject subject, CancellationToken cancellationToken);

    Task DeleteAsync(Subject subject, CancellationToken cancellationToken);

    Task AssignTeacherAsync(Guid subjectId, Guid teacherId, CancellationToken cancellationToken);
}
