using AssignmentSubmissionSystem.Application.Abstractions;
using AssignmentSubmissionSystem.Application.Auth;
using AssignmentSubmissionSystem.Application.Auth.Dtos;
using AssignmentSubmissionSystem.Application.Common.Exceptions;
using AssignmentSubmissionSystem.Domain.Entities;
using AssignmentSubmissionSystem.Domain.Enums;
using Moq;

namespace AssignmentSubmissionSystem.UnitTests.Auth;

public sealed class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(_userRepository.Object, _passwordHasher.Object, _tokenService.Object);
    }

    private static User BuildUser(UserRole role = UserRole.Student) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Test User",
        Email = "user@lms.test",
        PasswordHash = "hashed-password",
        Role = role
    };

    [Fact]
    public async Task LoginAsync_ReturnsToken_WhenCredentialsAreValid()
    {
        // Arrange
        var user = BuildUser(UserRole.Teacher);
        var request = new LoginRequestDto(user.Email, "correct-password");
        var expiresAt = DateTime.UtcNow.AddHours(1);

        _userRepository.Setup(r => r.FindByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify(request.Password, user.PasswordHash)).Returns(true);
        _tokenService.Setup(t => t.GenerateToken(user)).Returns(new GeneratedToken("jwt-token", expiresAt));

        // Act
        var result = await _sut.LoginAsync(request, CancellationToken.None);

        // Assert
        result.Token.Should().Be("jwt-token");
        result.ExpiresAtUtc.Should().Be(expiresAt);
        result.User.Id.Should().Be(user.Id);
        result.User.Role.Should().Be(nameof(UserRole.Teacher));
    }

    [Fact]
    public async Task LoginAsync_Throws401_WhenUserDoesNotExist()
    {
        // Arrange
        var request = new LoginRequestDto("missing@lms.test", "whatever");
        _userRepository.Setup(r => r.FindByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var act = () => _sut.LoginAsync(request, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<UnauthorizedAppException>();
        ex.Which.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task LoginAsync_Throws401_WhenPasswordIsWrong()
    {
        // Arrange
        var user = BuildUser();
        var request = new LoginRequestDto(user.Email, "wrong-password");

        _userRepository.Setup(r => r.FindByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify(request.Password, user.PasswordHash)).Returns(false);

        // Act
        var act = () => _sut.LoginAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAppException>();
        _tokenService.Verify(t => t.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ErrorMessage_DoesNotRevealWhetherUserExists()
    {
        // Arrange — same generic message for both failure paths, to prevent user enumeration.
        var missingUserRequest = new LoginRequestDto("missing@lms.test", "whatever");
        _userRepository.Setup(r => r.FindByEmailAsync(missingUserRequest.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var existingUser = BuildUser();
        var wrongPasswordRequest = new LoginRequestDto(existingUser.Email, "wrong-password");
        _userRepository.Setup(r => r.FindByEmailAsync(existingUser.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);
        _passwordHasher.Setup(h => h.Verify(wrongPasswordRequest.Password, existingUser.PasswordHash)).Returns(false);

        // Act
        var missingUserEx = await Assert.ThrowsAsync<UnauthorizedAppException>(
            () => _sut.LoginAsync(missingUserRequest, CancellationToken.None));
        var wrongPasswordEx = await Assert.ThrowsAsync<UnauthorizedAppException>(
            () => _sut.LoginAsync(wrongPasswordRequest, CancellationToken.None));

        // Assert
        missingUserEx.Message.Should().Be(wrongPasswordEx.Message);
    }
}
