using AssignmentSubmissionSystem.Application.Attachments.Dtos;
using AssignmentSubmissionSystem.Domain.Entities;

namespace AssignmentSubmissionSystem.Application.Attachments;

/// <summary>
/// Shared projection to <see cref="AttachmentDto"/>. Lives here rather than on each service so
/// assignments, submissions and the attachment endpoints cannot drift into exposing different
/// shapes — in particular, none of them leaks the storage key.
/// </summary>
public static class AttachmentMapper
{
    public static AttachmentDto ToDto(Attachment attachment) => new(
        attachment.Id,
        attachment.FileName,
        attachment.ContentType,
        attachment.SizeBytes,
        attachment.UploadedById,
        attachment.UploadedAt);

    public static IReadOnlyList<AttachmentDto> ToDtos(IEnumerable<Attachment>? attachments) =>
        attachments?.OrderBy(a => a.UploadedAt).Select(ToDto).ToList() ?? [];
}
