using System.ComponentModel.DataAnnotations;

namespace AssignmentSubmissionSystem.Application.Options;

/// <summary>
/// Fixed-window throttle for <c>POST /auth/login</c>, partitioned per client IP so one
/// attacker cannot lock every other user out. Configured under <c>RateLimiting:Login</c>.
/// </summary>
public sealed class LoginRateLimitOptions
{
    public const string SectionName = "RateLimiting:Login";

    /// <summary>Requests allowed per window, per IP.</summary>
    [Range(1, int.MaxValue)]
    public int PermitLimit { get; set; } = 10;

    [Range(1, 3600)]
    public int WindowSeconds { get; set; } = 60;
}
