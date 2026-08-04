using AssignmentSubmissionSystem.Application.Auth.Dtos;

namespace AssignmentSubmissionSystem.Application.Auth;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken);
}
