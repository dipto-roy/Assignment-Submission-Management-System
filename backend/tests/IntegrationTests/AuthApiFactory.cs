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
public sealed class AuthApiFactory : WebApplicationFactory<Program>
{
    private const string FallbackConnectionString =
        "Host=localhost;Port=5434;Database=assignment_submission_dev;Username=postgres;Password=postgres_dev_only";

    private const string FallbackJwtKey =
        "local-dev-only-jwt-signing-key-not-for-production-use-32chars";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var existing = context.Configuration;

            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] =
                    Coalesce(existing.GetConnectionString("Default"), FallbackConnectionString),
                ["Jwt:Key"] = Coalesce(existing["Jwt:Key"], FallbackJwtKey),
                ["Jwt:Issuer"] = Coalesce(existing["Jwt:Issuer"], "AssignmentSubmissionSystem"),
                ["Jwt:Audience"] = Coalesce(existing["Jwt:Audience"], "AssignmentSubmissionSystem.Client"),
                ["Jwt:ExpiryMinutes"] = Coalesce(existing["Jwt:ExpiryMinutes"], "60")
            });
        });
    }

    private static string Coalesce(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value;
}
