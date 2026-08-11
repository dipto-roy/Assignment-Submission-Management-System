using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AssignmentSubmissionSystem.Application.Assignments.Dtos;
using AssignmentSubmissionSystem.Application.Auth.Dtos;
using AssignmentSubmissionSystem.Application.Classes.Dtos;
using AssignmentSubmissionSystem.Application.Common;
using AssignmentSubmissionSystem.Application.Notifications.Dtos;
using AssignmentSubmissionSystem.Application.Subjects.Dtos;
using AssignmentSubmissionSystem.Application.Submissions.Dtos;
using AssignmentSubmissionSystem.Application.Users.Dtos;
using AssignmentSubmissionSystem.Domain.Enums;

namespace AssignmentSubmissionSystem.IntegrationTests;

/// <summary>
/// End-to-end coverage of the notification triggers and the endpoints that read them.
/// The deadline reminder worker is disabled for the suite (see <see cref="AuthApiFactory"/>),
/// so every row asserted on here was created by the action under test.
/// </summary>
public sealed class NotificationEndpointsTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;

    public NotificationEndpointsTests(AuthApiFactory factory)
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

    private static async Task<AssignmentSummaryDto> CreateDraftAssignmentAsync(HttpClient teacher, Guid subjectId)
    {
        var response = await teacher.PostAsJsonAsync(
            "/api/v1/assignments",
            new CreateAssignmentDto($"Notify Test {Guid.NewGuid():N}", "Desc", DateTime.UtcNow.AddDays(5), 100, subjectId));

        return (await response.Content.ReadFromJsonAsync<ApiResponse<AssignmentSummaryDto>>())!.Data!;
    }

    private static Task<HttpResponseMessage> PublishAsync(HttpClient teacher, Guid assignmentId) =>
        teacher.PatchAsJsonAsync($"/api/v1/assignments/{assignmentId}/publish", new SetPublishStateDto(true));

    private static async Task<IReadOnlyList<NotificationDto>> ListAsync(HttpClient client, bool unreadOnly = false)
    {
        var response = await client.GetAsync($"/api/v1/notifications?pageSize=100&unreadOnly={unreadOnly}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return (await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<NotificationDto>>>())!.Data!;
    }

    private static async Task<int> UnreadCountAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/notifications/unread-count");
        return (await response.Content.ReadFromJsonAsync<ApiResponse<UnreadCountDto>>())!.Data!.Unread;
    }

    /// <summary>A fresh student in the seeded class, so counts start from a known zero.</summary>
    private async Task<(HttpClient Client, string Email)> CreateStudentAsync(HttpClient admin)
    {
        var classesResponse = await admin.GetAsync("/api/v1/classes");
        var classes = (await classesResponse.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<ClassSummaryDto>>>())!.Data!;
        var classId = classes.Single(c => c.Name == "Class 10").Id;

        var email = $"notify-student-{Guid.NewGuid():N}@lms.test";
        var registerResponse = await admin.PostAsJsonAsync(
            "/api/v1/users",
            new CreateUserDto("Notify Student", email, "NotifyStudent@123", UserRole.Student, classId));
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        return (await AuthenticatedClientAsync(email, "NotifyStudent@123"), email);
    }

    [Fact]
    public async Task PublishingAnAssignment_NotifiesEveryStudentInTheClass()
    {
        var admin = await AuthenticatedClientAsync("admin@lms.test", "Admin@12345");
        var teacher = await AuthenticatedClientAsync("teacher@lms.test", "Teacher@12345");
        var (student, _) = await CreateStudentAsync(admin);

        (await UnreadCountAsync(student)).Should().Be(0);

        var assignment = await CreateDraftAssignmentAsync(teacher, await GetSeededMathSubjectIdAsync(admin));
        (await PublishAsync(teacher, assignment.Id)).StatusCode.Should().Be(HttpStatusCode.OK);

        var notifications = await ListAsync(student);
        var published = notifications.Should().ContainSingle(n => n.AssignmentId == assignment.Id).Subject;
        published.Type.Should().Be(nameof(NotificationType.AssignmentPublished));
        published.IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task CreatingAnAssignment_NotifiesNobodyUntilItIsPublished()
    {
        // A draft is invisible to students, so announcing it would leak unfinished work.
        var admin = await AuthenticatedClientAsync("admin@lms.test", "Admin@12345");
        var teacher = await AuthenticatedClientAsync("teacher@lms.test", "Teacher@12345");
        var (student, _) = await CreateStudentAsync(admin);

        await CreateDraftAssignmentAsync(teacher, await GetSeededMathSubjectIdAsync(admin));

        (await UnreadCountAsync(student)).Should().Be(0);
    }

    [Fact]
    public async Task RepublishingAnAlreadyPublishedAssignment_DoesNotNotifyAgain()
    {
        var admin = await AuthenticatedClientAsync("admin@lms.test", "Admin@12345");
        var teacher = await AuthenticatedClientAsync("teacher@lms.test", "Teacher@12345");
        var (student, _) = await CreateStudentAsync(admin);
        var assignment = await CreateDraftAssignmentAsync(teacher, await GetSeededMathSubjectIdAsync(admin));

        await PublishAsync(teacher, assignment.Id);
        await PublishAsync(teacher, assignment.Id);
        await PublishAsync(teacher, assignment.Id);

        var forThisAssignment = (await ListAsync(student)).Where(n => n.AssignmentId == assignment.Id);
        forThisAssignment.Should().HaveCount(1);
    }

    [Fact]
    public async Task SubmittingWork_NotifiesTheOwningTeacher()
    {
        var admin = await AuthenticatedClientAsync("admin@lms.test", "Admin@12345");
        var teacher = await AuthenticatedClientAsync("teacher@lms.test", "Teacher@12345");
        var (student, _) = await CreateStudentAsync(admin);
        var assignment = await CreateDraftAssignmentAsync(teacher, await GetSeededMathSubjectIdAsync(admin));
        await PublishAsync(teacher, assignment.Id);

        var submitResponse = await student.PostAsJsonAsync(
            $"/api/v1/assignments/{assignment.Id}/submissions",
            new CreateSubmissionDto("My answer"));
        submitResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var teacherNotifications = await ListAsync(teacher);
        teacherNotifications
            .Should().Contain(n =>
                n.AssignmentId == assignment.Id
                && n.Type == nameof(NotificationType.SubmissionReceived));
    }

    [Fact]
    public async Task GradingWork_NotifiesTheStudentWithTheScore()
    {
        var admin = await AuthenticatedClientAsync("admin@lms.test", "Admin@12345");
        var teacher = await AuthenticatedClientAsync("teacher@lms.test", "Teacher@12345");
        var (student, _) = await CreateStudentAsync(admin);
        var assignment = await CreateDraftAssignmentAsync(teacher, await GetSeededMathSubjectIdAsync(admin));
        await PublishAsync(teacher, assignment.Id);

        var submitResponse = await student.PostAsJsonAsync(
            $"/api/v1/assignments/{assignment.Id}/submissions",
            new CreateSubmissionDto("My answer"));
        var submissionId = (await submitResponse.Content.ReadFromJsonAsync<ApiResponse<SubmissionSummaryDto>>())!.Data!.Id;

        var gradeResponse = await teacher.PatchAsJsonAsync(
            $"/api/v1/submissions/{submissionId}/grade",
            new GradeSubmissionDto(87, "Solid work"));
        gradeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var graded = (await ListAsync(student))
            .Should().ContainSingle(n => n.Type == nameof(NotificationType.SubmissionGraded)).Subject;
        graded.Message.Should().Contain("87/100");
        graded.SubmissionId.Should().Be(submissionId);
    }

    [Fact]
    public async Task MarkingOneNotificationRead_DecrementsTheUnreadCount()
    {
        var admin = await AuthenticatedClientAsync("admin@lms.test", "Admin@12345");
        var teacher = await AuthenticatedClientAsync("teacher@lms.test", "Teacher@12345");
        var (student, _) = await CreateStudentAsync(admin);
        var assignment = await CreateDraftAssignmentAsync(teacher, await GetSeededMathSubjectIdAsync(admin));
        await PublishAsync(teacher, assignment.Id);

        var notification = (await ListAsync(student)).Single(n => n.AssignmentId == assignment.Id);
        (await UnreadCountAsync(student)).Should().Be(1);

        var readResponse = await student.PatchAsync($"/api/v1/notifications/{notification.Id}/read", null);
        readResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        (await UnreadCountAsync(student)).Should().Be(0);
        (await ListAsync(student, unreadOnly: true)).Should().BeEmpty();
    }

    [Fact]
    public async Task MarkAllRead_ClearsEveryUnreadNotification()
    {
        var admin = await AuthenticatedClientAsync("admin@lms.test", "Admin@12345");
        var teacher = await AuthenticatedClientAsync("teacher@lms.test", "Teacher@12345");
        var (student, _) = await CreateStudentAsync(admin);
        var subjectId = await GetSeededMathSubjectIdAsync(admin);

        foreach (var _ in Enumerable.Range(0, 3))
        {
            var assignment = await CreateDraftAssignmentAsync(teacher, subjectId);
            await PublishAsync(teacher, assignment.Id);
        }

        (await UnreadCountAsync(student)).Should().Be(3);

        var response = await student.PostAsync("/api/v1/notifications/read-all", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        (await UnreadCountAsync(student)).Should().Be(0);
    }

    [Fact]
    public async Task MarkingAnotherUsersNotificationRead_Returns404()
    {
        // Reported as missing rather than forbidden: existence is not this caller's business.
        var admin = await AuthenticatedClientAsync("admin@lms.test", "Admin@12345");
        var teacher = await AuthenticatedClientAsync("teacher@lms.test", "Teacher@12345");
        var (owner, _) = await CreateStudentAsync(admin);
        var (intruder, _) = await CreateStudentAsync(admin);
        var assignment = await CreateDraftAssignmentAsync(teacher, await GetSeededMathSubjectIdAsync(admin));
        await PublishAsync(teacher, assignment.Id);

        var ownerNotification = (await ListAsync(owner)).Single(n => n.AssignmentId == assignment.Id);

        var response = await intruder.PatchAsync($"/api/v1/notifications/{ownerNotification.Id}/read", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await UnreadCountAsync(owner)).Should().Be(1);
    }

    [Fact]
    public async Task ANotificationListNeverContainsAnotherUsersRows()
    {
        var admin = await AuthenticatedClientAsync("admin@lms.test", "Admin@12345");
        var teacher = await AuthenticatedClientAsync("teacher@lms.test", "Teacher@12345");
        var (studentA, _) = await CreateStudentAsync(admin);
        var (studentB, _) = await CreateStudentAsync(admin);
        var assignment = await CreateDraftAssignmentAsync(teacher, await GetSeededMathSubjectIdAsync(admin));
        await PublishAsync(teacher, assignment.Id);

        var submitResponse = await studentA.PostAsJsonAsync(
            $"/api/v1/assignments/{assignment.Id}/submissions",
            new CreateSubmissionDto("A's answer"));
        submitResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Both were told the assignment was published; only the teacher hears about the
        // submission, and B learns nothing about A.
        (await ListAsync(studentB)).Should().NotContain(n => n.Type == nameof(NotificationType.SubmissionReceived));
        (await ListAsync(studentB)).Should().HaveCount(1);
    }

    [Fact]
    public async Task NotificationEndpoints_Return401_WithoutAToken()
    {
        var anonymous = _factory.CreateClient();

        (await anonymous.GetAsync("/api/v1/notifications")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await anonymous.GetAsync("/api/v1/notifications/unread-count")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
