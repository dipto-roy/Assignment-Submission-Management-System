using System.Net;
using System.Net.Http.Json;
using AssignmentSubmissionSystem.Application.Auth.Dtos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace AssignmentSubmissionSystem.IntegrationTests;

/// <summary>
/// Boots the API with a deliberately tiny login budget so the brute-force guard can be
/// observed without hammering the endpoint hundreds of times.
/// </summary>
public sealed class ThrottledLoginApiFactory : AuthApiFactory
{
    public const int PermitLimit = 3;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        // Applied after the base configuration, so this wins over the high suite-wide limit.
        builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["RateLimiting:Login:PermitLimit"] = PermitLimit.ToString(),
                ["RateLimiting:Login:WindowSeconds"] = "60"
            }));
    }
}

public sealed class RateLimitEndpointsTests : IClassFixture<ThrottledLoginApiFactory>
{
    private readonly ThrottledLoginApiFactory _factory;

    public RateLimitEndpointsTests(ThrottledLoginApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_Returns429_AfterPermitLimitIsExhausted()
    {
        var client = _factory.CreateClient();
        var badCredentials = new LoginRequestDto("admin@lms.test", "wrong-password");

        for (var attempt = 0; attempt < ThrottledLoginApiFactory.PermitLimit; attempt++)
        {
            var allowed = await client.PostAsJsonAsync("/api/v1/auth/login", badCredentials);
            allowed.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        var throttled = await client.PostAsJsonAsync("/api/v1/auth/login", badCredentials);

        throttled.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        throttled.Headers.Should().ContainSingle(h => h.Key == "Retry-After");

        var body = await throttled.Content.ReadAsStringAsync();
        body.Should().Contain("Too many login attempts");
    }

    [Fact]
    public async Task RateLimiter_DoesNotApply_ToOtherEndpoints()
    {
        var client = _factory.CreateClient();

        // Well past the login budget; an unauthenticated protected endpoint still answers 401,
        // proving the policy is scoped to /auth/login rather than the whole pipeline.
        for (var attempt = 0; attempt < ThrottledLoginApiFactory.PermitLimit + 2; attempt++)
        {
            var response = await client.GetAsync("/api/v1/users");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
