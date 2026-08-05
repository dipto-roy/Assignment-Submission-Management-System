using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AssignmentSubmissionSystem.Application.Assignments.Dtos;
using AssignmentSubmissionSystem.Application.Auth.Dtos;
using AssignmentSubmissionSystem.Application.Classes.Dtos;
using AssignmentSubmissionSystem.Application.Common;
using AssignmentSubmissionSystem.Application.Subjects.Dtos;
using AssignmentSubmissionSystem.Application.Submissions.Dtos;
using AssignmentSubmissionSystem.Application.Users.Dtos;
using AssignmentSubmissionSystem.Domain.Enums;

namespace AssignmentSubmissionSystem.IntegrationTests;

public sealed class StudentEndpointsTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;

    public StudentEndpointsTests(AuthApiFactory factory)
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

    /// <summary>Creates and publishes a fresh assignment so each test gets a clean, submission-free assignment.</summary>
    private static async Task<AssignmentSummaryDto> CreatePublishedAssignmentAsync(HttpClient teacher, Guid subjectId)
    {
        var createResponse = await teacher.PostAsJsonAsync(
            "/api/v1/assignments",
            new CreateAssignmentDto($"Test Assignment {Guid.NewGuid():N}", "Desc", DateTime.UtcNow.AddDays(5), 100, subjectId));
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<AssignmentSummaryDto>>())!.Data!;

        var publishResponse = await teacher.PatchAsJsonAsync($"/api/v1/assignments/{created.Id}/publish", new SetPublishStateDto(true));
        return (await publishResponse.Content.ReadFromJsonAsync<ApiResponse<AssignmentSummaryDto>>())!.Data!;
    }

    [Fact]
    public async Task SubmitAssignment_Returns403_ForNonStudentRole()
    {
        var admin = await AuthenticatedClientAsync("admin@lms.test", "Admin@12345");
        var teacher = await AuthenticatedClientAsync("teacher@lms.test", "Teacher@12345");
        var subjectId = await GetSeededMathSubjectIdAsync(admin);
        var assignment = await CreatePublishedAssignmentAsync(teacher, subjectId);

        var response = await teacher.PostAsJsonAsync($"/api/v1/assignments/{assignment.Id}/submissions", new CreateSubmissionDto("work"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SubmissionFlow_WorksEndToEnd_ForEnrolledStudent()
    {
        var admin = await AuthenticatedClientAsync("admin@lms.test", "Admin@12345");
        var teacher = await AuthenticatedClientAsync("teacher@lms.test", "Teacher@12345");
        var student = await AuthenticatedClientAsync("student@lms.test", "Student@12345");
        var subjectId = await GetSeededMathSubjectIdAsync(admin);
        var assignment = await CreatePublishedAssignmentAsync(teacher, subjectId);

        // Submit
        var submitResponse = await student.PostAsJsonAsync(
            $"/api/v1/assignments/{assignment.Id}/submissions",
            new CreateSubmissionDto("My first attempt"));
        submitResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await submitResponse.Content.ReadFromJsonAsync<ApiResponse<SubmissionSummaryDto>>())!.Data!;
        created.Content.Should().Be("My first attempt");
        created.Status.Should().Be(nameof(SubmissionStatus.Submitted));

        // Duplicate submit → 409
        var duplicateResponse = await student.PostAsJsonAsync(
            $"/api/v1/assignments/{assignment.Id}/submissions",
            new CreateSubmissionDto("Second attempt"));
        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Appears in "mine"
        var mineResponse = await student.GetAsync("/api/v1/submissions/mine");
        mineResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var mine = (await mineResponse.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<SubmissionSummaryDto>>>())!.Data!;
        mine.Should().Contain(s => s.Id == created.Id);

        // Update before deadline
        var updateResponse = await student.PutAsJsonAsync($"/api/v1/submissions/{created.Id}", new UpdateSubmissionDto("Revised attempt"));
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = (await updateResponse.Content.ReadFromJsonAsync<ApiResponse<SubmissionSummaryDto>>())!.Data!;
        updated.Content.Should().Be("Revised attempt");
    }

    [Fact]
    public async Task UpdateSubmission_Returns403_ForStudentWhoDoesNotOwnIt()
    {
        var admin = await AuthenticatedClientAsync("admin@lms.test", "Admin@12345");
        var teacher = await AuthenticatedClientAsync("teacher@lms.test", "Teacher@12345");
        var owner = await AuthenticatedClientAsync("student@lms.test", "Student@12345");
        var subjectId = await GetSeededMathSubjectIdAsync(admin);
        var assignment = await CreatePublishedAssignmentAsync(teacher, subjectId);

        var submitResponse = await owner.PostAsJsonAsync(
            $"/api/v1/assignments/{assignment.Id}/submissions",
            new CreateSubmissionDto("Owner's work"));
        submitResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var ownerSubmissionId = (await submitResponse.Content.ReadFromJsonAsync<ApiResponse<SubmissionSummaryDto>>())!.Data!.Id;

        // A second student, enrolled in the same class, with no submission of their own here.
        var classesResponse = await admin.GetAsync("/api/v1/classes");
        var classes = (await classesResponse.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<ClassSummaryDto>>>())!.Data!;
        var classId = classes.Single(c => c.Name == "Class 10").Id;

        var otherEmail = $"other-student-{Guid.NewGuid():N}@lms.test";
        var registerResponse = await admin.PostAsJsonAsync(
            "/api/v1/users",
            new CreateUserDto("Other Student", otherEmail, "OtherStudent@123", UserRole.Student, classId));
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var otherStudent = await AuthenticatedClientAsync(otherEmail, "OtherStudent@123");

        var updateResponse = await otherStudent.PutAsJsonAsync(
            $"/api/v1/submissions/{ownerSubmissionId}",
            new UpdateSubmissionDto("Hijacked"));

        updateResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
