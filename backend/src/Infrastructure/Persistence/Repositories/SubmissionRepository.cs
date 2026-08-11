using AssignmentSubmissionSystem.Application.Abstractions;
using AssignmentSubmissionSystem.Application.Common.Paging;
using AssignmentSubmissionSystem.Application.Submissions.Dtos;
using AssignmentSubmissionSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSubmissionSystem.Infrastructure.Persistence.Repositories;

public sealed class SubmissionRepository(AppDbContext db) : ISubmissionRepository
{
    public Task<Submission?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Submissions
            .Include(s => s.Assignment).ThenInclude(a => a.Subject)
            .Include(s => s.Student)
            .Include(s => s.Attachments)
            .SingleOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<Submission?> FindByAssignmentAndStudentAsync(Guid assignmentId, Guid studentId, CancellationToken cancellationToken) =>
        db.Submissions.SingleOrDefaultAsync(
            s => s.AssignmentId == assignmentId && s.StudentId == studentId,
            cancellationToken);

    public Task<PagedResult<Submission>> FindByStudentAsync(Guid studentId, SubmissionQuery query, CancellationToken cancellationToken) =>
        WithStatusFilter(db.Submissions.AsNoTracking().Where(s => s.StudentId == studentId), query)
            .Include(s => s.Assignment).ThenInclude(a => a.Subject)
            .Include(s => s.Attachments)
            .OrderByDescending(s => s.SubmittedAt)
            .ToPagedResultAsync(query, cancellationToken);

    public Task<PagedResult<Submission>> FindByAssignmentAsync(Guid assignmentId, SubmissionQuery query, CancellationToken cancellationToken) =>
        WithStatusFilter(db.Submissions.AsNoTracking().Where(s => s.AssignmentId == assignmentId), query)
            .Include(s => s.Assignment).ThenInclude(a => a.Subject)
            .Include(s => s.Student)
            .Include(s => s.Attachments)
            .OrderBy(s => s.Student.Name)
            .ToPagedResultAsync(query, cancellationToken);

    private static IQueryable<Submission> WithStatusFilter(IQueryable<Submission> scoped, SubmissionQuery query) =>
        query.Status is { } status ? scoped.Where(s => s.Status == status) : scoped;

    public async Task<IReadOnlyList<Guid>> FindStudentIdsWithSubmissionAsync(Guid assignmentId, CancellationToken cancellationToken) =>
        await db.Submissions.AsNoTracking()
            .Where(s => s.AssignmentId == assignmentId)
            .Select(s => s.StudentId)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Submission submission, CancellationToken cancellationToken)
    {
        db.Submissions.Add(submission);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task UpdateAsync(Submission submission, CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);
}
