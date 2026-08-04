using AssignmentSubmissionSystem.Application.Abstractions;
using AssignmentSubmissionSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSubmissionSystem.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        db.Users.SingleOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);

    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Users.SingleOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<IReadOnlyList<User>> FindAllAsync(CancellationToken cancellationToken) =>
        await db.Users.AsNoTracking().OrderBy(u => u.Name).ToListAsync(cancellationToken);

    public Task<bool> ExistsByEmailAsync(string email, Guid? excludeUserId, CancellationToken cancellationToken) =>
        db.Users.AnyAsync(
            u => u.Email.ToLower() == email.ToLower() && (excludeUserId == null || u.Id != excludeUserId),
            cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);

    public async Task DeleteAsync(User user, CancellationToken cancellationToken)
    {
        db.Users.Remove(user);
        await db.SaveChangesAsync(cancellationToken);
    }
}
