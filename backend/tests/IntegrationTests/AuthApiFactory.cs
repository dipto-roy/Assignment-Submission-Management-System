using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace AssignmentSubmissionSystem.IntegrationTests;

/// <summary>
/// Boots the real API pipeline (Program.cs) against the docker-compose Postgres instance
/// on the Development connection string, regardless of the test runner's content root.
/// Requires `docker compose up postgres` (see backend/docker-compose.yml) to be running.
/// </summary>
public sealed class AuthApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] =
                    "Host=localhost;Port=5434;Database=assignment_submission_dev;Username=postgres;Password=postgres_dev_only",
                ["Jwt:Key"] = "local-dev-only-jwt-signing-key-not-for-production-use-32chars",
                ["Jwt:Issuer"] = "AssignmentSubmissionSystem",
                ["Jwt:Audience"] = "AssignmentSubmissionSystem.Client",
                ["Jwt:ExpiryMinutes"] = "60"
            });
        });
    }
}
