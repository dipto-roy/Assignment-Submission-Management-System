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
    DateTime? GradedAt);

public sealed record CreateSubmissionDto(string Content);

public sealed record UpdateSubmissionDto(string Content);
