using AssignmentSubmissionSystem.Application.Common;
using AssignmentSubmissionSystem.Application.Common.Constants;
using AssignmentSubmissionSystem.Application.Users;
using AssignmentSubmissionSystem.Application.Users.Dtos;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSubmissionSystem.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize(Roles = Roles.Admin)]
public sealed class UsersController(
    IUserService userService,
    IValidator<CreateUserDto> createValidator,
    IValidator<UpdateUserDto> updateValidator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UserSummaryDto>>>> GetAll(CancellationToken ct)
    {
        var users = await userService.GetAllAsync(ct);
        return Ok(ApiResponse<IReadOnlyList<UserSummaryDto>>.Ok(users));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserSummaryDto>>> GetById(Guid id, CancellationToken ct)
    {
        var user = await userService.GetByIdAsync(id, ct);
        return Ok(ApiResponse<UserSummaryDto>.Ok(user));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserSummaryDto>>> Create([FromBody] CreateUserDto request, CancellationToken ct)
    {
        var validation = await createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<UserSummaryDto>.Fail(validation.ToErrorMessage()));
        }

        var created = await userService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ApiResponse<UserSummaryDto>.Ok(created));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserSummaryDto>>> Update(Guid id, [FromBody] UpdateUserDto request, CancellationToken ct)
    {
        var validation = await updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<UserSummaryDto>.Fail(validation.ToErrorMessage()));
        }

        var updated = await userService.UpdateAsync(id, request, ct);
        return Ok(ApiResponse<UserSummaryDto>.Ok(updated));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await userService.DeleteAsync(id, ct);
        return NoContent();
    }
}
