using System.Security.Claims;
using AssignmentSubmissionSystem.Application.Common;
using AssignmentSubmissionSystem.Application.Common.Constants;
using AssignmentSubmissionSystem.Application.Common.Exceptions;
using AssignmentSubmissionSystem.Application.Submissions;
using AssignmentSubmissionSystem.Application.Submissions.Dtos;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSubmissionSystem.Api.Controllers;

[ApiController]
[Route("api/v1/submissions")]
[Authorize]
public sealed class SubmissionsController(
    ISubmissionService submissionService,
    IValidator<UpdateSubmissionDto> updateValidator,
    IValidator<GradeSubmissionDto> gradeValidator,
    IValidator<SetSubmissionStatusDto> statusValidator) : ControllerBase
{
    // Business rule §7.4: a student only ever sees their own submissions.
    [HttpGet("mine")]
    [Authorize(Roles = Roles.Student)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SubmissionSummaryDto>>>> GetMine(
        [FromQuery] SubmissionQuery query,
        CancellationToken ct)
    {
        var page = await submissionService.GetMineAsync(CurrentUserId, query, ct);
        return Ok(ApiResponse<IReadOnlyList<SubmissionSummaryDto>>.Ok(page.Items, page.ToMeta()));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Student)]
    public async Task<ActionResult<ApiResponse<SubmissionSummaryDto>>> Update(Guid id, [FromBody] UpdateSubmissionDto request, CancellationToken ct)
    {
        var validation = await updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<SubmissionSummaryDto>.Fail(validation.ToErrorMessage()));
        }

        var updated = await submissionService.UpdateAsync(id, CurrentUserId, request, ct);
        return Ok(ApiResponse<SubmissionSummaryDto>.Ok(updated));
    }

    // Marks + feedback. Owning teacher only (business rules §7.5, §7.6).
    [HttpPatch("{id:guid}/grade")]
    [Authorize(Roles = Roles.Teacher)]
    public async Task<ActionResult<ApiResponse<SubmissionDetailDto>>> Grade(Guid id, [FromBody] GradeSubmissionDto request, CancellationToken ct)
    {
        var validation = await gradeValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<SubmissionDetailDto>.Fail(validation.ToErrorMessage()));
        }

        var graded = await submissionService.GradeAsync(id, CurrentUserId, request, ct);
        return Ok(ApiResponse<SubmissionDetailDto>.Ok(graded));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = Roles.Teacher)]
    public async Task<ActionResult<ApiResponse<SubmissionDetailDto>>> SetStatus(
        Guid id,
        [FromBody] SetSubmissionStatusDto request,
        CancellationToken ct)
    {
        var validation = await statusValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<SubmissionDetailDto>.Fail(validation.ToErrorMessage()));
        }

        var updated = await submissionService.SetStatusAsync(id, CurrentUserId, request, ct);
        return Ok(ApiResponse<SubmissionDetailDto>.Ok(updated));
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
}
