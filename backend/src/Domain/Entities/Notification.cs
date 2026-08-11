using AssignmentSubmissionSystem.Domain.Enums;

namespace AssignmentSubmissionSystem.Domain.Entities;

/// <summary>
/// One in-app message addressed to one user. Notifications are per-recipient rows rather than
/// a shared event with a join table: a class of 30 students produces 30 rows, which keeps
/// read state, querying and authorization trivially per-user.
/// </summary>
public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>The recipient. A user may only ever read their own notifications.</summary>
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public NotificationType Type { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional deep-link targets. Nullable and set to null on delete rather than cascading,
    /// so removing an assignment does not silently erase the user's notification history.
    /// </summary>
    public Guid? AssignmentId { get; set; }
    public Assignment? Assignment { get; set; }

    public Guid? SubmissionId { get; set; }
    public Submission? Submission { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReadAt { get; set; }
}
