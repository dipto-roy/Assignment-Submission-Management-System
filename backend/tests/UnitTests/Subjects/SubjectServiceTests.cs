using AssignmentSubmissionSystem.Application.Abstractions;
using AssignmentSubmissionSystem.Application.Common.Exceptions;
using AssignmentSubmissionSystem.Application.Subjects;
using AssignmentSubmissionSystem.Application.Subjects.Dtos;
using AssignmentSubmissionSystem.Domain.Entities;
using AssignmentSubmissionSystem.Domain.Enums;
using Moq;

namespace AssignmentSubmissionSystem.UnitTests.Subjects;

public sealed class SubjectServiceTests
{
    private readonly Mock<ISubjectRepository> _subjectRepository = new();
    private readonly Mock<IClassRepository> _classRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly SubjectService _sut;

    public SubjectServiceTests()
    {
        _sut = new SubjectService(_subjectRepository.Object, _classRepository.Object, _userRepository.Object);
    }

    private static Subject BuildSubject(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Name = "Mathematics",
        Code = "MATH101",
        Class = new SchoolClass { Name = "Class 10", Section = "A" }
    };

    [Fact]
    public async Task CreateAsync_Throws404_WhenClassDoesNotExist()
    {
        var classId = Guid.NewGuid();
        var dto = new CreateSubjectDto("Maths", "MATH101", classId);
        _classRepository.Setup(r => r.FindByIdAsync(classId, It.IsAny<CancellationToken>())).ReturnsAsync((SchoolClass?)null);

        var act = () => _sut.CreateAsync(dto, CancellationToken.None);

        await Assert.ThrowsAsync<NotFoundAppException>(act);
    }

    [Fact]
    public async Task AssignTeacherAsync_Throws404_WhenSubjectDoesNotExist()
    {
        var subjectId = Guid.NewGuid();
        _subjectRepository.Setup(r => r.FindByIdAsync(subjectId, It.IsAny<CancellationToken>())).ReturnsAsync((Subject?)null);

        var act = () => _sut.AssignTeacherAsync(subjectId, new AssignTeacherDto(Guid.NewGuid()), CancellationToken.None);

        await Assert.ThrowsAsync<NotFoundAppException>(act);
    }

    [Fact]
    public async Task AssignTeacherAsync_Throws404_WhenTeacherDoesNotExist()
    {
        var subject = BuildSubject();
        _subjectRepository.Setup(r => r.FindByIdAsync(subject.Id, It.IsAny<CancellationToken>())).ReturnsAsync(subject);
        _userRepository.Setup(r => r.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var act = () => _sut.AssignTeacherAsync(subject.Id, new AssignTeacherDto(Guid.NewGuid()), CancellationToken.None);

        await Assert.ThrowsAsync<NotFoundAppException>(act);
    }

    [Fact]
    public async Task AssignTeacherAsync_Throws400_WhenUserIsNotATeacher()
    {
        var subject = BuildSubject();
        var student = new User { Name = "S", Email = "s@lms.test", PasswordHash = "x", Role = UserRole.Student };
        _subjectRepository.Setup(r => r.FindByIdAsync(subject.Id, It.IsAny<CancellationToken>())).ReturnsAsync(subject);
        _userRepository.Setup(r => r.FindByIdAsync(student.Id, It.IsAny<CancellationToken>())).ReturnsAsync(student);

        var act = () => _sut.AssignTeacherAsync(subject.Id, new AssignTeacherDto(student.Id), CancellationToken.None);

        await Assert.ThrowsAsync<BadRequestAppException>(act);
        _subjectRepository.Verify(
            r => r.AssignTeacherAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AssignTeacherAsync_Succeeds_WhenUserIsATeacher()
    {
        var subject = BuildSubject();
        var teacher = new User { Name = "T", Email = "t@lms.test", PasswordHash = "x", Role = UserRole.Teacher };
        _subjectRepository.SetupSequence(r => r.FindByIdAsync(subject.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subject)
            .ReturnsAsync(subject);
        _userRepository.Setup(r => r.FindByIdAsync(teacher.Id, It.IsAny<CancellationToken>())).ReturnsAsync(teacher);

        await _sut.AssignTeacherAsync(subject.Id, new AssignTeacherDto(teacher.Id), CancellationToken.None);

        _subjectRepository.Verify(r => r.AssignTeacherAsync(subject.Id, teacher.Id, It.IsAny<CancellationToken>()), Times.Once);
    }
}
