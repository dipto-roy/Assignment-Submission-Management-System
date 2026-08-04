using System.Security.Claims;
using AssignmentSubmissionSystem.Application.Auth;
using AssignmentSubmissionSystem.Application.Auth.Dtos;
using AssignmentSubmissionSystem.Application.Common;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSubmissionSystem.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IAuthService authService, IValidator<LoginRequestDto> loginValidator) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        var validation = await loginValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<LoginResponseDto>.Fail(validation.ToErrorMessage()));
        }

        var result = await authService.LoginAsync(request, cancellationToken);
        return Ok(ApiResponse<LoginResponseDto>.Ok(result));
    }

    /// <summary>
    /// Returns the caller's identity from JWT claims. Proves token issuance + role
    /// middleware work end-to-end without depending on a downstream domain endpoint.
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    public ActionResult<ApiResponse<UserDto>> Me()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var name = User.FindFirstValue(ClaimTypes.Name)!;
        var email = User.FindFirstValue(ClaimTypes.Email)!;
        var role = User.FindFirstValue(ClaimTypes.Role)!;

        return Ok(ApiResponse<UserDto>.Ok(new UserDto(Guid.Parse(id), name, email, role)));
    }
}
