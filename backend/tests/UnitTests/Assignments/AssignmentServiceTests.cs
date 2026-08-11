using AssignmentSubmissionSystem.Application.Abstractions;
using AssignmentSubmissionSystem.Application.Assignments;
using AssignmentSubmissionSystem.Application.Assignments.Dtos;
using AssignmentSubmissionSystem.Application.Common.Exceptions;
using AssignmentSubmissionSystem.Application.Notifications;
using AssignmentSubmissionSystem.Domain.Entities;
using AssignmentSubmissionSystem.Domain.Enums;
using Moq;
using static AssignmentSubmissionSystem.UnitTests.TestPaging;

namespace AssignmentSubmissionSystem.UnitTests.Assignments;

public sealed class AssignmentServiceTests
{
    private readonly Mock<IAssignmentRepository> _assignmentRepository = new();
    private readonly Mock<INotificationService> _notificationService = new();
    private readonly AssignmentService _sut;

    public AssignmentServiceTests()
    {
        _sut = new AssignmentService(_assignmentRepository.Object, _notificationService.Object);
    }

    private static Assignment BuildAssignment(Guid teacherId, Guid? id = null, AssignmentStatus status = AssignmentStatus.Draft)
    {
        var schoolClass = new SchoolClass { Name = "Class 10", Section = "A" };
        var subject = new Subject { Name = "Mathematics", Code = "MATH101", Class = schoolClass, ClassId = schoolClass.Id };
        return new Assignment
        {
            Id = id ?? Guid.NewGuid(),
            Title = "Algebra",
            Description = "Solve problems",
            Deadline = DateTime.UtcNow.AddDays(7),
            MaxMarks = 100,
            Status = status,
            Subject = subject,
            SubjectId = subject.Id,
            TeacherId = teacherId,
            Teacher = new User { Name = "T", Email = "t@lms.test", PasswordHash = "x", Role = UserRole.Teacher, Id = teacherId }
        };
    }

