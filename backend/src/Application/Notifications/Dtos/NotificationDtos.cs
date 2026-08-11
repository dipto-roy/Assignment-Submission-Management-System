using AssignmentSubmissionSystem.Application.Common.Paging;

namespace AssignmentSubmissionSystem.Application.Notifications.Dtos;

public sealed record NotificationDto(
    Guid Id,
    string Type,
    string Title,
    string Message,
    Guid? AssignmentId,
    Guid? SubmissionId,
    bool IsRead,
    DateTime CreatedAt,
    DateTime? ReadAt);

/// <summary>Filters for the notification list. Recipient is never a parameter — it is the caller.</summary>
public sealed class NotificationQuery : PageQuery
{
    /// <summary>Restricts the page to unread items, for the "you have N new" view.</summary>
    public bool UnreadOnly { get; set; }
}

public sealed record UnreadCountDto(int Unread);
