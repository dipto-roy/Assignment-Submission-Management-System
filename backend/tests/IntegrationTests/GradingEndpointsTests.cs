using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AssignmentSubmissionSystem.Application.Assignments.Dtos;
using AssignmentSubmissionSystem.Application.Auth.Dtos;
using AssignmentSubmissionSystem.Application.Common;
using AssignmentSubmissionSystem.Application.Subjects.Dtos;
using AssignmentSubmissionSystem.Application.Submissions.Dtos;
using AssignmentSubmissionSystem.Domain.Enums;

namespace AssignmentSubmissionSystem.IntegrationTests;

/// <summary>Phase 6 — teacher grading/feedback/status and the student's view of their marks.</summary>
public sealed class GradingEndpointsTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;

    public GradingEndpointsTests(AuthApiFactory factory)
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

    private static async Task<Guid> GetSeededMathSubjectIdAsync(HttpClient admin)
    {
        var response = await admin.GetAsync("/api/v1/subjects");
        var subjects = (await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<SubjectSummaryDto>>>())!.Data!;
        return subjects.Single(s => s.Code == "MATH101").Id;
    }

    private static async Task<AssignmentSummaryDto> CreatePublishedAssignmentAsync(HttpClient teacher, Guid subjectId, int maxMarks = 100)
    {
        var createResponse = await teacher.PostAsJsonAsync(
            "/api/v1/assignments",
            new CreateAssignmentDto($"Graded Assignment {Guid.NewGuid():N}", "Desc", DateTime.UtcNow.AddDays(5), maxMarks, subjectId));
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<AssignmentSummaryDto>>())!.Data!;

        var publishResponse = await teacher.PatchAsJsonAsync($"/api/v1/assignments/{created.Id}/publish", new SetPublishStateDto(true));
        return (await publishResponse.Content.ReadFromJsonAsync<ApiResponse<AssignmentSummaryDto>>())!.Data!;
    }

    private static async Task<SubmissionSummaryDto> SubmitAsync(HttpClient student, Guid assignmentId, string content)
    {
        var response = await student.PostAsJsonAsync($"/api/v1/assignments/{assignmentId}/submissions", new CreateSubmissionDto(content));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ApiResponse<SubmissionSummaryDto>>())!.Data!;
    }

    [Fact]
    public async Task GradingFlow_WorksEndToEnd_AndStudentSeesMarksAndFeedback()
    {
        var admin = await AuthenticatedClientAsync("admin@lms.test", "Admin@12345");
        var teacher = await AuthenticatedClientAsync("teacher@lms.test", "Teacher@12345");
        var student = await AuthenticatedClientAsync("student@lms.test", "Student@12345");
        var subjectId = await GetSeededMathSubjectIdAsync(admin);
        var assignment = await CreatePublishedAssignmentAsync(teacher, subjectId, maxMarks: 50);
        var submission = await SubmitAsync(student, assignment.Id, "My answer");

        // Teacher reviews every submission for the assignment, with student identity attached.
        var reviewResponse = await teacher.GetAsync($"/api/v1/assignments/{assignment.Id}/submissions");
        reviewResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var review = (await reviewResponse.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<SubmissionDetailDto>>>())!.Data!;
        review.Should().ContainSingle(s => s.Id == submission.Id && s.StudentEmail == "student@lms.test");

        // Grade with marks + feedback.
        var gradeResponse = await teacher.PatchAsJsonAsync(
            $"/api/v1/submissions/{submission.Id}/grade",
            new GradeSubmissionDto(45, "Solid reasoning, watch the algebra."));
        gradeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var graded = (await gradeResponse.Content.ReadFromJsonAsync<ApiResponse<SubmissionDetailDto>>())!.Data!;
        graded.Marks.Should().Be(45);
        graded.Feedback.Should().Be("Solid reasoning, watch the algebra.");
        graded.Status.Should().Be(nameof(SubmissionStatus.Graded));

        // Hand the marked work back.
        var statusResponse = await teacher.PatchAsJsonAsync(
            $"/api/v1/submissions/{submission.Id}/status",
            new SetSubmissionStatusDto(SubmissionStatus.Returned));
        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await statusResponse.Content.ReadFromJsonAsync<ApiResponse<SubmissionDetailDto>>())!.Data!.Status
            .Should().Be(nameof(SubmissionStatus.Returned));

        // Student sees marks + feedback on their own submission — closing the loop.
        var mineResponse = await student.GetAsync("/api/v1/submissions/mine");
        var mine = (await mineResponse.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<SubmissionSummaryDto>>>())!.Data!;
        var mineGraded = mine.Single(s => s.Id == submission.Id);
        mineGraded.Marks.Should().Be(45);
        mineGraded.Feedback.Should().Be("Solid reasoning, watch the algebra.");
        mineGraded.Status.Should().Be(nameof(SubmissionStatus.Returned));
        mineGraded.GradedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Grade_Returns400_WhenMarksExceedMaxMarks()
    {
        var admin = await AuthenticatedClientAsync("admin@lms.test", "Admin@12345");
        var teacher = await AuthenticatedClientAsync("teacher@lms.test", "Teacher@12345");
        var student = await AuthenticatedClientAsync("student@lms.test", "Student@12345");
        var subjectId = await GetSeededMathSubjectIdAsync(admin);
        var assignment = await CreatePublishedAssignmentAsync(teacher, subjectId, maxMarks: 20);
        var submission = await SubmitAsync(student, assignment.Id, "My answer");

        var response = await teacher.PatchAsJsonAsync(
            $"/api/v1/submissions/{submission.Id}/grade",
            new GradeSubmissionDto(21, "Over the cap"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Grade_Returns403_ForStudentRole()
    {
        var admin = await AuthenticatedClientAsync("admin@lms.test", "Admin@12345");
        var teacher = await AuthenticatedClientAsync("teacher@lms.test", "Teacher@12345");
        var student = await AuthenticatedClientAsync("student@lms.test", "Student@12345");
        var subjectId = await GetSeededMathSubjectIdAsync(admin);
        var assignment = await CreatePublishedAssignmentAsync(teacher, subjectId);
        var submission = await SubmitAsync(student, assignment.Id, "My answer");

        var response = await student.PatchAsJsonAsync(
            $"/api/v1/submissions/{submission.Id}/grade",
            new GradeSubmissionDto(100, "Self-awarded"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetSubmissionsForAssignment_Returns403_ForStudentRole()
    {
        var admin = await AuthenticatedClientAsync("admin@lms.test", "Admin@12345");
        var teacher = await AuthenticatedClientAsync("teacher@lms.test", "Teacher@12345");
        var student = await AuthenticatedClientAsync("student@lms.test", "Student@12345");
        var subjectId = await GetSeededMathSubjectIdAsync(admin);
        var assignment = await CreatePublishedAssignmentAsync(teacher, subjectId);

        var response = await student.GetAsync($"/api/v1/assignments/{assignment.Id}/submissions");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetSubmissionsForAssignment_Succeeds_ForAdmin()
    {
        var admin = await AuthenticatedClientAsync("admin@lms.test", "Admin@12345");
        var teacher = await AuthenticatedClientAsync("teacher@lms.test", "Teacher@12345");
        var subjectId = await GetSeededMathSubjectIdAsync(admin);
        var assignment = await CreatePublishedAssignmentAsync(teacher, subjectId);

        var response = await admin.GetAsync($"/api/v1/assignments/{assignment.Id}/submissions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SetStatus_Returns400_WhenReturningUngradedWork()
    {
        var admin = await AuthenticatedClientAsync("admin@lms.test", "Admin@12345");
        var teacher = await AuthenticatedClientAsync("teacher@lms.test", "Teacher@12345");
        var student = await AuthenticatedClientAsync("student@lms.test", "Student@12345");
        var subjectId = await GetSeededMathSubjectIdAsync(admin);
        var assignment = await CreatePublishedAssignmentAsync(teacher, subjectId);
        var submission = await SubmitAsync(student, assignment.Id, "My answer");

        var response = await teacher.PatchAsJsonAsync(
            $"/api/v1/submissions/{submission.Id}/status",
            new SetSubmissionStatusDto(SubmissionStatus.Returned));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
