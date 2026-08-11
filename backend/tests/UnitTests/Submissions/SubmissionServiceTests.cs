using AssignmentSubmissionSystem.Application.Abstractions;
using AssignmentSubmissionSystem.Application.Common.Exceptions;
using AssignmentSubmissionSystem.Application.Notifications;
using AssignmentSubmissionSystem.Application.Submissions;
using AssignmentSubmissionSystem.Application.Submissions.Dtos;
using AssignmentSubmissionSystem.Domain.Entities;
using AssignmentSubmissionSystem.Domain.Enums;
using Moq;
using static AssignmentSubmissionSystem.UnitTests.TestPaging;

namespace AssignmentSubmissionSystem.UnitTests.Submissions;

public sealed class SubmissionServiceTests
{
    private readonly Mock<ISubmissionRepository> _submissionRepository = new();
    private readonly Mock<IAssignmentRepository> _assignmentRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<INotificationService> _notificationService = new();
    private readonly SubmissionService _sut;

    public SubmissionServiceTests()
    {
        _sut = new SubmissionService(
            _submissionRepository.Object,
            _assignmentRepository.Object,
            _userRepository.Object,
            _notificationService.Object);
    }

    private static Assignment BuildAssignment(AssignmentStatus status, DateTime deadline, Guid? teacherId = null, int maxMarks = 100)
    {
        var schoolClass = new SchoolClass { Name = "Class 10", Section = "A" };
        var subject = new Subject { Name = "Mathematics", Code = "MATH101", Class = schoolClass, ClassId = schoolClass.Id };
        return new Assignment
        {
            Title = "Algebra",
            Description = "Solve problems",
            Deadline = deadline,
            MaxMarks = maxMarks,
            Status = status,
            Subject = subject,
            SubjectId = subject.Id,
            TeacherId = teacherId ?? Guid.NewGuid()
        };
    }

    private static Submission BuildSubmission(Guid studentId, Assignment assignment) => new()
    {
        AssignmentId = assignment.Id,
        Assignment = assignment,
        StudentId = studentId,
        Student = new User { Id = studentId, Name = "Sample Student", Email = "student@lms.test", Role = UserRole.Student },
        Content = "My work",
        Status = SubmissionStatus.Submitted
    };

    [Fact]
    public async Task SubmitAsync_Throws404_WhenAssignmentDoesNotExist()
    {
        var assignmentId = Guid.NewGuid();
        _assignmentRepository.Setup(r => r.FindByIdAsync(assignmentId, It.IsAny<CancellationToken>())).ReturnsAsync((Assignment?)null);

        var act = () => _sut.SubmitAsync(assignmentId, Guid.NewGuid(), new CreateSubmissionDto("work"), CancellationToken.None);

        await Assert.ThrowsAsync<NotFoundAppException>(act);
    }

