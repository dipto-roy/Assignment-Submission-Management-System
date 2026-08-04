using AssignmentSubmissionSystem.Application.Common;
using AssignmentSubmissionSystem.Application.Common.Constants;
using AssignmentSubmissionSystem.Application.Subjects;
using AssignmentSubmissionSystem.Application.Subjects.Dtos;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSubmissionSystem.Api.Controllers;

[ApiController]
[Route("api/v1/subjects")]
[Authorize]
public sealed class SubjectsController(
    ISubjectService subjectService,
    IValidator<CreateSubjectDto> createValidator,
    IValidator<UpdateSubjectDto> updateValidator,
    IValidator<AssignTeacherDto> assignTeacherValidator) : ControllerBase
{
    // Open to any authenticated role — Admin manages subjects, Teacher/Student browse their own.
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SubjectSummaryDto>>>> GetAll(CancellationToken ct)
    {
        var subjects = await subjectService.GetAllAsync(ct);
        return Ok(ApiResponse<IReadOnlyList<SubjectSummaryDto>>.Ok(subjects));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SubjectSummaryDto>>> GetById(Guid id, CancellationToken ct)
    {
        var subject = await subjectService.GetByIdAsync(id, ct);
        return Ok(ApiResponse<SubjectSummaryDto>.Ok(subject));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<ApiResponse<SubjectSummaryDto>>> Create([FromBody] CreateSubjectDto request, CancellationToken ct)
    {
        var validation = await createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<SubjectSummaryDto>.Fail(validation.ToErrorMessage()));
        }

        var created = await subjectService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ApiResponse<SubjectSummaryDto>.Ok(created));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<ApiResponse<SubjectSummaryDto>>> Update(Guid id, [FromBody] UpdateSubjectDto request, CancellationToken ct)
    {
        var validation = await updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<SubjectSummaryDto>.Fail(validation.ToErrorMessage()));
        }

        var updated = await subjectService.UpdateAsync(id, request, ct);
        return Ok(ApiResponse<SubjectSummaryDto>.Ok(updated));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await subjectService.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/assign-teacher")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<ApiResponse<SubjectSummaryDto>>> AssignTeacher(
        Guid id,
        [FromBody] AssignTeacherDto request,
        CancellationToken ct)
    {
        var validation = await assignTeacherValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<SubjectSummaryDto>.Fail(validation.ToErrorMessage()));
        }

        var updated = await subjectService.AssignTeacherAsync(id, request, ct);
        return Ok(ApiResponse<SubjectSummaryDto>.Ok(updated));
    }
}
