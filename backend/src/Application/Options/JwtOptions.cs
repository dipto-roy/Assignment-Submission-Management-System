using System.ComponentModel.DataAnnotations;

namespace AssignmentSubmissionSystem.Application.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Minimum key length in characters, matching the 256-bit requirement of HMAC-SHA256.</summary>
    public const int MinimumKeyLength = 32;

    /// <remarks>
    /// <c>required</c> is not enforced by the configuration binder, so the annotations below are
    /// what actually fail the start-up when the secret is missing or too weak to sign with.
    /// </remarks>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Jwt:Key is not configured.")]
    [MinLength(MinimumKeyLength, ErrorMessage = "Jwt:Key must be at least 32 characters (256 bits) for HMAC-SHA256.")]
    public required string Key { get; init; }

    [Required(AllowEmptyStrings = false, ErrorMessage = "Jwt:Issuer is not configured.")]
    public required string Issuer { get; init; }

    [Required(AllowEmptyStrings = false, ErrorMessage = "Jwt:Audience is not configured.")]
    public required string Audience { get; init; }

    [Range(1, 1440, ErrorMessage = "Jwt:ExpiryMinutes must be between 1 and 1440.")]
    public int ExpiryMinutes { get; init; } = 60;
}