    [Fact]
    public async Task SubmitAsync_Throws403_WhenAssignmentIsDraft()
    {
        var assignment = BuildAssignment(AssignmentStatus.Draft, DateTime.UtcNow.AddDays(1));
        _assignmentRepository.Setup(r => r.FindByIdAsync(assignment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(assignment);

        var act = () => _sut.SubmitAsync(assignment.Id, Guid.NewGuid(), new CreateSubmissionDto("work"), CancellationToken.None);

        await Assert.ThrowsAsync<ForbiddenAppException>(act);
    }

    [Fact]
    public async Task SubmitAsync_Throws403_WhenStudentNotEnrolledInClass()
    {
        var assignment = BuildAssignment(AssignmentStatus.Published, DateTime.UtcNow.AddDays(1));
        var studentId = Guid.NewGuid();
        _assignmentRepository.Setup(r => r.FindByIdAsync(assignment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(assignment);
        _assignmentRepository.Setup(r => r.IsStudentEnrolledInClassAsync(studentId, assignment.Subject.ClassId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var act = () => _sut.SubmitAsync(assignment.Id, studentId, new CreateSubmissionDto("work"), CancellationToken.None);

        await Assert.ThrowsAsync<ForbiddenAppException>(act);
    }

    [Fact]
    public async Task SubmitAsync_Throws400_WhenDeadlineHasPassed()
    {
        var assignment = BuildAssignment(AssignmentStatus.Published, DateTime.UtcNow.AddDays(-1));
        var studentId = Guid.NewGuid();
        _assignmentRepository.Setup(r => r.FindByIdAsync(assignment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(assignment);
        _assignmentRepository.Setup(r => r.IsStudentEnrolledInClassAsync(studentId, assignment.Subject.ClassId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = () => _sut.SubmitAsync(assignment.Id, studentId, new CreateSubmissionDto("work"), CancellationToken.None);

        await Assert.ThrowsAsync<BadRequestAppException>(act);
        _submissionRepository.Verify(r => r.AddAsync(It.IsAny<Submission>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubmitAsync_Throws409_WhenAlreadySubmitted()
    {
        var assignment = BuildAssignment(AssignmentStatus.Published, DateTime.UtcNow.AddDays(1));
        var studentId = Guid.NewGuid();
        _assignmentRepository.Setup(r => r.FindByIdAsync(assignment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(assignment);
        _assignmentRepository.Setup(r => r.IsStudentEnrolledInClassAsync(studentId, assignment.Subject.ClassId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _submissionRepository.Setup(r => r.FindByAssignmentAndStudentAsync(assignment.Id, studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildSubmission(studentId, assignment));

        var act = () => _sut.SubmitAsync(assignment.Id, studentId, new CreateSubmissionDto("work"), CancellationToken.None);

        await Assert.ThrowsAsync<ConflictAppException>(act);
        _submissionRepository.Verify(r => r.AddAsync(It.IsAny<Submission>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubmitAsync_Succeeds_WhenPublishedEnrolledAndBeforeDeadline()
    {
        var assignment = BuildAssignment(AssignmentStatus.Published, DateTime.UtcNow.AddDays(1));
        var studentId = Guid.NewGuid();
        _assignmentRepository.Setup(r => r.FindByIdAsync(assignment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(assignment);
        _assignmentRepository.Setup(r => r.IsStudentEnrolledInClassAsync(studentId, assignment.Subject.ClassId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _submissionRepository.Setup(r => r.FindByAssignmentAndStudentAsync(assignment.Id, studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Submission?)null);

        var result = await _sut.SubmitAsync(assignment.Id, studentId, new CreateSubmissionDto("work"), CancellationToken.None);

        result.Content.Should().Be("work");
        result.Status.Should().Be(nameof(SubmissionStatus.Submitted));
        _submissionRepository.Verify(
            r => r.AddAsync(It.Is<Submission>(s => s.StudentId == studentId && s.AssignmentId == assignment.Id), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_Throws404_WhenSubmissionDoesNotExist()
    {
        var id = Guid.NewGuid();
        _submissionRepository.Setup(r => r.FindByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Submission?)null);

        var act = () => _sut.UpdateAsync(id, Guid.NewGuid(), new UpdateSubmissionDto("edited"), CancellationToken.None);

        await Assert.ThrowsAsync<NotFoundAppException>(act);
    }

    [Fact]
    public async Task UpdateAsync_Throws403_WhenStudentDoesNotOwnSubmission()
    {
        var assignment = BuildAssignment(AssignmentStatus.Published, DateTime.UtcNow.AddDays(1));
        var owner = Guid.NewGuid();
        var otherStudent = Guid.NewGuid();
        var submission = BuildSubmission(owner, assignment);
        _submissionRepository.Setup(r => r.FindByIdAsync(submission.Id, It.IsAny<CancellationToken>())).ReturnsAsync(submission);

        var act = () => _sut.UpdateAsync(submission.Id, otherStudent, new UpdateSubmissionDto("hijacked"), CancellationToken.None);

        await Assert.ThrowsAsync<ForbiddenAppException>(act);
    }

    [Fact]
    public async Task UpdateAsync_Throws400_WhenDeadlineHasPassed()
    {
        var assignment = BuildAssignment(AssignmentStatus.Published, DateTime.UtcNow.AddDays(-1));
        var studentId = Guid.NewGuid();
        var submission = BuildSubmission(studentId, assignment);
        _submissionRepository.Setup(r => r.FindByIdAsync(submission.Id, It.IsAny<CancellationToken>())).ReturnsAsync(submission);

        var act = () => _sut.UpdateAsync(submission.Id, studentId, new UpdateSubmissionDto("edited"), CancellationToken.None);

        await Assert.ThrowsAsync<BadRequestAppException>(act);
    }

    [Fact]
    public async Task UpdateAsync_Succeeds_WhenOwnerUpdatesBeforeDeadline()
    {
        var assignment = BuildAssignment(AssignmentStatus.Published, DateTime.UtcNow.AddDays(1));
        var studentId = Guid.NewGuid();
        var submission = BuildSubmission(studentId, assignment);
        _submissionRepository.Setup(r => r.FindByIdAsync(submission.Id, It.IsAny<CancellationToken>())).ReturnsAsync(submission);

        var result = await _sut.UpdateAsync(submission.Id, studentId, new UpdateSubmissionDto("edited"), CancellationToken.None);

        result.Content.Should().Be("edited");
        _submissionRepository.Verify(r => r.UpdateAsync(submission, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMineAsync_ReturnsOnlyStudentsOwnSubmissions()
    {
        var studentId = Guid.NewGuid();
        var assignment = BuildAssignment(AssignmentStatus.Published, DateTime.UtcNow.AddDays(1));
        _submissionRepository.Setup(r => r.FindByStudentAsync(studentId, It.IsAny<SubmissionQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Page(BuildSubmission(studentId, assignment)));

        var result = await _sut.GetMineAsync(studentId, new SubmissionQuery(), CancellationToken.None);

        result.Items.Should().ContainSingle(s => s.StudentId == studentId);
        _submissionRepository.Verify(
            r => r.FindByStudentAsync(studentId, It.IsAny<SubmissionQuery>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ---- Phase 6: grading, feedback, status ----

    [Fact]
    public async Task GetForAssignmentAsync_Throws404_WhenAssignmentDoesNotExist()
    {
        var assignmentId = Guid.NewGuid();
        _assignmentRepository.Setup(r => r.FindByIdAsync(assignmentId, It.IsAny<CancellationToken>())).ReturnsAsync((Assignment?)null);

        var act = () => _sut.GetForAssignmentAsync(assignmentId, Guid.NewGuid(), UserRole.Teacher, new SubmissionQuery(), CancellationToken.None);

        await Assert.ThrowsAsync<NotFoundAppException>(act);
    }

    [Fact]
    public async Task GetForAssignmentAsync_Throws403_ForTeacherWhoDoesNotOwnTheAssignment()
    {
        var assignment = BuildAssignment(AssignmentStatus.Published, DateTime.UtcNow.AddDays(1));
        _assignmentRepository.Setup(r => r.FindByIdAsync(assignment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(assignment);

        var act = () => _sut.GetForAssignmentAsync(assignment.Id, Guid.NewGuid(), UserRole.Teacher, new SubmissionQuery(), CancellationToken.None);

        await Assert.ThrowsAsync<ForbiddenAppException>(act);
        _submissionRepository.Verify(
            r => r.FindByAssignmentAsync(It.IsAny<Guid>(), It.IsAny<SubmissionQuery>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetForAssignmentAsync_Throws403_ForStudent()
    {
        var studentId = Guid.NewGuid();
        var assignment = BuildAssignment(AssignmentStatus.Published, DateTime.UtcNow.AddDays(1));
        _assignmentRepository.Setup(r => r.FindByIdAsync(assignment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(assignment);

        var act = () => _sut.GetForAssignmentAsync(assignment.Id, studentId, UserRole.Student, new SubmissionQuery(), CancellationToken.None);

        await Assert.ThrowsAsync<ForbiddenAppException>(act);
    }

    [Fact]
    public async Task GetForAssignmentAsync_ReturnsSubmissions_ForOwningTeacher()
    {
        var teacherId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var assignment = BuildAssignment(AssignmentStatus.Published, DateTime.UtcNow.AddDays(1), teacherId);
        _assignmentRepository.Setup(r => r.FindByIdAsync(assignment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(assignment);
        _submissionRepository.Setup(r => r.FindByAssignmentAsync(assignment.Id, It.IsAny<SubmissionQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Page(BuildSubmission(studentId, assignment)));

        var result = await _sut.GetForAssignmentAsync(assignment.Id, teacherId, UserRole.Teacher, new SubmissionQuery(), CancellationToken.None);

        result.Items.Should().ContainSingle(s => s.StudentId == studentId && s.StudentName == "Sample Student");
    }

    [Fact]
    public async Task GetForAssignmentAsync_ReturnsSubmissions_ForAdminOnAnyAssignment()
    {
        var assignment = BuildAssignment(AssignmentStatus.Published, DateTime.UtcNow.AddDays(1));
        _assignmentRepository.Setup(r => r.FindByIdAsync(assignment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(assignment);
        _submissionRepository.Setup(r => r.FindByAssignmentAsync(assignment.Id, It.IsAny<SubmissionQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Page(BuildSubmission(Guid.NewGuid(), assignment)));

        var result = await _sut.GetForAssignmentAsync(assignment.Id, Guid.NewGuid(), UserRole.Admin, new SubmissionQuery(), CancellationToken.None);

        result.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task GradeAsync_Throws404_WhenSubmissionDoesNotExist()
    {
        var id = Guid.NewGuid();
        _submissionRepository.Setup(r => r.FindByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Submission?)null);

        var act = () => _sut.GradeAsync(id, Guid.NewGuid(), new GradeSubmissionDto(50, "ok"), CancellationToken.None);

        await Assert.ThrowsAsync<NotFoundAppException>(act);
    }

    [Fact]
    public async Task GradeAsync_Throws403_WhenTeacherDoesNotOwnTheAssignment()
    {
        var assignment = BuildAssignment(AssignmentStatus.Published, DateTime.UtcNow.AddDays(1));
        var submission = BuildSubmission(Guid.NewGuid(), assignment);
        _submissionRepository.Setup(r => r.FindByIdAsync(submission.Id, It.IsAny<CancellationToken>())).ReturnsAsync(submission);

        var act = () => _sut.GradeAsync(submission.Id, Guid.NewGuid(), new GradeSubmissionDto(50, "ok"), CancellationToken.None);

        await Assert.ThrowsAsync<ForbiddenAppException>(act);
        _submissionRepository.Verify(r => r.UpdateAsync(It.IsAny<Submission>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GradeAsync_Throws400_WhenMarksExceedAssignmentMaxMarks()
    {
        var teacherId = Guid.NewGuid();
        var assignment = BuildAssignment(AssignmentStatus.Published, DateTime.UtcNow.AddDays(1), teacherId, maxMarks: 50);
        var submission = BuildSubmission(Guid.NewGuid(), assignment);
        _submissionRepository.Setup(r => r.FindByIdAsync(submission.Id, It.IsAny<CancellationToken>())).ReturnsAsync(submission);

        var act = () => _sut.GradeAsync(submission.Id, teacherId, new GradeSubmissionDto(51, "too generous"), CancellationToken.None);

        await Assert.ThrowsAsync<BadRequestAppException>(act);
        _submissionRepository.Verify(r => r.UpdateAsync(It.IsAny<Submission>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GradeAsync_RecordsMarksFeedbackAndStatus_ForOwningTeacher()
    {
        var teacherId = Guid.NewGuid();
        var assignment = BuildAssignment(AssignmentStatus.Published, DateTime.UtcNow.AddDays(1), teacherId, maxMarks: 50);
        var submission = BuildSubmission(Guid.NewGuid(), assignment);
        _submissionRepository.Setup(r => r.FindByIdAsync(submission.Id, It.IsAny<CancellationToken>())).ReturnsAsync(submission);

        var result = await _sut.GradeAsync(submission.Id, teacherId, new GradeSubmissionDto(50, "Excellent work"), CancellationToken.None);

        result.Marks.Should().Be(50);
        result.Feedback.Should().Be("Excellent work");
        result.Status.Should().Be(nameof(SubmissionStatus.Graded));
        result.GradedAt.Should().NotBeNull();
        _submissionRepository.Verify(r => r.UpdateAsync(submission, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetStatusAsync_Throws403_WhenTeacherDoesNotOwnTheAssignment()
    {
        var assignment = BuildAssignment(AssignmentStatus.Published, DateTime.UtcNow.AddDays(1));
        var submission = BuildSubmission(Guid.NewGuid(), assignment);
        _submissionRepository.Setup(r => r.FindByIdAsync(submission.Id, It.IsAny<CancellationToken>())).ReturnsAsync(submission);

        var act = () => _sut.SetStatusAsync(submission.Id, Guid.NewGuid(), new SetSubmissionStatusDto(SubmissionStatus.Late), CancellationToken.None);

        await Assert.ThrowsAsync<ForbiddenAppException>(act);
    }

    [Fact]
    public async Task SetStatusAsync_Throws400_WhenMarkingReturnedBeforeGrading()
    {
        var teacherId = Guid.NewGuid();
        var assignment = BuildAssignment(AssignmentStatus.Published, DateTime.UtcNow.AddDays(1), teacherId);
        var submission = BuildSubmission(Guid.NewGuid(), assignment);
        _submissionRepository.Setup(r => r.FindByIdAsync(submission.Id, It.IsAny<CancellationToken>())).ReturnsAsync(submission);

        var act = () => _sut.SetStatusAsync(submission.Id, teacherId, new SetSubmissionStatusDto(SubmissionStatus.Returned), CancellationToken.None);

        await Assert.ThrowsAsync<BadRequestAppException>(act);
    }

    [Fact]
    public async Task SetStatusAsync_Succeeds_ForOwningTeacher()
    {
        var teacherId = Guid.NewGuid();
        var assignment = BuildAssignment(AssignmentStatus.Published, DateTime.UtcNow.AddDays(1), teacherId);
        var submission = BuildSubmission(Guid.NewGuid(), assignment);
        _submissionRepository.Setup(r => r.FindByIdAsync(submission.Id, It.IsAny<CancellationToken>())).ReturnsAsync(submission);

        var result = await _sut.SetStatusAsync(submission.Id, teacherId, new SetSubmissionStatusDto(SubmissionStatus.Late), CancellationToken.None);

        result.Status.Should().Be(nameof(SubmissionStatus.Late));
        _submissionRepository.Verify(r => r.UpdateAsync(submission, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetStatusAsync_AllowsReturned_AfterGrading()
    {
        var teacherId = Guid.NewGuid();
        var assignment = BuildAssignment(AssignmentStatus.Published, DateTime.UtcNow.AddDays(1), teacherId);
        var submission = BuildSubmission(Guid.NewGuid(), assignment);
        submission.Marks = 40;
        _submissionRepository.Setup(r => r.FindByIdAsync(submission.Id, It.IsAny<CancellationToken>())).ReturnsAsync(submission);

        var result = await _sut.SetStatusAsync(submission.Id, teacherId, new SetSubmissionStatusDto(SubmissionStatus.Returned), CancellationToken.None);

        result.Status.Should().Be(nameof(SubmissionStatus.Returned));
    }
}
