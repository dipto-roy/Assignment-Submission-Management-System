using AssignmentSubmissionSystem.Application.Abstractions;
using AssignmentSubmissionSystem.Application.Classes;
using AssignmentSubmissionSystem.Application.Classes.Dtos;
using AssignmentSubmissionSystem.Application.Common.Exceptions;
using AssignmentSubmissionSystem.Domain.Entities;
using AssignmentSubmissionSystem.Domain.Enums;
using Moq;

namespace AssignmentSubmissionSystem.UnitTests.Classes;

public sealed class ClassServiceTests
{
    private readonly Mock<IClassRepository> _classRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly ClassService _sut;

    public ClassServiceTests()
    {
        _sut = new ClassService(_classRepository.Object, _userRepository.Object);
    }

    [Fact]
    public async Task CreateAsync_AddsClassWithGivenNameAndSection()
    {
        var dto = new CreateClassDto("Class 10", "A");

        var result = await _sut.CreateAsync(dto, CancellationToken.None);

        result.Name.Should().Be("Class 10");
        result.Section.Should().Be("A");
        _classRepository.Verify(r => r.AddAsync(It.IsAny<SchoolClass>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_Throws404_WhenClassDoesNotExist()
    {
        var id = Guid.NewGuid();
        _classRepository.Setup(r => r.FindByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((SchoolClass?)null);

        var act = () => _sut.UpdateAsync(id, new UpdateClassDto("X", null), CancellationToken.None);

        await Assert.ThrowsAsync<NotFoundAppException>(act);
    }

    [Fact]
    public async Task EnrollStudentAsync_Throws404_WhenClassDoesNotExist()
    {
        var classId = Guid.NewGuid();
        _classRepository.Setup(r => r.FindByIdAsync(classId, It.IsAny<CancellationToken>())).ReturnsAsync((SchoolClass?)null);

        var act = () => _sut.EnrollStudentAsync(classId, new EnrollStudentDto(Guid.NewGuid()), CancellationToken.None);

        await Assert.ThrowsAsync<NotFoundAppException>(act);
    }

    [Fact]
    public async Task EnrollStudentAsync_Throws400_WhenUserIsNotAStudent()
    {
        var schoolClass = GivenClass();
        var teacher = new User { Id = Guid.NewGuid(), Name = "Tess", Email = "t@lms.test", Role = UserRole.Teacher };
        _userRepository.Setup(r => r.FindByIdAsync(teacher.Id, It.IsAny<CancellationToken>())).ReturnsAsync(teacher);

        var act = () => _sut.EnrollStudentAsync(schoolClass.Id, new EnrollStudentDto(teacher.Id), CancellationToken.None);

        await Assert.ThrowsAsync<BadRequestAppException>(act);
    }

    [Fact]
    public async Task EnrollStudentAsync_Throws409_WhenAlreadyEnrolledInThatClass()
    {
        var schoolClass = GivenClass();
        var student = GivenStudent();
        _classRepository
            .Setup(r => r.FindEnrollmentsByStudentAsync(student.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StudentClass> { new() { ClassId = schoolClass.Id, StudentId = student.Id } });

        var act = () => _sut.EnrollStudentAsync(schoolClass.Id, new EnrollStudentDto(student.Id), CancellationToken.None);

        await Assert.ThrowsAsync<ConflictAppException>(act);
    }

    [Fact]
    public async Task EnrollStudentAsync_MovesStudent_WhenAlreadyEnrolledElsewhere()
    {
        var schoolClass = GivenClass();
        var student = GivenStudent();
        _classRepository
            .Setup(r => r.FindEnrollmentsByStudentAsync(student.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StudentClass> { new() { ClassId = Guid.NewGuid(), StudentId = student.Id } });

        var result = await _sut.EnrollStudentAsync(schoolClass.Id, new EnrollStudentDto(student.Id), CancellationToken.None);

        result.Id.Should().Be(student.Id);
        _classRepository.Verify(
            r => r.EnrollStudentAsync(schoolClass.Id, student.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UnenrollStudentAsync_Throws404_WhenStudentIsNotInThatClass()
    {
        var schoolClass = GivenClass();
        var studentId = Guid.NewGuid();
        _classRepository
            .Setup(r => r.FindEnrollmentsByStudentAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StudentClass>());

        var act = () => _sut.UnenrollStudentAsync(schoolClass.Id, studentId, CancellationToken.None);

        await Assert.ThrowsAsync<NotFoundAppException>(act);
    }

    [Fact]
    public async Task UnenrollStudentAsync_RemovesTheEnrollmentRow()
    {
        var schoolClass = GivenClass();
        var studentId = Guid.NewGuid();
        var enrollment = new StudentClass { ClassId = schoolClass.Id, StudentId = studentId };
        _classRepository
            .Setup(r => r.FindEnrollmentsByStudentAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StudentClass> { enrollment });

        await _sut.UnenrollStudentAsync(schoolClass.Id, studentId, CancellationToken.None);

        _classRepository.Verify(r => r.RemoveEnrollmentAsync(enrollment, It.IsAny<CancellationToken>()), Times.Once);
    }

    private SchoolClass GivenClass()
    {
        var schoolClass = new SchoolClass { Id = Guid.NewGuid(), Name = "Class 10", Section = "A" };
        _classRepository
            .Setup(r => r.FindByIdAsync(schoolClass.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(schoolClass);
        return schoolClass;
    }

    private User GivenStudent()
    {
        var student = new User { Id = Guid.NewGuid(), Name = "Sam", Email = "s@lms.test", Role = UserRole.Student };
        _userRepository.Setup(r => r.FindByIdAsync(student.Id, It.IsAny<CancellationToken>())).ReturnsAsync(student);
        return student;
    }
}
