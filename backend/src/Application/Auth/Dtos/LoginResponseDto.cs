namespace AssignmentSubmissionSystem.Application.Auth.Dtos;

public sealed record LoginResponseDto(string Token, DateTime ExpiresAtUtc, UserDto User);
