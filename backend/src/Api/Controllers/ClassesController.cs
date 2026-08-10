using AssignmentSubmissionSystem.Application.Classes;
using AssignmentSubmissionSystem.Application.Classes.Dtos;
using AssignmentSubmissionSystem.Application.Common;
using AssignmentSubmissionSystem.Application.Common.Constants;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSubmissionSystem.Api.Controllers;

[ApiController]
[Route("api/v1/classes")]
[Authorize]
public sealed class ClassesController(
    IClassService classService,
    IValidator<CreateClassDto> createValidator,
    IValidator<UpdateClassDto> updateValidator) : ControllerBase
{
    // Open to any authenticated role — Admin manages classes, Teacher/Student browse their own.
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ClassSummaryDto>>>> GetAll(CancellationToken ct)
    {
        var classes = await classService.GetAllAsync(ct);
        return Ok(ApiResponse<IReadOnlyList<ClassSummaryDto>>.Ok(classes));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ClassSummaryDto>>> GetById(Guid id, CancellationToken ct)
    {
        var schoolClass = await classService.GetByIdAsync(id, ct);
        return Ok(ApiResponse<ClassSummaryDto>.Ok(schoolClass));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<ApiResponse<ClassSummaryDto>>> Create([FromBody] CreateClassDto request, CancellationToken ct)
    {
        var validation = await createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<ClassSummaryDto>.Fail(validation.ToErrorMessage()));
        }

        var created = await classService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ApiResponse<ClassSummaryDto>.Ok(created));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<ApiResponse<ClassSummaryDto>>> Update(Guid id, [FromBody] UpdateClassDto request, CancellationToken ct)
    {
        var validation = await updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<ClassSummaryDto>.Fail(validation.ToErrorMessage()));
        }

        var updated = await classService.UpdateAsync(id, request, ct);
        return Ok(ApiResponse<ClassSummaryDto>.Ok(updated));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await classService.DeleteAsync(id, ct);
        return NoContent();
    }

    // ---- Enrollment (Admin only) ----

    [HttpGet("{id:guid}/students")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<EnrolledStudentDto>>>> GetStudents(Guid id, CancellationToken ct)
    {
        var students = await classService.GetStudentsAsync(id, ct);
        return Ok(ApiResponse<IReadOnlyList<EnrolledStudentDto>>.Ok(students));
    }

    /// <summary>Enrolls a student, moving them out of any previous class (plan §11: one class per student).</summary>
    [HttpPost("{id:guid}/students")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<ApiResponse<EnrolledStudentDto>>> EnrollStudent(
        Guid id,
        [FromBody] EnrollStudentDto request,
        CancellationToken ct)
    {
        var enrolled = await classService.EnrollStudentAsync(id, request, ct);
        return Ok(ApiResponse<EnrolledStudentDto>.Ok(enrolled));
    }

    [HttpDelete("{id:guid}/students/{studentId:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> UnenrollStudent(Guid id, Guid studentId, CancellationToken ct)
    {
        await classService.UnenrollStudentAsync(id, studentId, ct);
        return NoContent();
    }
}
