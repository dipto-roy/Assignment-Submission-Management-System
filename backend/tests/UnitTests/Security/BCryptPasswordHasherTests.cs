using AssignmentSubmissionSystem.Infrastructure.Security;

namespace AssignmentSubmissionSystem.UnitTests.Security;

public sealed class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _sut = new();

    [Fact]
    public void Verify_ReturnsTrue_ForMatchingPassword()
    {
        var hash = _sut.Hash("Correct@Password1");

        _sut.Verify("Correct@Password1", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_ReturnsFalse_ForWrongPassword()
    {
        var hash = _sut.Hash("Correct@Password1");

        _sut.Verify("Wrong@Password1", hash).Should().BeFalse();
    }

    [Fact]
    public void Hash_ProducesDifferentOutput_ForSamePasswordEachTime()
    {
        // BCrypt salts each hash uniquely — same input must not produce identical output.
        var first = _sut.Hash("Repeatable@Password1");
        var second = _sut.Hash("Repeatable@Password1");

        first.Should().NotBe(second);
    }
}
