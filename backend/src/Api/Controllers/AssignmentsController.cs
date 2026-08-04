using System.Security.Claims;
using AssignmentSubmissionSystem.Application.Assignments;
using AssignmentSubmissionSystem.Application.Assignments.Dtos;
using AssignmentSubmissionSystem.Application.Common;
using AssignmentSubmissionSystem.Application.Common.Constants;
using AssignmentSubmissionSystem.Application.Common.Exceptions;
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
    IValidator<CreateAssignmentDto> createValidator,
    IValidator<UpdateAssignmentDto> updateValidator) : ControllerBase
{
    // Role-filtered: Admin → all, Teacher → own, Student → Published for their class.
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AssignmentSummaryDto>>>> GetAll(CancellationToken ct)
    {
        var assignments = await assignmentService.GetAllAsync(CurrentUserId, CurrentRole, ct);
        return Ok(ApiResponse<IReadOnlyList<AssignmentSummaryDto>>.Ok(assignments));
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
