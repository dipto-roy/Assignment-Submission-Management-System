using AssignmentSubmissionSystem.Domain.Entities;

namespace AssignmentSubmissionSystem.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<User>> FindAllAsync(CancellationToken cancellationToken);

    Task<bool> ExistsByEmailAsync(string email, Guid? excludeUserId, CancellationToken cancellationToken);

    Task AddAsync(User user, CancellationToken cancellationToken);

    Task UpdateAsync(User user, CancellationToken cancellationToken);

    Task DeleteAsync(User user, CancellationToken cancellationToken);
}