    [Fact]
    public async Task CreateAsync_Throws403_WhenTeacherNotAssignedToSubject()
    {
        var teacherId = Guid.NewGuid();
        var dto = new CreateAssignmentDto("Algebra", "Desc", DateTime.UtcNow.AddDays(1), 100, Guid.NewGuid());
        _assignmentRepository.Setup(r => r.IsTeacherAssignedToSubjectAsync(teacherId, dto.SubjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var act = () => _sut.CreateAsync(teacherId, dto, CancellationToken.None);

        await Assert.ThrowsAsync<ForbiddenAppException>(act);
        _assignmentRepository.Verify(r => r.AddAsync(It.IsAny<Assignment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_Succeeds_WhenTeacherIsAssignedToSubject()
    {
        var teacherId = Guid.NewGuid();
        var dto = new CreateAssignmentDto("Algebra", "Desc", DateTime.UtcNow.AddDays(1), 100, Guid.NewGuid());
        _assignmentRepository.Setup(r => r.IsTeacherAssignedToSubjectAsync(teacherId, dto.SubjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _assignmentRepository.Setup(r => r.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => BuildAssignment(teacherId, id));

        var result = await _sut.CreateAsync(teacherId, dto, CancellationToken.None);

        result.Title.Should().Be("Algebra");
        result.Status.Should().Be(nameof(AssignmentStatus.Draft));
        _assignmentRepository.Verify(r => r.AddAsync(It.Is<Assignment>(a => a.TeacherId == teacherId && a.Status == AssignmentStatus.Draft), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_Throws404_WhenAssignmentDoesNotExist()
    {
        var id = Guid.NewGuid();
        _assignmentRepository.Setup(r => r.FindByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Assignment?)null);

        var act = () => _sut.UpdateAsync(id, Guid.NewGuid(), new UpdateAssignmentDto("T", "D", DateTime.UtcNow.AddDays(1), 50), CancellationToken.None);

        await Assert.ThrowsAsync<NotFoundAppException>(act);
    }

    [Fact]
    public async Task UpdateAsync_Throws403_WhenTeacherDoesNotOwnAssignment()
    {
        var owner = Guid.NewGuid();
        var otherTeacher = Guid.NewGuid();
        var assignment = BuildAssignment(owner);
        _assignmentRepository.Setup(r => r.FindByIdAsync(assignment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(assignment);

        var act = () => _sut.UpdateAsync(assignment.Id, otherTeacher, new UpdateAssignmentDto("T", "D", DateTime.UtcNow.AddDays(1), 50), CancellationToken.None);

        await Assert.ThrowsAsync<ForbiddenAppException>(act);
    }

    [Fact]
    public async Task DeleteAsync_Throws403_WhenTeacherDoesNotOwnAssignment()
    {
        var owner = Guid.NewGuid();
        var otherTeacher = Guid.NewGuid();
        var assignment = BuildAssignment(owner);
        _assignmentRepository.Setup(r => r.FindByIdAsync(assignment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(assignment);

        var act = () => _sut.DeleteAsync(assignment.Id, otherTeacher, CancellationToken.None);

        await Assert.ThrowsAsync<ForbiddenAppException>(act);
        _assignmentRepository.Verify(r => r.DeleteAsync(It.IsAny<Assignment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetPublishStateAsync_SetsStatusToPublished_WhenOwnerPublishes()
    {
        var teacherId = Guid.NewGuid();
        var assignment = BuildAssignment(teacherId, status: AssignmentStatus.Draft);
        _assignmentRepository.Setup(r => r.FindByIdAsync(assignment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(assignment);

        var result = await _sut.SetPublishStateAsync(assignment.Id, teacherId, new SetPublishStateDto(true), CancellationToken.None);

        result.Status.Should().Be(nameof(AssignmentStatus.Published));
    }

    [Fact]
    public async Task SetPublishStateAsync_RevertsToDraft_WhenOwnerUnpublishes()
    {
        var teacherId = Guid.NewGuid();
        var assignment = BuildAssignment(teacherId, status: AssignmentStatus.Published);
        _assignmentRepository.Setup(r => r.FindByIdAsync(assignment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(assignment);

        var result = await _sut.SetPublishStateAsync(assignment.Id, teacherId, new SetPublishStateDto(false), CancellationToken.None);

        result.Status.Should().Be(nameof(AssignmentStatus.Draft));
    }

    [Fact]
    public async Task GetAllAsync_UsesAdminFindAll_ForAdminRole()
    {
        var userId = Guid.NewGuid();
        _assignmentRepository.Setup(r => r.FindAllAsync(It.IsAny<AssignmentQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Page(BuildAssignment(Guid.NewGuid())));

        var result = await _sut.GetAllAsync(userId, UserRole.Admin, new AssignmentQuery(), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Total.Should().Be(1);
        _assignmentRepository.Verify(r => r.FindAllAsync(It.IsAny<AssignmentQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_UsesTeacherOwnAssignments_ForTeacherRole()
    {
        var teacherId = Guid.NewGuid();
        _assignmentRepository.Setup(r => r.FindByTeacherAsync(teacherId, It.IsAny<AssignmentQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Page(BuildAssignment(teacherId)));

        var result = await _sut.GetAllAsync(teacherId, UserRole.Teacher, new AssignmentQuery(), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        _assignmentRepository.Verify(r => r.FindByTeacherAsync(teacherId, It.IsAny<AssignmentQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_UsesPublishedForStudent_ForStudentRole()
    {
        var studentId = Guid.NewGuid();
        _assignmentRepository.Setup(r => r.FindPublishedForStudentAsync(studentId, It.IsAny<AssignmentQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Page<Assignment>());

        var result = await _sut.GetAllAsync(studentId, UserRole.Student, new AssignmentQuery(), CancellationToken.None);

        result.Items.Should().BeEmpty();
        _assignmentRepository.Verify(
            r => r.FindPublishedForStudentAsync(studentId, It.IsAny<AssignmentQuery>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_Throws403_ForStudent_WhenAssignmentIsDraft()
    {
        var studentId = Guid.NewGuid();
        var assignment = BuildAssignment(Guid.NewGuid(), status: AssignmentStatus.Draft);
        _assignmentRepository.Setup(r => r.FindByIdAsync(assignment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(assignment);

        var act = () => _sut.GetByIdAsync(assignment.Id, studentId, UserRole.Student, CancellationToken.None);

        await Assert.ThrowsAsync<ForbiddenAppException>(act);
    }

    [Fact]
    public async Task GetByIdAsync_Throws403_ForStudent_WhenNotEnrolledInClass()
    {
        var studentId = Guid.NewGuid();
        var assignment = BuildAssignment(Guid.NewGuid(), status: AssignmentStatus.Published);
        _assignmentRepository.Setup(r => r.FindByIdAsync(assignment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(assignment);
        _assignmentRepository.Setup(r => r.IsStudentEnrolledInClassAsync(studentId, assignment.Subject.ClassId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var act = () => _sut.GetByIdAsync(assignment.Id, studentId, UserRole.Student, CancellationToken.None);

        await Assert.ThrowsAsync<ForbiddenAppException>(act);
    }

    [Fact]
    public async Task GetByIdAsync_Succeeds_ForStudent_WhenPublishedAndEnrolled()
    {
        var studentId = Guid.NewGuid();
        var assignment = BuildAssignment(Guid.NewGuid(), status: AssignmentStatus.Published);
        _assignmentRepository.Setup(r => r.FindByIdAsync(assignment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(assignment);
        _assignmentRepository.Setup(r => r.IsStudentEnrolledInClassAsync(studentId, assignment.Subject.ClassId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.GetByIdAsync(assignment.Id, studentId, UserRole.Student, CancellationToken.None);

        result.Id.Should().Be(assignment.Id);
    }

    [Fact]
    public async Task GetByIdAsync_Throws403_ForTeacher_WhenNotOwner()
    {
        var owner = Guid.NewGuid();
        var otherTeacher = Guid.NewGuid();
        var assignment = BuildAssignment(owner);
        _assignmentRepository.Setup(r => r.FindByIdAsync(assignment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(assignment);

        var act = () => _sut.GetByIdAsync(assignment.Id, otherTeacher, UserRole.Teacher, CancellationToken.None);

        await Assert.ThrowsAsync<ForbiddenAppException>(act);
    }
}
