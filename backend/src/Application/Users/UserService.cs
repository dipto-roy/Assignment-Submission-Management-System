using AssignmentSubmissionSystem.Application.Abstractions;
using AssignmentSubmissionSystem.Application.Common.Exceptions;
using AssignmentSubmissionSystem.Application.Users.Dtos;
using AssignmentSubmissionSystem.Domain.Entities;
using AssignmentSubmissionSystem.Domain.Enums;

namespace AssignmentSubmissionSystem.Application.Users;

public interface IUserService
{
    Task<IReadOnlyList<UserSummaryDto>> GetAllAsync(CancellationToken cancellationToken);

    Task<UserSummaryDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<UserSummaryDto> CreateAsync(CreateUserDto dto, CancellationToken cancellationToken);

    Task<UserSummaryDto> UpdateAsync(Guid id, UpdateUserDto dto, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class UserService(
    IUserRepository userRepository,
    IClassRepository classRepository,
    IPasswordHasher passwordHasher) : IUserService
{
    public async Task<IReadOnlyList<UserSummaryDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var users = await userRepository.FindAllAsync(cancellationToken);
        return users.Select(ToDto).ToList();
    }

    public async Task<UserSummaryDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(id, cancellationToken)
            ?? throw new NotFoundAppException($"User {id} was not found.");
        return ToDto(user);
    }

    public async Task<UserSummaryDto> CreateAsync(CreateUserDto dto, CancellationToken cancellationToken)
    {
        if (await userRepository.ExistsByEmailAsync(dto.Email, excludeUserId: null, cancellationToken))
        {
            throw new ConflictAppException($"A user with email '{dto.Email}' already exists.");
        }

        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            PasswordHash = passwordHasher.Hash(dto.Password),
            Role = dto.Role
        };

        if (dto.Role == UserRole.Student && dto.ClassId is { } classId)
        {
            _ = await classRepository.FindByIdAsync(classId, cancellationToken)
                ?? throw new NotFoundAppException($"Class {classId} was not found.");
            user.StudentClasses.Add(new StudentClass { ClassId = classId });
        }

        await userRepository.AddAsync(user, cancellationToken);
        return ToDto(user);
    }

    public async Task<UserSummaryDto> UpdateAsync(Guid id, UpdateUserDto dto, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(id, cancellationToken)
            ?? throw new NotFoundAppException($"User {id} was not found.");

        if (await userRepository.ExistsByEmailAsync(dto.Email, excludeUserId: id, cancellationToken))
        {
            throw new ConflictAppException($"A user with email '{dto.Email}' already exists.");
        }

        user.Name = dto.Name;
        user.Email = dto.Email;
        user.Role = dto.Role;

        await userRepository.UpdateAsync(user, cancellationToken);
        return ToDto(user);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(id, cancellationToken)
            ?? throw new NotFoundAppException($"User {id} was not found.");

        // DbUpdateException from FK Restrict (e.g. teacher has assignments, student has
        // submissions) is caught by ExceptionHandlingMiddleware and mapped to 409.
        await userRepository.DeleteAsync(user, cancellationToken);
    }

    private static UserSummaryDto ToDto(User user) =>
        new(user.Id, user.Name, user.Email, user.Role.ToString(), user.CreatedAt);
}
