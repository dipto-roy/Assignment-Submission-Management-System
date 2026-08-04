namespace AssignmentSubmissionSystem.Application.Subjects.Dtos;

public sealed record SubjectSummaryDto(
    Guid Id,
    string Name,
    string Code,
    Guid ClassId,
    string ClassName,
    IReadOnlyList<TeacherRefDto> Teachers);

public sealed record TeacherRefDto(Guid Id, string Name, string Email);

public sealed record CreateSubjectDto(string Name, string Code, Guid ClassId);

public sealed record UpdateSubjectDto(string Name, string Code);

public sealed record AssignTeacherDto(Guid TeacherId);
