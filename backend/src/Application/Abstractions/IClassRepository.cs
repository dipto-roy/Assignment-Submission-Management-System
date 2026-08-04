using AssignmentSubmissionSystem.Domain.Entities;

namespace AssignmentSubmissionSystem.Application.Abstractions;

public interface IClassRepository
{
    Task<SchoolClass?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<SchoolClass>> FindAllAsync(CancellationToken cancellationToken);

    Task AddAsync(SchoolClass schoolClass, CancellationToken cancellationToken);

    Task UpdateAsync(SchoolClass schoolClass, CancellationToken cancellationToken);

    Task DeleteAsync(SchoolClass schoolClass, CancellationToken cancellationToken);
}
