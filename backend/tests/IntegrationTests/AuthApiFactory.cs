using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace AssignmentSubmissionSystem.IntegrationTests;

/// <summary>
/// Boots the real API pipeline (Program.cs) against the docker-compose Postgres instance,
/// regardless of the test runner's content root.
/// Requires `docker compose up postgres` (see backend/docker-compose.yml) to be running.
/// </summary>
/// <remarks>
/// Configuration is read from the environment (and therefore from backend/.env, which
/// Program.cs loads) so tests exercise the same settings as a real run. The fallbacks below
/// mirror the docker-compose defaults, keeping the suite runnable on a clean checkout with
/// no .env present. They are local-development values only.
/// </remarks>
public class AuthApiFactory : WebApplicationFactory<Program>
{
    private const string FallbackConnectionString =
        "Host=localhost;Port=5434;Database=assignment_submission_dev;Username=postgres;Password=postgres_dev_only";

    private const string FallbackJwtKey =
        "local-dev-only-jwt-signing-key-not-for-production-use-32chars";

    /// <summary>
    /// Upload target for the suite. Unique per run so parallel or repeated runs cannot see
    /// each other's files, and so nothing is left behind in the repository tree.
    /// </summary>
    private static readonly string LocalStorageRoot =
        Path.Combine(Path.GetTempPath(), $"assignment-submission-tests-{Guid.NewGuid():N}");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var existing = context.Configuration;

            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // The suite logs in on nearly every test from one loopback address, which is a
                // single rate-limit partition. The production default (10/minute) would start
                // returning 429 partway through the run, so it is raised here. The limiter
                // itself is covered by RateLimitEndpointsTests, which pins a low limit.
                ["RateLimiting:Login:PermitLimit"] = "100000",
                ["ConnectionStrings:Default"] =
                    Coalesce(existing.GetConnectionString("Default"), FallbackConnectionString),
                ["Jwt:Key"] = Coalesce(existing["Jwt:Key"], FallbackJwtKey),
                ["Jwt:Issuer"] = Coalesce(existing["Jwt:Issuer"], "AssignmentSubmissionSystem"),
                ["Jwt:Audience"] = Coalesce(existing["Jwt:Audience"], "AssignmentSubmissionSystem.Client"),
                ["Jwt:ExpiryMinutes"] = Coalesce(existing["Jwt:ExpiryMinutes"], "60"),

                // The reminder worker would otherwise scan on boot and insert notifications
                // underneath tests that assert on exact counts, making them order-dependent.
                // Its behaviour is covered directly in DeadlineReminderServiceTests instead.
                ["Notifications:DeadlineReminder:Enabled"] = "false",

                // Local storage in a per-run temp directory: the suite exercises the real
                // upload path end to end without depending on Cloudinary credentials or
                // touching a real account from CI.
                ["Storage:Provider"] = "Local",
                ["Storage:LocalRootPath"] = LocalStorageRoot
            });
        });
    }

    private static string Coalesce(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value;
}
