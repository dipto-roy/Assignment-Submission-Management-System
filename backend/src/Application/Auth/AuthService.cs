using AssignmentSubmissionSystem.Application.Abstractions;
using AssignmentSubmissionSystem.Application.Auth.Dtos;
using AssignmentSubmissionSystem.Application.Common.Exceptions;

namespace AssignmentSubmissionSystem.Application.Auth;

public sealed class AuthService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService) : IAuthService
{
    // Generic message on both "no such user" and "wrong password" — never reveal
    // which one failed, to avoid leaking valid account emails to an attacker.
    private const string InvalidCredentialsMessage = "Invalid email or password.";

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByEmailAsync(request.Email, cancellationToken)
            ?? throw new UnauthorizedAppException(InvalidCredentialsMessage);

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAppException(InvalidCredentialsMessage);
        }

        var token = tokenService.GenerateToken(user);

        return new LoginResponseDto(
            token.Value,
            token.ExpiresAtUtc,
            new UserDto(user.Id, user.Name, user.Email, user.Role.ToString()));
    }
}
