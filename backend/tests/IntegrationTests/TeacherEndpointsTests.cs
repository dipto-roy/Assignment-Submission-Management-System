using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AssignmentSubmissionSystem.Application.Assignments.Dtos;
using AssignmentSubmissionSystem.Application.Auth.Dtos;
using AssignmentSubmissionSystem.Application.Common;
using AssignmentSubmissionSystem.Application.Subjects.Dtos;
using AssignmentSubmissionSystem.Application.Users.Dtos;
using AssignmentSubmissionSystem.Domain.Enums;

namespace AssignmentSubmissionSystem.IntegrationTests;

public sealed class TeacherEndpointsTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;

    public TeacherEndpointsTests(AuthApiFactory factory)
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

    /// <summary>Seeded teacher (teacher@lms.test) is already assigned to the seeded "Mathematics" subject.</summary>
    private static async Task<Guid> GetSeededMathSubjectIdAsync(HttpClient admin)
    {
        var response = await admin.GetAsync("/api/v1/subjects");
        var subjects = (await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<SubjectSummaryDto>>>())!.Data!;
        return subjects.Single(s => s.Code == "MATH101").Id;
    }

    [Fact]
    public async Task CreateAssignment_Returns403_ForStudentRole()
    {
        var client = await AuthenticatedClientAsync("student@lms.test", "Student@12345");
        var dto = new CreateAssignmentDto("Should Not Create", "Desc", DateTime.UtcNow.AddDays(1), 100, Guid.NewGuid());

        var response = await client.PostAsJsonAsync("/api/v1/assignments", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateAssignment_Returns403_WhenTeacherNotAssignedToSubject()
    {
        var teacher = await AuthenticatedClientAsync("teacher@lms.test", "Teacher@12345");
        var dto = new CreateAssignmentDto("Should Not Create", "Desc", DateTime.UtcNow.AddDays(1), 100, Guid.NewGuid());

        var response = await teacher.PostAsJsonAsync("/api/v1/assignments", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AssignmentsCrud_WorksEndToEnd_ForAssignedTeacher()
    {
        var admin = await AuthenticatedClientAsync("admin@lms.test", "Admin@12345");
        var teacher = await AuthenticatedClientAsync("teacher@lms.test", "Teacher@12345");
        var subjectId = await GetSeededMathSubjectIdAsync(admin);

        // Create → starts as Draft
        var createDto = new CreateAssignmentDto($"Test Assignment {Guid.NewGuid():N}", "Desc", DateTime.UtcNow.AddDays(5), 100, subjectId);
        var createResponse = await teacher.PostAsJsonAsync("/api/v1/assignments", createDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<AssignmentSummaryDto>>())!.Data!;
        created.Status.Should().Be(nameof(AssignmentStatus.Draft));

        // Read
        var getResponse = await teacher.GetAsync($"/api/v1/assignments/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Update
        var updateDto = new UpdateAssignmentDto("Updated Title", "Updated Desc", DateTime.UtcNow.AddDays(6), 80);
        var updateResponse = await teacher.PutAsJsonAsync($"/api/v1/assignments/{created.Id}", updateDto);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = (await updateResponse.Content.ReadFromJsonAsync<ApiResponse<AssignmentSummaryDto>>())!.Data!;
        updated.Title.Should().Be("Updated Title");

        // Publish
        var publishResponse = await teacher.PatchAsJsonAsync($"/api/v1/assignments/{created.Id}/publish", new SetPublishStateDto(true));
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var published = (await publishResponse.Content.ReadFromJsonAsync<ApiResponse<AssignmentSummaryDto>>())!.Data!;
        published.Status.Should().Be(nameof(AssignmentStatus.Published));

        // Revert to Draft
        var unpublishResponse = await teacher.PatchAsJsonAsync($"/api/v1/assignments/{created.Id}/publish", new SetPublishStateDto(false));
        (await unpublishResponse.Content.ReadFromJsonAsync<ApiResponse<AssignmentSummaryDto>>())!.Data!.Status
            .Should().Be(nameof(AssignmentStatus.Draft));

        // Delete
        var deleteResponse = await teacher.DeleteAsync($"/api/v1/assignments/{created.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterDelete = await teacher.GetAsync($"/api/v1/assignments/{created.Id}");
        afterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateAssignment_Returns403_ForTeacherWhoDoesNotOwnIt()
    {
        var admin = await AuthenticatedClientAsync("admin@lms.test", "Admin@12345");
        var owner = await AuthenticatedClientAsync("teacher@lms.test", "Teacher@12345");
        var subjectId = await GetSeededMathSubjectIdAsync(admin);

        var createResponse = await owner.PostAsJsonAsync(
            "/api/v1/assignments",
            new CreateAssignmentDto($"Owned Assignment {Guid.NewGuid():N}", "Desc", DateTime.UtcNow.AddDays(5), 100, subjectId));
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<AssignmentSummaryDto>>())!.Data!;

        // A second teacher, unrelated to this assignment.
        var otherEmail = $"other-teacher-{Guid.NewGuid():N}@lms.test";
        var registerResponse = await admin.PostAsJsonAsync(
            "/api/v1/users",
            new CreateUserDto("Other Teacher", otherEmail, "OtherTeacher@123", UserRole.Teacher, null));
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var otherTeacher = await AuthenticatedClientAsync(otherEmail, "OtherTeacher@123");

        var updateResponse = await otherTeacher.PutAsJsonAsync(
            $"/api/v1/assignments/{created.Id}",
            new UpdateAssignmentDto("Hijacked", "Desc", DateTime.UtcNow.AddDays(1), 50));

        updateResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
