using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AssignmentSubmissionSystem.Application.Options;
using AssignmentSubmissionSystem.Domain.Entities;
using AssignmentSubmissionSystem.Domain.Enums;
using AssignmentSubmissionSystem.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace AssignmentSubmissionSystem.UnitTests.Security;

public sealed class JwtTokenServiceTests
{
    private readonly JwtTokenService _sut;
    private readonly JwtOptions _options = new()
    {
        Key = "unit-test-signing-key-at-least-32-characters-long",
        Issuer = "TestIssuer",
        Audience = "TestAudience",
        ExpiryMinutes = 15
    };

    public JwtTokenServiceTests()
    {
        _sut = new JwtTokenService(Options.Create(_options));
    }

    [Fact]
    public void GenerateToken_EmbedsUserIdEmailAndRoleClaims()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Jane Teacher",
            Email = "teacher@lms.test",
            PasswordHash = "irrelevant",
            Role = UserRole.Teacher
        };

        // Act
        var result = _sut.GenerateToken(user);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Value);

        // Assert
        token.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id.ToString());
        token.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == user.Email);
        token.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == nameof(UserRole.Teacher));
        token.Issuer.Should().Be(_options.Issuer);
        token.Audiences.Should().Contain(_options.Audience);
    }

    [Fact]
    public void GenerateToken_SetsExpiryAccordingToConfiguredMinutes()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid(), Name = "N", Email = "e@lms.test", PasswordHash = "x", Role = UserRole.Student };
        var before = DateTime.UtcNow;

        // Act
        var result = _sut.GenerateToken(user);

        // Assert
        result.ExpiresAtUtc.Should().BeCloseTo(before.AddMinutes(_options.ExpiryMinutes), TimeSpan.FromSeconds(5));
    }
}
