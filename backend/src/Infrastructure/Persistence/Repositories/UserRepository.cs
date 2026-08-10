using AssignmentSubmissionSystem.Application.Abstractions;
using AssignmentSubmissionSystem.Application.Common.Paging;
using AssignmentSubmissionSystem.Application.Users.Dtos;
using AssignmentSubmissionSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSubmissionSystem.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        db.Users.SingleOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);

    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Users.SingleOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<PagedResult<User>> FindPageAsync(UserQuery query, CancellationToken cancellationToken)
    {
        var users = db.Users.AsNoTracking().AsQueryable();

        if (query.Role is { } role)
        {
            users = users.Where(u => u.Role == role);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // EF.Functions.ILike is Npgsql's case-insensitive LIKE — parameterised, so the
            // search term never reaches SQL as concatenated text.
            var pattern = $"%{query.Search.Trim()}%";
            users = users.Where(u => EF.Functions.ILike(u.Name, pattern) || EF.Functions.ILike(u.Email, pattern));
        }

        return users.OrderBy(u => u.Name).ToPagedResultAsync(query, cancellationToken);
    }

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
