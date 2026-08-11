using AssignmentSubmissionSystem.Domain.Entities;

namespace AssignmentSubmissionSystem.Application.Abstractions;

public interface IAttachmentRepository
{
    /// <summary>Loads an attachment with the owner graph the authorization checks need.</summary>
    Task<Attachment?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Attachment>> FindByAssignmentAsync(Guid assignmentId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Attachment>> FindBySubmissionAsync(Guid submissionId, CancellationToken cancellationToken);

    /// <summary>Current file count for an owner, used to enforce the per-owner cap.</summary>
    Task<int> CountForAssignmentAsync(Guid assignmentId, CancellationToken cancellationToken);

    Task<int> CountForSubmissionAsync(Guid submissionId, CancellationToken cancellationToken);

    Task AddAsync(Attachment attachment, CancellationToken cancellationToken);

    Task DeleteAsync(Attachment attachment, CancellationToken cancellationToken);
}
