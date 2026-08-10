using AssignmentSubmissionSystem.Application.Common.Paging;
using AssignmentSubmissionSystem.Domain.Enums;

namespace AssignmentSubmissionSystem.Application.Users.Dtos;

/// <summary>
/// Filters for <c>GET /users</c>: `?role=Teacher&amp;search=ada&amp;page=1&amp;pageSize=20`.
/// </summary>
public sealed class UserQuery : PageQuery
{
    public UserRole? Role { get; set; }

    /// <summary>Case-insensitive substring match against name or email.</summary>
    public string? Search { get; set; }
}
