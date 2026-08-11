using AssignmentSubmissionSystem.Application.Abstractions;
using AssignmentSubmissionSystem.Application.Attachments.Dtos;
using AssignmentSubmissionSystem.Application.Common.Exceptions;
using AssignmentSubmissionSystem.Application.Options;
using AssignmentSubmissionSystem.Domain.Entities;
using AssignmentSubmissionSystem.Domain.Enums;
using Microsoft.Extensions.Options;

namespace AssignmentSubmissionSystem.Application.Attachments;

/// <summary>A file's bytes plus the metadata needed to serve it back to a browser.</summary>
public sealed record AttachmentDownload(Stream Content, string ContentType, string FileName);

public interface IAttachmentService
{
    /// <summary>Teacher's brief or rubric. Owning teacher only (business rule §7.5).</summary>
    Task<AttachmentDto> UploadToAssignmentAsync(Guid assignmentId, Guid userId, UserRole role, FileUpload file, CancellationToken cancellationToken);

    /// <summary>Student's work. Owning student only, and only while the deadline stands (§7.2).</summary>
    Task<AttachmentDto> UploadToSubmissionAsync(Guid submissionId, Guid userId, FileUpload file, CancellationToken cancellationToken);

    Task<IReadOnlyList<AttachmentDto>> ListForAssignmentAsync(Guid assignmentId, CancellationToken cancellationToken);

    Task<IReadOnlyList<AttachmentDto>> ListForSubmissionAsync(Guid submissionId, CancellationToken cancellationToken);

    Task<AttachmentDownload> DownloadAsync(Guid attachmentId, Guid userId, UserRole role, CancellationToken cancellationToken);

    Task DeleteAsync(Guid attachmentId, Guid userId, UserRole role, CancellationToken cancellationToken);
}

