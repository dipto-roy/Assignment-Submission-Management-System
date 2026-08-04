using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AssignmentSubmissionSystem.Application.Auth.Dtos;
using AssignmentSubmissionSystem.Application.Classes.Dtos;
using AssignmentSubmissionSystem.Application.Common;

namespace AssignmentSubmissionSystem.IntegrationTests;

public sealed class AdminEndpointsTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;

    public AdminEndpointsTests(AuthApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> AuthenticatedClientAsync(string email, string password)
    {
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDto(email, password));
        var body = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Data!.Token);
        return client;
    }

    [Fact]
    public async Task GetUsers_Returns403_ForNonAdminRole()
    {
        var client = await AuthenticatedClientAsync("teacher@lms.test", "Teacher@12345");

        var response = await client.GetAsync("/api/v1/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUsers_Returns401_WithoutToken()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/users");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ClassesCrud_WorksEndToEnd_ForAdmin()
    {
        var admin = await AuthenticatedClientAsync("admin@lms.test", "Admin@12345");
        var createDto = new CreateClassDto($"Test Class {Guid.NewGuid():N}", "Z");

        // Create
        var createResponse = await admin.PostAsJsonAsync("/api/v1/classes", createDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<ClassSummaryDto>>())!.Data!;

        // Read
        var getResponse = await admin.GetAsync($"/api/v1/classes/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Update
        var updateDto = new UpdateClassDto(createDto.Name, "Updated-Section");
        var updateResponse = await admin.PutAsJsonAsync($"/api/v1/classes/{created.Id}", updateDto);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = (await updateResponse.Content.ReadFromJsonAsync<ApiResponse<ClassSummaryDto>>())!.Data!;
        updated.Section.Should().Be("Updated-Section");

        // Delete
        var deleteResponse = await admin.DeleteAsync($"/api/v1/classes/{created.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterDelete = await admin.GetAsync($"/api/v1/classes/{created.Id}");
        afterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateClass_Returns403_ForStudentRole()
    {
        var client = await AuthenticatedClientAsync("student@lms.test", "Student@12345");

        var response = await client.PostAsJsonAsync("/api/v1/classes", new CreateClassDto("Should Not Create", null));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
