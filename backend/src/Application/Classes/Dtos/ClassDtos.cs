namespace AssignmentSubmissionSystem.Application.Classes.Dtos;

public sealed record ClassSummaryDto(Guid Id, string Name, string? Section);

public sealed record CreateClassDto(string Name, string? Section);

public sealed record UpdateClassDto(string Name, string? Section);
