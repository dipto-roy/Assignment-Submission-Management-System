using AssignmentSubmissionSystem.Domain.Enums;

namespace AssignmentSubmissionSystem.Application.Common.Constants;

/// <summary>
/// String forms of <see cref="UserRole"/> for use in [Authorize(Roles = ...)] attributes,
/// where role names must be compile-time string constants.
/// </summary>
public static class Roles
{
    public const string Admin = nameof(UserRole.Admin);
    public const string Teacher = nameof(UserRole.Teacher);
    public const string Student = nameof(UserRole.Student);
}
