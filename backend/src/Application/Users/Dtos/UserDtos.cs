using AssignmentSubmissionSystem.Domain.Enums;

namespace AssignmentSubmissionSystem.Application.Users.Dtos;

public sealed record UserSummaryDto(Guid Id, string Name, string Email, string Role, DateTime CreatedAt);

/// <summary>
/// ClassId is only used when Role = Student, to auto-enroll the new student in a class
/// (see plan assumption: one student belongs to exactly one class).
/// </summary>
public sealed record CreateUserDto(string Name, string Email, string Password, UserRole Role, Guid? ClassId);

public sealed record UpdateUserDto(string Name, string Email, UserRole Role);
