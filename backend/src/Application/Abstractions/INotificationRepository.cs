using AssignmentSubmissionSystem.Application.Common.Paging;
using AssignmentSubmissionSystem.Application.Notifications.Dtos;
using AssignmentSubmissionSystem.Domain.Entities;

namespace AssignmentSubmissionSystem.Application.Abstractions;

public interface INotificationRepository
{
    /// <summary>One user's own notifications, newest first.</summary>
    Task<PagedResult<Notification>> FindByUserAsync(Guid userId, NotificationQuery query, CancellationToken cancellationToken);

    Task<Notification?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Inserts a batch, skipping rows that collide with the deadline-reminder uniqueness rule.
    /// Batched because publishing an assignment notifies a whole class at once.
    /// </summary>
    Task AddRangeAsync(IReadOnlyCollection<Notification> notifications, CancellationToken cancellationToken);

    Task UpdateAsync(Notification notification, CancellationToken cancellationToken);

    /// <summary>Marks every unread notification for one user as read; returns how many changed.</summary>
    Task<int> MarkAllReadAsync(Guid userId, DateTime readAt, CancellationToken cancellationToken);
}
