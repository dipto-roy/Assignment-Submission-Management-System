using System.Security.Claims;
using AssignmentSubmissionSystem.Application.Assignments;
using AssignmentSubmissionSystem.Application.Assignments.Dtos;
using AssignmentSubmissionSystem.Application.Common;
using AssignmentSubmissionSystem.Application.Common.Constants;
using AssignmentSubmissionSystem.Application.Common.Exceptions;
using AssignmentSubmissionSystem.Application.Submissions;
using AssignmentSubmissionSystem.Application.Submissions.Dtos;
using AssignmentSubmissionSystem.Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSubmissionSystem.Api.Controllers;

[ApiController]
[Route("api/v1/assignments")]
[Authorize]
public sealed class AssignmentsController(
    IAssignmentService assignmentService,
    ISubmissionService submissionService,
    IValidator<CreateAssignmentDto> createValidator,
    IValidator<UpdateAssignmentDto> updateValidator,
    IValidator<CreateSubmissionDto> createSubmissionValidator) : ControllerBase
{
    // Role-filtered: Admin → all, Teacher → own, Student → Published for their class.
    // Paginated and filterable: `?status=Published&subjectId=…&classId=…&search=essay&page=1&pageSize=20`.
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AssignmentSummaryDto>>>> GetAll(
        [FromQuery] AssignmentQuery query,
        CancellationToken ct)
    {
        var page = await assignmentService.GetAllAsync(CurrentUserId, CurrentRole, query, ct);
        return Ok(ApiResponse<IReadOnlyList<AssignmentSummaryDto>>.Ok(page.Items, page.ToMeta()));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AssignmentSummaryDto>>> GetById(Guid id, CancellationToken ct)
    {
        var assignment = await assignmentService.GetByIdAsync(id, CurrentUserId, CurrentRole, ct);
        return Ok(ApiResponse<AssignmentSummaryDto>.Ok(assignment));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Teacher)]
    public async Task<ActionResult<ApiResponse<AssignmentSummaryDto>>> Create([FromBody] CreateAssignmentDto request, CancellationToken ct)
    {
        var validation = await createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<AssignmentSummaryDto>.Fail(validation.ToErrorMessage()));
        }

        var created = await assignmentService.CreateAsync(CurrentUserId, request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ApiResponse<AssignmentSummaryDto>.Ok(created));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Teacher)]
    public async Task<ActionResult<ApiResponse<AssignmentSummaryDto>>> Update(Guid id, [FromBody] UpdateAssignmentDto request, CancellationToken ct)
    {
        var validation = await updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<AssignmentSummaryDto>.Fail(validation.ToErrorMessage()));
        }

        var updated = await assignmentService.UpdateAsync(id, CurrentUserId, request, ct);
        return Ok(ApiResponse<AssignmentSummaryDto>.Ok(updated));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Teacher)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await assignmentService.DeleteAsync(id, CurrentUserId, ct);
        return NoContent();
    }

    [HttpPatch("{id:guid}/publish")]
    [Authorize(Roles = Roles.Teacher)]
    public async Task<ActionResult<ApiResponse<AssignmentSummaryDto>>> SetPublishState(
        Guid id,
        [FromBody] SetPublishStateDto request,
        CancellationToken ct)
    {
        var updated = await assignmentService.SetPublishStateAsync(id, CurrentUserId, request, ct);
        return Ok(ApiResponse<AssignmentSummaryDto>.Ok(updated));
    }

    // Review view: Admin sees any assignment's submissions, Teacher only their own (business rule §7.5).
    [HttpGet("{id:guid}/submissions")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Teacher}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SubmissionDetailDto>>>> GetSubmissions(
        Guid id,
        [FromQuery] SubmissionQuery query,
        CancellationToken ct)
    {
        var page = await submissionService.GetForAssignmentAsync(id, CurrentUserId, CurrentRole, query, ct);
        return Ok(ApiResponse<IReadOnlyList<SubmissionDetailDto>>.Ok(page.Items, page.ToMeta()));
    }

    // Nested resource: a student submits their work against a specific assignment.
    [HttpPost("{id:guid}/submissions")]
    [Authorize(Roles = Roles.Student)]
    public async Task<ActionResult<ApiResponse<SubmissionSummaryDto>>> Submit(
        Guid id,
        [FromBody] CreateSubmissionDto request,
        CancellationToken ct)
    {
        var validation = await createSubmissionValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<SubmissionSummaryDto>.Fail(validation.ToErrorMessage()));
        }

        var created = await submissionService.SubmitAsync(id, CurrentUserId, request, ct);
        return CreatedAtAction(
            nameof(SubmissionsController.GetMine),
            "Submissions",
            null,
            ApiResponse<SubmissionSummaryDto>.Ok(created));
    }

    private Guid CurrentUserId
    {
        get
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw, out var id)
                ? id
                : throw new UnauthorizedAppException("Token is missing a valid user id.");
        }
    }

    private UserRole CurrentRole
    {
        get
        {
            var raw = User.FindFirstValue(ClaimTypes.Role);
            return Enum.TryParse<UserRole>(raw, out var role)
                ? role
                : throw new UnauthorizedAppException("Token is missing a valid role.");
        }
    }
}
