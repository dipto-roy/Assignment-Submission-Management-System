using AssignmentSubmissionSystem.Application.Abstractions;
using AssignmentSubmissionSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSubmissionSystem.Infrastructure.Persistence.Repositories;

public sealed class ClassRepository(AppDbContext db) : IClassRepository
{
    public Task<SchoolClass?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Classes.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<SchoolClass>> FindAllAsync(CancellationToken cancellationToken) =>
        await db.Classes.AsNoTracking().OrderBy(c => c.Name).ThenBy(c => c.Section).ToListAsync(cancellationToken);

    public async Task AddAsync(SchoolClass schoolClass, CancellationToken cancellationToken)
    {
        db.Classes.Add(schoolClass);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task UpdateAsync(SchoolClass schoolClass, CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);

    public async Task DeleteAsync(SchoolClass schoolClass, CancellationToken cancellationToken)
    {
        db.Classes.Remove(schoolClass);
        await db.SaveChangesAsync(cancellationToken);
    }
}
