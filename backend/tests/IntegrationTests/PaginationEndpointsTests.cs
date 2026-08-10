using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AssignmentSubmissionSystem.Application.Auth.Dtos;
using AssignmentSubmissionSystem.Application.Common;
using AssignmentSubmissionSystem.Application.Common.Paging;
using AssignmentSubmissionSystem.Application.Users.Dtos;

namespace AssignmentSubmissionSystem.IntegrationTests;

/// <summary>
/// Covers the query-string surface added for plan §10.4: page/pageSize clamping, meta totals,
/// and filters that narrow — never widen — what the caller's role already allows.
/// </summary>
public sealed class PaginationEndpointsTests : IClassFixture<AuthApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AuthApiFactory _factory;

    public PaginationEndpointsTests(AuthApiFactory factory)
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

    private static PageMeta ReadMeta<T>(ApiResponse<T> response) =>
        JsonSerializer.Deserialize<PageMeta>(
            JsonSerializer.Serialize(response.Meta, JsonOptions),
            JsonOptions)!;

    [Fact]
    public async Task GetUsers_ReturnsPageMeta_AndHonoursPageSize()
    {
        var admin = await AuthenticatedClientAsync("admin@lms.test", "Admin@12345");

        var response = await admin.GetAsync("/api/v1/users?page=1&pageSize=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = (await response.Content.ReadFromJsonAsync<ApiResponse<List<UserSummaryDto>>>())!;
        body.Data!.Should().HaveCount(1);

        var meta = ReadMeta(body);
        meta.Page.Should().Be(1);
        meta.PageSize.Should().Be(1);
        meta.Total.Should().BeGreaterThan(1, "the seeded database holds an admin, a teacher and a student");
        meta.TotalPages.Should().Be(meta.Total);
    }

    [Fact]
    public async Task GetUsers_ClampsOutOfRangePagingParameters()
    {
        var admin = await AuthenticatedClientAsync("admin@lms.test", "Admin@12345");

        var response = await admin.GetAsync("/api/v1/users?page=0&pageSize=100000");

        var body = (await response.Content.ReadFromJsonAsync<ApiResponse<List<UserSummaryDto>>>())!;
        var meta = ReadMeta(body);

        meta.Page.Should().Be(1);
        meta.PageSize.Should().Be(PageQuery.MaxPageSize);
    }

    [Fact]
    public async Task GetUsers_FiltersByRole()
    {
        var admin = await AuthenticatedClientAsync("admin@lms.test", "Admin@12345");

        var response = await admin.GetAsync("/api/v1/users?role=Student");

        var body = (await response.Content.ReadFromJsonAsync<ApiResponse<List<UserSummaryDto>>>())!;
        body.Data!.Should().NotBeEmpty();
        body.Data!.Should().OnlyContain(u => u.Role == "Student");
    }

    [Fact]
    public async Task GetUsers_FiltersBySearch_CaseInsensitively()
    {
        var admin = await AuthenticatedClientAsync("admin@lms.test", "Admin@12345");

        var response = await admin.GetAsync("/api/v1/users?search=TEACHER@LMS");

        var body = (await response.Content.ReadFromJsonAsync<ApiResponse<List<UserSummaryDto>>>())!;
        body.Data!.Should().ContainSingle(u => u.Email == "teacher@lms.test");
    }

    [Fact]
    public async Task GetAssignments_StatusFilter_CannotRevealDraftsToStudents()
    {
        var student = await AuthenticatedClientAsync("student@lms.test", "Student@12345");

        // A student asking for Drafts must get nothing: the role scope is applied before the
        // filter, so this narrows an already-Published-only set (business rule §7.3).
        var response = await student.GetAsync("/api/v1/assignments?status=Draft");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = (await response.Content.ReadFromJsonAsync<ApiResponse<List<object>>>())!;
        body.Data!.Should().BeEmpty();
        ReadMeta(body).Total.Should().Be(0);
    }
}
