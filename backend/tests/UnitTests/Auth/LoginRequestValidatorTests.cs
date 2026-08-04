using AssignmentSubmissionSystem.Application.Auth;
using AssignmentSubmissionSystem.Application.Auth.Dtos;

namespace AssignmentSubmissionSystem.UnitTests.Auth;

public sealed class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _sut = new();

    [Fact]
    public void Validate_Passes_ForValidEmailAndPassword()
    {
        var result = _sut.Validate(new LoginRequestDto("user@lms.test", "Some@Password1"));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "password")]
    [InlineData("not-an-email", "password")]
    [InlineData("user@lms.test", "")]
    public void Validate_Fails_ForInvalidInput(string email, string password)
    {
        var result = _sut.Validate(new LoginRequestDto(email, password));

        result.IsValid.Should().BeFalse();
    }
}
