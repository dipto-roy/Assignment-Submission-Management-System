namespace AssignmentSubmissionSystem.Application.Classes.Dtos;

public sealed record ClassSummaryDto(Guid Id, string Name, string? Section);

public sealed record CreateClassDto(string Name, string? Section);

public sealed record UpdateClassDto(string Name, string? Section);

public sealed record EnrolledStudentDto(Guid Id, string Name, string Email);

public sealed record EnrollStudentDto(Guid StudentId);
