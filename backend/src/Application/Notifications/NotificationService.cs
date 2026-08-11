using AssignmentSubmissionSystem.Application.Abstractions;
using AssignmentSubmissionSystem.Application.Common.Exceptions;
using AssignmentSubmissionSystem.Application.Common.Paging;
using AssignmentSubmissionSystem.Application.Notifications.Dtos;
using AssignmentSubmissionSystem.Domain.Entities;
using AssignmentSubmissionSystem.Domain.Enums;

namespace AssignmentSubmissionSystem.Application.Notifications;

public interface INotificationService
{
    /// <summary>The caller's own notifications. There is deliberately no "read another user's" path.</summary>
    Task<PagedResult<NotificationDto>> GetMineAsync(Guid userId, NotificationQuery query, CancellationToken cancellationToken);

    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken);

    Task<NotificationDto> MarkReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken);

    Task<int> MarkAllReadAsync(Guid userId, CancellationToken cancellationToken);

    // ---- Triggers. Called by the services that own each event. ----

    Task NotifyAssignmentPublishedAsync(Assignment assignment, IReadOnlyCollection<Guid> studentIds, CancellationToken cancellationToken);

    Task NotifySubmissionReceivedAsync(Submission submission, string studentName, CancellationToken cancellationToken);

    Task NotifySubmissionGradedAsync(Submission submission, CancellationToken cancellationToken);

    Task NotifyDeadlineApproachingAsync(Assignment assignment, IReadOnlyCollection<Guid> studentIds, CancellationToken cancellationToken);
}

/// <summary>
/// Creates and reads in-app notifications.
/// </summary>
/// <remarks>
/// Message text is composed here rather than at each call site so wording stays consistent and
/// the triggers stay readable. Every read path is scoped to the calling user's id.
/// </remarks>
public sealed class NotificationService(INotificationRepository notificationRepository) : INotificationService
{
    public async Task<PagedResult<NotificationDto>> GetMineAsync(
        Guid userId,
        NotificationQuery query,
        CancellationToken cancellationToken)
    {
        var page = await notificationRepository.FindByUserAsync(userId, query, cancellationToken);
        return page.Map(ToDto);
    }

    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken) =>
        notificationRepository.CountUnreadAsync(userId, cancellationToken);

    public async Task<NotificationDto> MarkReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken)
    {
        var notification = await notificationRepository.FindByIdAsync(notificationId, cancellationToken)
            ?? throw new NotFoundAppException($"Notification {notificationId} was not found.");

        // Not-found rather than forbidden: whether some other user's notification exists is
        // not information this caller is entitled to.
        if (notification.UserId != userId)
        {
            throw new NotFoundAppException($"Notification {notificationId} was not found.");
        }

        // Idempotent — marking an already-read item again must not move its ReadAt timestamp.
        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await notificationRepository.UpdateAsync(notification, cancellationToken);
        }

        return ToDto(notification);
    }

    public Task<int> MarkAllReadAsync(Guid userId, CancellationToken cancellationToken) =>
        notificationRepository.MarkAllReadAsync(userId, DateTime.UtcNow, cancellationToken);

    public Task NotifyAssignmentPublishedAsync(
        Assignment assignment,
        IReadOnlyCollection<Guid> studentIds,
        CancellationToken cancellationToken)
    {
        var notifications = studentIds
            .Select(studentId => new Notification
            {
                UserId = studentId,
                Type = NotificationType.AssignmentPublished,
                Title = "New assignment published",
                Message = $"\"{assignment.Title}\" is due {FormatDeadline(assignment.Deadline)}.",
                AssignmentId = assignment.Id
            })
            .ToList();

        return notificationRepository.AddRangeAsync(notifications, cancellationToken);
    }

    public Task NotifySubmissionReceivedAsync(Submission submission, string studentName, CancellationToken cancellationToken)
    {
        var notification = new Notification
        {
            UserId = submission.Assignment.TeacherId,
            Type = NotificationType.SubmissionReceived,
            Title = "New submission received",
            Message = $"{studentName} submitted \"{submission.Assignment.Title}\".",
            AssignmentId = submission.AssignmentId,
            SubmissionId = submission.Id
        };

        return notificationRepository.AddRangeAsync(new[] { notification }, cancellationToken);
    }

    public Task NotifySubmissionGradedAsync(Submission submission, CancellationToken cancellationToken)
    {
        var notification = new Notification
        {
            UserId = submission.StudentId,
            Type = NotificationType.SubmissionGraded,
            Title = "Your submission was graded",
            Message = $"\"{submission.Assignment.Title}\" scored {submission.Marks}/{submission.Assignment.MaxMarks}.",
            AssignmentId = submission.AssignmentId,
            SubmissionId = submission.Id
        };

        return notificationRepository.AddRangeAsync(new[] { notification }, cancellationToken);
    }

    public Task NotifyDeadlineApproachingAsync(
        Assignment assignment,
        IReadOnlyCollection<Guid> studentIds,
        CancellationToken cancellationToken)
    {
        var notifications = studentIds
            .Select(studentId => new Notification
            {
                UserId = studentId,
                Type = NotificationType.DeadlineApproaching,
                Title = "Deadline approaching",
                Message = $"\"{assignment.Title}\" is due {FormatDeadline(assignment.Deadline)} and you have not submitted yet.",
                AssignmentId = assignment.Id
            })
            .ToList();

        // Duplicates are absorbed by the filtered unique index inside the repository, so an
        // overlapping worker tick cannot notify the same student twice.
        return notificationRepository.AddRangeAsync(notifications, cancellationToken);
    }

    /// <summary>
    /// Deadlines are stored and reasoned about in UTC; the message says so rather than
    /// implying a local time the recipient may not be in.
    /// </summary>
    private static string FormatDeadline(DateTime deadline) =>
        $"{deadline:dd MMM yyyy HH:mm} UTC";

    private static NotificationDto ToDto(Notification notification) => new(
        notification.Id,
        notification.Type.ToString(),
        notification.Title,
        notification.Message,
        notification.AssignmentId,
        notification.SubmissionId,
        notification.IsRead,
        notification.CreatedAt,
        notification.ReadAt);
}