public sealed class AttachmentService(
    IAttachmentRepository attachmentRepository,
    IAssignmentRepository assignmentRepository,
    ISubmissionRepository submissionRepository,
    IFileStorage fileStorage,
    IOptions<StorageOptions> storageOptions) : IAttachmentService
{
    private readonly StorageOptions options = storageOptions.Value;

    public async Task<AttachmentDto> UploadToAssignmentAsync(
        Guid assignmentId,
        Guid userId,
        UserRole role,
        FileUpload file,
        CancellationToken cancellationToken)
    {
        var assignment = await assignmentRepository.FindByIdAsync(assignmentId, cancellationToken)
            ?? throw new NotFoundAppException($"Assignment {assignmentId} was not found.");

        // Business rule §7.5: only the owning teacher may change an assignment. Admin is
        // included because admin already has full oversight of assignments.
        if (role != UserRole.Admin && assignment.TeacherId != userId)
        {
            throw new ForbiddenAppException("You do not own this assignment.");
        }

        var existing = await attachmentRepository.CountForAssignmentAsync(assignmentId, cancellationToken);
        EnsureUnderFileCap(existing);
        AttachmentRules.EnsureAcceptable(file, options);

        return await StoreAsync(file, userId, a => a.AssignmentId = assignmentId, cancellationToken);
    }

    public async Task<AttachmentDto> UploadToSubmissionAsync(
        Guid submissionId,
        Guid userId,
        FileUpload file,
        CancellationToken cancellationToken)
    {
        var submission = await submissionRepository.FindByIdAsync(submissionId, cancellationToken)
            ?? throw new NotFoundAppException($"Submission {submissionId} was not found.");

        // Business rule §7.4: a student may only touch their own submission.
        if (submission.StudentId != userId)
        {
            throw new ForbiddenAppException("You do not own this submission.");
        }

        // Business rule §7.2: the submission is locked once the deadline passes. Attaching a
        // file is a change to the submission, so it is barred on the same terms as editing the
        // text — otherwise the deadline would be trivially sidesteppable by uploading late.
        if (DateTime.UtcNow > submission.Assignment.Deadline)
        {
            throw new BadRequestAppException("The deadline for this assignment has passed; the submission is locked.");
        }

        var existing = await attachmentRepository.CountForSubmissionAsync(submissionId, cancellationToken);
        EnsureUnderFileCap(existing);
        AttachmentRules.EnsureAcceptable(file, options);

        return await StoreAsync(file, userId, a => a.SubmissionId = submissionId, cancellationToken);
    }

    public async Task<IReadOnlyList<AttachmentDto>> ListForAssignmentAsync(Guid assignmentId, CancellationToken cancellationToken)
    {
        var items = await attachmentRepository.FindByAssignmentAsync(assignmentId, cancellationToken);
        return AttachmentMapper.ToDtos(items);
    }

    public async Task<IReadOnlyList<AttachmentDto>> ListForSubmissionAsync(Guid submissionId, CancellationToken cancellationToken)
    {
        var items = await attachmentRepository.FindBySubmissionAsync(submissionId, cancellationToken);
        return AttachmentMapper.ToDtos(items);
    }

    public async Task<AttachmentDownload> DownloadAsync(
        Guid attachmentId,
        Guid userId,
        UserRole role,
        CancellationToken cancellationToken)
    {
        var attachment = await attachmentRepository.FindByIdAsync(attachmentId, cancellationToken)
            ?? throw new NotFoundAppException($"Attachment {attachmentId} was not found.");

        await EnsureCanReadAsync(attachment, userId, role, cancellationToken);

        var stored = await fileStorage.OpenReadAsync(attachment.StorageKey, cancellationToken);

        // The content type recorded at upload wins over whatever the provider reports: the
        // local provider has no way to know it, and it is the value this system validated.
        return new AttachmentDownload(stored.Content, attachment.ContentType, attachment.FileName);
    }

    public async Task DeleteAsync(Guid attachmentId, Guid userId, UserRole role, CancellationToken cancellationToken)
    {
        var attachment = await attachmentRepository.FindByIdAsync(attachmentId, cancellationToken)
            ?? throw new NotFoundAppException($"Attachment {attachmentId} was not found.");

        EnsureCanDelete(attachment, userId, role);

        // Row first, bytes second. The reverse order risks a row that points at nothing if the
        // delete succeeds and the commit does not; this way the worst case is an orphaned
        // object in the store, which the provider logs and which harms nobody.
        await attachmentRepository.DeleteAsync(attachment, cancellationToken);
        await fileStorage.DeleteAsync(attachment.StorageKey, cancellationToken);
    }

    private void EnsureUnderFileCap(int existingCount)
    {
        if (existingCount >= options.MaxFilesPerOwner)
        {
            throw new BadRequestAppException(
                $"A maximum of {options.MaxFilesPerOwner} files may be attached. Remove one before uploading another.");
        }
    }

    private async Task<AttachmentDto> StoreAsync(
        FileUpload file,
        Guid uploaderId,
        Action<Attachment> setOwner,
        CancellationToken cancellationToken)
    {
        var safeName = AttachmentRules.SanitizeFileName(file.FileName);
        var stored = await fileStorage.SaveAsync(file.Content, safeName, file.ContentType, cancellationToken);

        var attachment = new Attachment
        {
            FileName = safeName,
            ContentType = file.ContentType,
            // The provider's count, not the client's claim: Length is only a header until the
            // bytes have actually been written.
            SizeBytes = stored.SizeBytes,
            StorageKey = stored.StorageKey,
            StorageProvider = fileStorage.ProviderName,
            UploadedById = uploaderId
        };

        setOwner(attachment);

        await attachmentRepository.AddAsync(attachment, cancellationToken);
        return AttachmentMapper.ToDto(attachment);
    }

    /// <summary>
    /// Read access mirrors visibility of the owning record, so a file is never more reachable
    /// than the assignment or submission it belongs to.
    /// </summary>
    private async Task EnsureCanReadAsync(Attachment attachment, Guid userId, UserRole role, CancellationToken cancellationToken)
    {
        if (role == UserRole.Admin)
        {
            return;
        }

        if (attachment.Assignment is { } assignment)
        {
            if (role == UserRole.Teacher && assignment.TeacherId == userId)
            {
                return;
            }

            // A student may read the brief only once it is published and only if they are in
            // the class it was set for — the same test the assignment listing applies.
            if (role == UserRole.Student && assignment.Status == AssignmentStatus.Published)
            {
                var enrolled = await assignmentRepository.IsStudentEnrolledInClassAsync(
                    userId, assignment.Subject.ClassId, cancellationToken);

                if (enrolled)
                {
                    return;
                }
            }

            throw new ForbiddenAppException("You do not have access to this file.");
        }

        if (attachment.Submission is { } submission)
        {
            // Business rule §7.4: the student who submitted it, plus the teacher who set the
            // assignment and has to mark it. No other student, ever.
            if (submission.StudentId == userId)
            {
                return;
            }

            if (role == UserRole.Teacher && submission.Assignment.TeacherId == userId)
            {
                return;
            }

            throw new ForbiddenAppException("You do not have access to this file.");
        }

        // Unreachable while the check constraint holds; treated as a failure rather than a
        // silent allow, because an ownerless row is a corrupt row.
        throw new ForbiddenAppException("You do not have access to this file.");
    }

    /// <summary>
    /// Deletion is narrower than reading: an admin, or the person who owns the parent record.
    /// A teacher may not delete a student's submitted file, and vice versa.
    /// </summary>
    private static void EnsureCanDelete(Attachment attachment, Guid userId, UserRole role)
    {
        if (role == UserRole.Admin)
        {
            return;
        }

        if (attachment.Assignment is { } assignment && assignment.TeacherId == userId)
        {
            return;
        }

        if (attachment.Submission is { } submission && submission.StudentId == userId)
        {
            // Same deadline lock as uploading: after the deadline the submission is frozen,
            // so removing evidence from it is not allowed either.
            if (DateTime.UtcNow > submission.Assignment.Deadline)
            {
                throw new BadRequestAppException("The deadline for this assignment has passed; the submission is locked.");
            }

            return;
        }

        throw new ForbiddenAppException("You may not delete this file.");
    }

}
