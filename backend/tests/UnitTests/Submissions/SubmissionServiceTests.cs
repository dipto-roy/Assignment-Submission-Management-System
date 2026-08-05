using AssignmentSubmissionSystem.Application.Abstractions;
using AssignmentSubmissionSystem.Application.Common.Exceptions;
using AssignmentSubmissionSystem.Application.Submissions;
using AssignmentSubmissionSystem.Application.Submissions.Dtos;
using AssignmentSubmissionSystem.Domain.Entities;
using AssignmentSubmissionSystem.Domain.Enums;
using Moq;

namespace AssignmentSubmissionSystem.UnitTests.Submissions;

public sealed class SubmissionServiceTests
{
    private readonly Mock<ISubmissionRepository> _submissionRepository = new();
    private readonly Mock<IAssignmentRepository> _assignmentRepository = new();
    private readonly SubmissionService _sut;

    public SubmissionServiceTests()
    {
        _sut = new SubmissionService(_submissionRepository.Object, _assignmentRepository.Object);
    }

    private static Assignment BuildAssignment(AssignmentStatus status, DateTime deadline)
    {
        var schoolClass = new SchoolClass { Name = "Class 10", Section = "A" };
        var subject = new Subject { Name = "Mathematics", Code = "MATH101", Class = schoolClass, ClassId = schoolClass.Id };
        return new Assignment
        {
            Title = "Algebra",
            Description = "Solve problems",
            Deadline = deadline,
            MaxMarks = 100,
            Status = status,
            Subject = subject,
            SubjectId = subject.Id,
            TeacherId = Guid.NewGuid()
        };
    }

    private static Submission BuildSubmission(Guid studentId, Assignment assignment) => new()
    {
        AssignmentId = assignment.Id,
        Assignment = assignment,
        StudentId = studentId,
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
        _submissionRepository.Setup(r => r.FindByStudentAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Submission> { BuildSubmission(studentId, assignment) });

        var result = await _sut.GetMineAsync(studentId, CancellationToken.None);

        result.Should().ContainSingle(s => s.StudentId == studentId);
        _submissionRepository.Verify(r => r.FindByStudentAsync(studentId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
