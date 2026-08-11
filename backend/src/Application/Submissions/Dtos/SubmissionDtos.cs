using AssignmentSubmissionSystem.Application.Attachments.Dtos;
using AssignmentSubmissionSystem.Domain.Enums;

namespace AssignmentSubmissionSystem.Application.Submissions.Dtos;

public sealed record SubmissionSummaryDto(
    Guid Id,
    Guid AssignmentId,
    string AssignmentTitle,
    DateTime AssignmentDeadline,
    Guid StudentId,
    string Content,
    string Status,
    int? Marks,
    string? Feedback,
    DateTime SubmittedAt,
    DateTime? UpdatedAt,
    DateTime? GradedAt,
    IReadOnlyList<AttachmentDto> Attachments);

/// <summary>
/// Teacher/Admin review view — adds student identity, which students never see for each other (business rule §7.4).
/// </summary>
public sealed record SubmissionDetailDto(
    Guid Id,
    Guid AssignmentId,
    string AssignmentTitle,
    DateTime AssignmentDeadline,
    int AssignmentMaxMarks,
    Guid StudentId,
    string StudentName,
    string StudentEmail,
    string Content,
    string Status,
    int? Marks,
    string? Feedback,
    DateTime SubmittedAt,
    DateTime? UpdatedAt,
    DateTime? GradedAt,
    IReadOnlyList<AttachmentDto> Attachments);

public sealed record CreateSubmissionDto(string Content);

public sealed record UpdateSubmissionDto(string Content);

public sealed record GradeSubmissionDto(int Marks, string? Feedback);

public sealed record SetSubmissionStatusDto(SubmissionStatus Status);
