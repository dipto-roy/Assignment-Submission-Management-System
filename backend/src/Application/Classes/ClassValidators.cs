using AssignmentSubmissionSystem.Application.Classes.Dtos;
using FluentValidation;

namespace AssignmentSubmissionSystem.Application.Classes;

public sealed class CreateClassValidator : AbstractValidator<CreateClassDto>
{
    public CreateClassValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Section).MaximumLength(50);
    }
}

public sealed class UpdateClassValidator : AbstractValidator<UpdateClassDto>
{
    public UpdateClassValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Section).MaximumLength(50);
    }
}
