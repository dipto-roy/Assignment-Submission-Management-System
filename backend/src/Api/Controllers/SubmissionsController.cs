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
    IValidator<UpdateSubmissionDto> updateValidator) : ControllerBase
{
    // Business rule §7.4: a student only ever sees their own submissions.
    [HttpGet("mine")]
    [Authorize(Roles = Roles.Student)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SubmissionSummaryDto>>>> GetMine(CancellationToken ct)
    {
        var submissions = await submissionService.GetMineAsync(CurrentUserId, ct);
        return Ok(ApiResponse<IReadOnlyList<SubmissionSummaryDto>>.Ok(submissions));
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
