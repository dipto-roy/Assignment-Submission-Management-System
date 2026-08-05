using AssignmentSubmissionSystem.Application.Abstractions;
using AssignmentSubmissionSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSubmissionSystem.Infrastructure.Persistence.Repositories;

public sealed class SubmissionRepository(AppDbContext db) : ISubmissionRepository
{
    public Task<Submission?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Submissions
            .Include(s => s.Assignment).ThenInclude(a => a.Subject)
            .Include(s => s.Student)
            .SingleOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<Submission?> FindByAssignmentAndStudentAsync(Guid assignmentId, Guid studentId, CancellationToken cancellationToken) =>
        db.Submissions.SingleOrDefaultAsync(
            s => s.AssignmentId == assignmentId && s.StudentId == studentId,
            cancellationToken);

    public async Task<IReadOnlyList<Submission>> FindByStudentAsync(Guid studentId, CancellationToken cancellationToken) =>
        await db.Submissions
            .AsNoTracking()
            .Where(s => s.StudentId == studentId)
            .Include(s => s.Assignment).ThenInclude(a => a.Subject)
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Submission>> FindByAssignmentAsync(Guid assignmentId, CancellationToken cancellationToken) =>
        await db.Submissions
            .AsNoTracking()
            .Where(s => s.AssignmentId == assignmentId)
            .Include(s => s.Assignment).ThenInclude(a => a.Subject)
            .Include(s => s.Student)
            .OrderBy(s => s.Student.Name)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Submission submission, CancellationToken cancellationToken)
    {
        db.Submissions.Add(submission);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task UpdateAsync(Submission submission, CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);
}
