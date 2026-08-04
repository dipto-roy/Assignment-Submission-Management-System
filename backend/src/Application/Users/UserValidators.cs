using AssignmentSubmissionSystem.Application.Users.Dtos;
using AssignmentSubmissionSystem.Domain.Enums;
using FluentValidation;

namespace AssignmentSubmissionSystem.Application.Users;

public sealed class CreateUserValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
            .WithMessage("Password must be at least 8 characters.");
        RuleFor(x => x.Role).IsInEnum();
        RuleFor(x => x.ClassId)
            .NotNull()
            .When(x => x.Role == UserRole.Student)
            .WithMessage("ClassId is required when creating a Student.");
    }
}

public sealed class UpdateUserValidator : AbstractValidator<UpdateUserDto>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Role).IsInEnum();
    }
}
