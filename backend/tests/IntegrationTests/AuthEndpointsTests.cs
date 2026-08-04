using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AssignmentSubmissionSystem.Application.Auth.Dtos;
using AssignmentSubmissionSystem.Application.Common;

namespace AssignmentSubmissionSystem.IntegrationTests;

public sealed class AuthEndpointsTests : IClassFixture<AuthApiFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointsTests(AuthApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_ReturnsTokenAndUser_ForSeededAdminCredentials()
    {
        // Arrange — from DbSeeder's documented demo credentials.
        var request = new LoginRequestDto("admin@lms.test", "Admin@12345");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", request);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body!.Success.Should().BeTrue();
        body.Data!.Token.Should().NotBeNullOrWhiteSpace();
        body.Data.User.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task Login_Returns401_ForWrongPassword()
    {
        var request = new LoginRequestDto("admin@lms.test", "wrong-password");

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", request);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        body!.Success.Should().BeFalse();
        body.Error.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_Returns400_ForMissingEmail()
    {
        var request = new LoginRequestDto("", "whatever");

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Me_Returns401_WithoutToken()
    {
        var response = await _client.GetAsync("/api/v1/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_ReturnsCallerIdentity_WithValidToken()
    {
        // Arrange — log in first to get a real token.
        var loginResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequestDto("teacher@lms.test", "Teacher@12345"));
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>();

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginBody!.Data!.Token);

        // Act
        var meResponse = await _client.GetAsync("/api/v1/auth/me");
        var meBody = await meResponse.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();

        // Assert
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        meBody!.Data!.Email.Should().Be("teacher@lms.test");
        meBody.Data.Role.Should().Be("Teacher");
    }
}
