using AssignmentSubmissionSystem.Application.Abstractions;
using AssignmentSubmissionSystem.Application.Common.Exceptions;
using AssignmentSubmissionSystem.Application.Notifications;
using AssignmentSubmissionSystem.Domain.Entities;
using AssignmentSubmissionSystem.Domain.Enums;
using Moq;

namespace AssignmentSubmissionSystem.UnitTests.Notifications;

public sealed class NotificationServiceTests
{
    private readonly Mock<INotificationRepository> _notificationRepository = new();
    private readonly NotificationService _sut;

    private IReadOnlyCollection<Notification>? _captured;

    public NotificationServiceTests()
    {
        _notificationRepository
            .Setup(r => r.AddRangeAsync(It.IsAny<IReadOnlyCollection<Notification>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<Notification>, CancellationToken>((rows, _) => _captured = rows)
            .Returns(Task.CompletedTask);

        _sut = new NotificationService(_notificationRepository.Object);
    }

    private static Assignment BuildAssignment() => new()
    {
        Title = "Algebra",
        Deadline = new DateTime(2026, 9, 1, 17, 0, 0, DateTimeKind.Utc),
        MaxMarks = 50,
        TeacherId = Guid.NewGuid()
    };

    [Fact]
    public async Task NotifyAssignmentPublished_CreatesOneRowPerStudent()
    {
        var assignment = BuildAssignment();
        var students = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        await _sut.NotifyAssignmentPublishedAsync(assignment, students, CancellationToken.None);

        Assert.NotNull(_captured);
        Assert.Equal(3, _captured!.Count);
        Assert.Equal(students.OrderBy(s => s), _captured.Select(n => n.UserId).OrderBy(s => s));
        Assert.All(_captured, n => Assert.Equal(NotificationType.AssignmentPublished, n.Type));
        Assert.All(_captured, n => Assert.Equal(assignment.Id, n.AssignmentId));
    }

    [Fact]
    public async Task NotifyAssignmentPublished_WritesNothing_ForAnEmptyClass()
    {
        await _sut.NotifyAssignmentPublishedAsync(BuildAssignment(), Array.Empty<Guid>(), CancellationToken.None);

        Assert.Empty(_captured!);
    }

    [Fact]
    public async Task NotifyAssignmentPublished_StatesTheDeadlineInUtc()
    {
        // Recipients may be anywhere; an unqualified time would be read as local.
        await _sut.NotifyAssignmentPublishedAsync(BuildAssignment(), new[] { Guid.NewGuid() }, CancellationToken.None);

        Assert.Contains("UTC", _captured!.Single().Message);
    }

    [Fact]
    public async Task NotifySubmissionReceived_AddressesTheOwningTeacher()
    {
        var assignment = BuildAssignment();
        var submission = new Submission { Assignment = assignment, AssignmentId = assignment.Id, StudentId = Guid.NewGuid() };

        await _sut.NotifySubmissionReceivedAsync(submission, "Ayesha Rahman", CancellationToken.None);

        var notification = _captured!.Single();
        Assert.Equal(assignment.TeacherId, notification.UserId);
        Assert.Equal(NotificationType.SubmissionReceived, notification.Type);
        Assert.Contains("Ayesha Rahman", notification.Message);
        Assert.Equal(submission.Id, notification.SubmissionId);
    }

    [Fact]
    public async Task NotifySubmissionGraded_AddressesTheStudentAndCarriesTheScore()
    {
        var assignment = BuildAssignment();
        var submission = new Submission
        {
            Assignment = assignment,
            AssignmentId = assignment.Id,
            StudentId = Guid.NewGuid(),
            Marks = 42
        };

        await _sut.NotifySubmissionGradedAsync(submission, CancellationToken.None);

        var notification = _captured!.Single();
        Assert.Equal(submission.StudentId, notification.UserId);
        Assert.Equal(NotificationType.SubmissionGraded, notification.Type);
        Assert.Contains("42/50", notification.Message);
    }

    [Fact]
    public async Task MarkRead_SetsTheFlagAndTimestamp()
    {
        var userId = Guid.NewGuid();
        var notification = new Notification { UserId = userId, Title = "t", Message = "m" };
        _notificationRepository
            .Setup(r => r.FindByIdAsync(notification.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);

        var result = await _sut.MarkReadAsync(notification.Id, userId, CancellationToken.None);

        Assert.True(result.IsRead);
        Assert.NotNull(result.ReadAt);
    }

    [Fact]
    public async Task MarkRead_IsIdempotent_AndDoesNotMoveTheOriginalTimestamp()
    {
        var userId = Guid.NewGuid();
        var readAt = DateTime.UtcNow.AddDays(-1);
        var notification = new Notification { UserId = userId, Title = "t", Message = "m", IsRead = true, ReadAt = readAt };
        _notificationRepository
            .Setup(r => r.FindByIdAsync(notification.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);

        var result = await _sut.MarkReadAsync(notification.Id, userId, CancellationToken.None);

        Assert.Equal(readAt, result.ReadAt);
        _notificationRepository.Verify(
            r => r.UpdateAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task MarkRead_ReportsNotFound_ForAnotherUsersNotification()
    {
        // Not-found rather than forbidden: whether the row exists is not this caller's business.
        var notification = new Notification { UserId = Guid.NewGuid(), Title = "t", Message = "m" };
        _notificationRepository
            .Setup(r => r.FindByIdAsync(notification.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);

        await Assert.ThrowsAsync<NotFoundAppException>(
            () => _sut.MarkReadAsync(notification.Id, Guid.NewGuid(), CancellationToken.None));

        Assert.False(notification.IsRead);
    }

    [Fact]
    public async Task MarkRead_ReportsNotFound_WhenTheNotificationDoesNotExist()
    {
        _notificationRepository
            .Setup(r => r.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Notification?)null);

        await Assert.ThrowsAsync<NotFoundAppException>(
            () => _sut.MarkReadAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None));
    }
}
