using AssignmentSubmissionSystem.Application.Abstractions;
using AssignmentSubmissionSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSubmissionSystem.Infrastructure.Persistence.Repositories;

public sealed class AttachmentRepository(AppDbContext db) : IAttachmentRepository
{
    /// <summary>
    /// Both owner graphs are eagerly loaded because the authorization check needs whichever
    /// one is set: an assignment attachment is gated on the assignment's teacher and class,
    /// a submission attachment on the student plus the owning teacher of its assignment.
    /// </summary>
    public Task<Attachment?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Attachments
            .Include(a => a.Assignment).ThenInclude(x => x!.Subject)
            .Include(a => a.Submission).ThenInclude(x => x!.Assignment).ThenInclude(x => x.Subject)
            .SingleOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Attachment>> FindByAssignmentAsync(Guid assignmentId, CancellationToken cancellationToken) =>
        await db.Attachments.AsNoTracking()
            .Where(a => a.AssignmentId == assignmentId)
            .OrderBy(a => a.UploadedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Attachment>> FindBySubmissionAsync(Guid submissionId, CancellationToken cancellationToken) =>
        await db.Attachments.AsNoTracking()
            .Where(a => a.SubmissionId == submissionId)
            .OrderBy(a => a.UploadedAt)
            .ToListAsync(cancellationToken);

    public Task<int> CountForAssignmentAsync(Guid assignmentId, CancellationToken cancellationToken) =>
        db.Attachments.CountAsync(a => a.AssignmentId == assignmentId, cancellationToken);

    public Task<int> CountForSubmissionAsync(Guid submissionId, CancellationToken cancellationToken) =>
        db.Attachments.CountAsync(a => a.SubmissionId == submissionId, cancellationToken);

    public async Task AddAsync(Attachment attachment, CancellationToken cancellationToken)
    {
        db.Attachments.Add(attachment);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Attachment attachment, CancellationToken cancellationToken)
    {
        db.Attachments.Remove(attachment);
        await db.SaveChangesAsync(cancellationToken);
    }
}
