using AssignmentSubmissionSystem.Application.Abstractions;
using AssignmentSubmissionSystem.Application.Common.Exceptions;
using AssignmentSubmissionSystem.Application.Users;
using AssignmentSubmissionSystem.Application.Users.Dtos;
using AssignmentSubmissionSystem.Domain.Entities;
using AssignmentSubmissionSystem.Domain.Enums;
using Moq;

namespace AssignmentSubmissionSystem.UnitTests.Users;

public sealed class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IClassRepository> _classRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _sut = new UserService(_userRepository.Object, _classRepository.Object, _passwordHasher.Object);
        _passwordHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed");
    }

    [Fact]
    public async Task CreateAsync_Throws409_WhenEmailAlreadyExists()
    {
        var dto = new CreateUserDto("Dup", "dup@lms.test", "Password123", UserRole.Teacher, null);
        _userRepository.Setup(r => r.ExistsByEmailAsync(dto.Email, null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = () => _sut.CreateAsync(dto, CancellationToken.None);

        await Assert.ThrowsAsync<ConflictAppException>(act);
        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_Throws404_WhenStudentClassIdDoesNotExist()
    {
        var classId = Guid.NewGuid();
        var dto = new CreateUserDto("Stu", "stu@lms.test", "Password123", UserRole.Student, classId);
        _userRepository.Setup(r => r.ExistsByEmailAsync(dto.Email, null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _classRepository.Setup(r => r.FindByIdAsync(classId, It.IsAny<CancellationToken>())).ReturnsAsync((SchoolClass?)null);

        var act = () => _sut.CreateAsync(dto, CancellationToken.None);

        await Assert.ThrowsAsync<NotFoundAppException>(act);
    }

    [Fact]
    public async Task CreateAsync_EnrollsStudentInClass_WhenClassExists()
    {
        var schoolClass = new SchoolClass { Name = "Class 10", Section = "A" };
        var dto = new CreateUserDto("Stu", "stu@lms.test", "Password123", UserRole.Student, schoolClass.Id);
        _userRepository.Setup(r => r.ExistsByEmailAsync(dto.Email, null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _classRepository.Setup(r => r.FindByIdAsync(schoolClass.Id, It.IsAny<CancellationToken>())).ReturnsAsync(schoolClass);

        User? captured = null;
        _userRepository.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => captured = u)
            .Returns(Task.CompletedTask);

        await _sut.CreateAsync(dto, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.StudentClasses.Should().ContainSingle(sc => sc.ClassId == schoolClass.Id);
    }

    [Fact]
    public async Task DeleteAsync_Throws404_WhenUserDoesNotExist()
    {
        var id = Guid.NewGuid();
        _userRepository.Setup(r => r.FindByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var act = () => _sut.DeleteAsync(id, CancellationToken.None);

        await Assert.ThrowsAsync<NotFoundAppException>(act);
    }
}
