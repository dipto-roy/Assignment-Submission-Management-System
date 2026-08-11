using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using AssignmentSubmissionSystem.Application.Assignments.Dtos;
using AssignmentSubmissionSystem.Application.Attachments.Dtos;
using AssignmentSubmissionSystem.Application.Auth.Dtos;
using AssignmentSubmissionSystem.Application.Classes.Dtos;
using AssignmentSubmissionSystem.Application.Common;
using AssignmentSubmissionSystem.Application.Subjects.Dtos;
using AssignmentSubmissionSystem.Application.Submissions.Dtos;
using AssignmentSubmissionSystem.Application.Users.Dtos;
using AssignmentSubmissionSystem.Domain.Enums;

namespace AssignmentSubmissionSystem.IntegrationTests;

/// <summary>
/// File upload, download and delete over the real pipeline, against the local storage provider
/// configured in <see cref="AuthApiFactory"/>. Cloudinary is never contacted.
/// </summary>
public sealed class AttachmentEndpointsTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;

    public AttachmentEndpointsTests(AuthApiFactory factory)
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

    private static async Task<AssignmentSummaryDto> CreatePublishedAssignmentAsync(HttpClient teacher, Guid subjectId)
    {
        var createResponse = await teacher.PostAsJsonAsync(
            "/api/v1/assignments",
            new CreateAssignmentDto($"Attachment Test {Guid.NewGuid():N}", "Desc", DateTime.UtcNow.AddDays(5), 100, subjectId));
        var created = (await createResponse.Content.ReadFromJsonAsync<ApiResponse<AssignmentSummaryDto>>())!.Data!;

        var publishResponse = await teacher.PatchAsJsonAsync(
            $"/api/v1/assignments/{created.Id}/publish",
            new SetPublishStateDto(true));

        return (await publishResponse.Content.ReadFromJsonAsync<ApiResponse<AssignmentSummaryDto>>())!.Data!;
    }

    /// <summary>Builds a multipart body with the single "file" part the endpoints expect.</summary>
    private static MultipartFormDataContent FilePart(
        string fileName = "essay.pdf",
        string contentType = "application/pdf",
        string body = "submitted coursework")
    {
        var content = new ByteArrayContent(Encoding.UTF8.GetBytes(body));
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        return new MultipartFormDataContent { { content, "file", fileName } };
    }

    private static async Task<Guid> SubmitAsync(HttpClient student, Guid assignmentId)
    {
        var response = await student.PostAsJsonAsync(
            $"/api/v1/assignments/{assignmentId}/submissions",
            new CreateSubmissionDto("My written answer"));
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        return (await response.Content.ReadFromJsonAsync<ApiResponse<SubmissionSummaryDto>>())!.Data!.Id;
    }

    private async Task<HttpClient> CreateSecondStudentAsync(HttpClient admin)
    {
        var classesResponse = await admin.GetAsync("/api/v1/classes");
        var classes = (await classesResponse.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<ClassSummaryDto>>>())!.Data!;
        var classId = classes.Single(c => c.Name == "Class 10").Id;

        var email = $"peer-student-{Guid.NewGuid():N}@lms.test";
        var registerResponse = await admin.PostAsJsonAsync(
            "/api/v1/users",
            new CreateUserDto("Peer Student", email, "PeerStudent@123", UserRole.Student, classId));
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        return await AuthenticatedClientAsync(email, "PeerStudent@123");
    }

    [Fact]
    public async Task Student_CanAttachAFileToTheirOwnSubmission_AndDownloadItBack()
    {
        var admin = await AuthenticatedClientAsync("admin@lms.test", "Admin@12345");
        var teacher = await AuthenticatedClientAsync("teacher@lms.test", "Teacher@12345");
        var student = await AuthenticatedClientAsync("student@lms.test", "Student@12345");
        var assignment = await CreatePublishedAssignmentAsync(teacher, await GetSeededMathSubjectIdAsync(admin));
        var submissionId = await SubmitAsync(student, assignment.Id);

        var uploadResponse = await student.PostAsync(
            $"/api/v1/submissions/{submissionId}/attachments",
            FilePart(body: "the actual coursework bytes"));

        uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var attachment = (await uploadResponse.Content.ReadFromJsonAsync<ApiResponse<AttachmentDto>>())!.Data!;
        attachment.FileName.Should().Be("essay.pdf");
        attachment.SizeBytes.Should().Be("the actual coursework bytes".Length);

        var downloadResponse = await student.GetAsync($"/api/v1/attachments/{attachment.Id}/download");

        downloadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await downloadResponse.Content.ReadAsStringAsync()).Should().Be("the actual coursework bytes");
    }

    [Fact]
    public async Task Download_SendsTheFileAsAnAttachment_WithSniffingDisabled()
    {
        // Inline rendering of an uploaded file would execute it on the API's own origin.
        var admin = await AuthenticatedClientAsync("admin@lms.test", "Admin@12345");
        var teacher = await AuthenticatedClientAsync("teacher@lms.test", "Teacher@12345");
        var student = await AuthenticatedClientAsync("student@lms.test", "Student@12345");
        var assignment = await CreatePublishedAssignmentAsync(teacher, await GetSeededMathSubjectIdAsync(admin));
        var submissionId = await SubmitAsync(student, assignment.Id);

        var uploadResponse = await student.PostAsync($"/api/v1/submissions/{submissionId}/attachments", FilePart());
        var attachment = (await uploadResponse.Content.ReadFromJsonAsync<ApiResponse<AttachmentDto>>())!.Data!;

        var downloadResponse = await student.GetAsync($"/api/v1/attachments/{attachment.Id}/download");

        downloadResponse.Content.Headers.ContentDisposition!.DispositionType.Should().Be("attachment");
        downloadResponse.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");
    }

    [Fact]
    public async Task Download_Returns403_ForAnotherStudentInTheSameClass()
    {
        var admin = await AuthenticatedClientAsync("admin@lms.test", "Admin@12345");
        var teacher = await AuthenticatedClientAsync("teacher@lms.test", "Teacher@12345");
        var owner = await AuthenticatedClientAsync("student@lms.test", "Student@12345");
        var assignment = await CreatePublishedAssignmentAsync(teacher, await GetSeededMathSubjectIdAsync(admin));
        var submissionId = await SubmitAsync(owner, assignment.Id);

        var uploadResponse = await owner.PostAsync($"/api/v1/submissions/{submissionId}/attachments", FilePart());
        var attachment = (await uploadResponse.Content.ReadFromJsonAsync<ApiResponse<AttachmentDto>>())!.Data!;

        var peer = await CreateSecondStudentAsync(admin);
        var downloadResponse = await peer.GetAsync($"/api/v1/attachments/{attachment.Id}/download");

        downloadResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Download_Succeeds_ForTheTeacherWhoSetTheAssignment()
    {
        var admin = await AuthenticatedClientAsync("admin@lms.test", "Admin@12345");
        var teacher = await AuthenticatedClientAsync("teacher@lms.test", "Teacher@12345");
        var student = await AuthenticatedClientAsync("student@lms.test", "Student@12345");
        var assignment = await CreatePublishedAssignmentAsync(teacher, await GetSeededMathSubjectIdAsync(admin));
        var submissionId = await SubmitAsync(student, assignment.Id);

        var uploadResponse = await student.PostAsync(
            $"/api/v1/submissions/{submissionId}/attachments",
            FilePart(body: "work to be marked"));
        var attachment = (await uploadResponse.Content.ReadFromJsonAsync<ApiResponse<AttachmentDto>>())!.Data!;

        var downloadResponse = await teacher.GetAsync($"/api/v1/attachments/{attachment.Id}/download");

        downloadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await downloadResponse.Content.ReadAsStringAsync()).Should().Be("work to be marked");
    }

    [Fact]
    public async Task Upload_Returns400_ForADisallowedFileType()
    {
        var admin = await AuthenticatedClientAsync("admin@lms.test", "Admin@12345");
        var teacher = await AuthenticatedClientAsync("teacher@lms.test", "Teacher@12345");
        var student = await AuthenticatedClientAsync("student@lms.test", "Student@12345");
        var assignment = await CreatePublishedAssignmentAsync(teacher, await GetSeededMathSubjectIdAsync(admin));
        var submissionId = await SubmitAsync(student, assignment.Id);

        var uploadResponse = await student.PostAsync(
            $"/api/v1/submissions/{submissionId}/attachments",
            FilePart("payload.exe", "application/octet-stream"));

        uploadResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Upload_Returns403_WhenAStudentTargetsAnotherStudentsSubmission()
    {
        var admin = await AuthenticatedClientAsync("admin@lms.test", "Admin@12345");
        var teacher = await AuthenticatedClientAsync("teacher@lms.test", "Teacher@12345");
        var owner = await AuthenticatedClientAsync("student@lms.test", "Student@12345");
        var assignment = await CreatePublishedAssignmentAsync(teacher, await GetSeededMathSubjectIdAsync(admin));
        var submissionId = await SubmitAsync(owner, assignment.Id);

        var peer = await CreateSecondStudentAsync(admin);
        var uploadResponse = await peer.PostAsync($"/api/v1/submissions/{submissionId}/attachments", FilePart());

        uploadResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Teacher_CanAttachABriefToTheirAssignment_AndAnEnrolledStudentCanReadIt()
    {
        var admin = await AuthenticatedClientAsync("admin@lms.test", "Admin@12345");
        var teacher = await AuthenticatedClientAsync("teacher@lms.test", "Teacher@12345");
        var student = await AuthenticatedClientAsync("student@lms.test", "Student@12345");
        var assignment = await CreatePublishedAssignmentAsync(teacher, await GetSeededMathSubjectIdAsync(admin));

        var uploadResponse = await teacher.PostAsync(
            $"/api/v1/assignments/{assignment.Id}/attachments",
            FilePart("brief.pdf", body: "the specification"));

        uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var attachment = (await uploadResponse.Content.ReadFromJsonAsync<ApiResponse<AttachmentDto>>())!.Data!;

        var downloadResponse = await student.GetAsync($"/api/v1/attachments/{attachment.Id}/download");

        downloadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await downloadResponse.Content.ReadAsStringAsync()).Should().Be("the specification");
    }

    [Fact]
    public async Task Upload_Returns403_WhenATeacherTargetsAnotherTeachersAssignment()
    {
        var admin = await AuthenticatedClientAsync("admin@lms.test", "Admin@12345");
        var teacher = await AuthenticatedClientAsync("teacher@lms.test", "Teacher@12345");
        var assignment = await CreatePublishedAssignmentAsync(teacher, await GetSeededMathSubjectIdAsync(admin));

        var otherEmail = $"other-teacher-{Guid.NewGuid():N}@lms.test";
        var registerResponse = await admin.PostAsJsonAsync(
            "/api/v1/users",
            new CreateUserDto("Other Teacher", otherEmail, "OtherTeacher@123", UserRole.Teacher, null));
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var otherTeacher = await AuthenticatedClientAsync(otherEmail, "OtherTeacher@123");

        var uploadResponse = await otherTeacher.PostAsync(
            $"/api/v1/assignments/{assignment.Id}/attachments",
            FilePart("brief.pdf"));

        uploadResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_RemovesTheFile_AndSubsequentDownloadsReturn404()
    {
        var admin = await AuthenticatedClientAsync("admin@lms.test", "Admin@12345");
        var teacher = await AuthenticatedClientAsync("teacher@lms.test", "Teacher@12345");
        var student = await AuthenticatedClientAsync("student@lms.test", "Student@12345");
        var assignment = await CreatePublishedAssignmentAsync(teacher, await GetSeededMathSubjectIdAsync(admin));
        var submissionId = await SubmitAsync(student, assignment.Id);

        var uploadResponse = await student.PostAsync($"/api/v1/submissions/{submissionId}/attachments", FilePart());
        var attachment = (await uploadResponse.Content.ReadFromJsonAsync<ApiResponse<AttachmentDto>>())!.Data!;

        var deleteResponse = await student.DeleteAsync($"/api/v1/attachments/{attachment.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var downloadResponse = await student.GetAsync($"/api/v1/attachments/{attachment.Id}/download");
        downloadResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Download_Returns401_WithoutAToken()
    {
        var anonymous = _factory.CreateClient();

        var response = await anonymous.GetAsync($"/api/v1/attachments/{Guid.NewGuid()}/download");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
