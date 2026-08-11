using AssignmentSubmissionSystem.Application.Attachments.Dtos;

namespace AssignmentSubmissionSystem.Application.Assignments.Dtos;

public sealed record AssignmentSummaryDto(
    Guid Id,
    string Title,
    string Description,
    DateTime Deadline,
    int MaxMarks,
    string Status,
    Guid SubjectId,
    string SubjectName,
    Guid ClassId,
    string ClassName,
    Guid TeacherId,
    string TeacherName,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    // Embedded rather than fetched per row: a teacher's list of 30 assignments would
    // otherwise cost 30 extra requests to show which ones carry a brief.
    IReadOnlyList<AttachmentDto> Attachments);

public sealed record CreateAssignmentDto(string Title, string Description, DateTime Deadline, int MaxMarks, Guid SubjectId);

public sealed record UpdateAssignmentDto(string Title, string Description, DateTime Deadline, int MaxMarks);

/// <summary>True = Published, false = revert to Draft.</summary>
public sealed record SetPublishStateDto(bool Publish);
