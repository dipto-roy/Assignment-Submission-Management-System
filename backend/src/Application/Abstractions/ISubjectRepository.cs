using AssignmentSubmissionSystem.Application.Common.Paging;
using AssignmentSubmissionSystem.Domain.Entities;

namespace AssignmentSubmissionSystem.Application.Abstractions;

public interface ISubjectRepository
{
    Task<Subject?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>One page of subjects, ordered by name.</summary>
    Task<PagedResult<Subject>> FindAllAsync(PageQuery page, CancellationToken cancellationToken);

    Task AddAsync(Subject subject, CancellationToken cancellationToken);

    Task UpdateAsync(Subject subject, CancellationToken cancellationToken);

    Task DeleteAsync(Subject subject, CancellationToken cancellationToken);

    Task AssignTeacherAsync(Guid subjectId, Guid teacherId, CancellationToken cancellationToken);
}
