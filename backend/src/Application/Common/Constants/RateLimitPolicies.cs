namespace AssignmentSubmissionSystem.Application.Common.Constants;

/// <summary>
/// Names shared between the rate-limiter registration in <c>Program.cs</c> and the
/// <c>[EnableRateLimiting]</c> attributes on controllers.
/// </summary>
public static class RateLimitPolicies
{
    public const string Login = "login";
}
