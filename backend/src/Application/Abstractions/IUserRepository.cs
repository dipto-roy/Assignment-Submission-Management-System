using AssignmentSubmissionSystem.Domain.Entities;

namespace AssignmentSubmissionSystem.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken);
}
