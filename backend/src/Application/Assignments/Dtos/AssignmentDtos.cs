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
    DateTime? UpdatedAt);

public sealed record CreateAssignmentDto(string Title, string Description, DateTime Deadline, int MaxMarks, Guid SubjectId);

public sealed record UpdateAssignmentDto(string Title, string Description, DateTime Deadline, int MaxMarks);

/// <summary>True = Published, false = revert to Draft.</summary>
public sealed record SetPublishStateDto(bool Publish);
