using AssignmentSubmissionSystem.Domain.Entities;

namespace AssignmentSubmissionSystem.Application.Abstractions;

public interface ITokenService
{
    GeneratedToken GenerateToken(User user);
}

public sealed record GeneratedToken(string Value, DateTime ExpiresAtUtc);
