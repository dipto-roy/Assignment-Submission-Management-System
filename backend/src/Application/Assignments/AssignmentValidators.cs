using AssignmentSubmissionSystem.Application.Assignments.Dtos;
using FluentValidation;

namespace AssignmentSubmissionSystem.Application.Assignments;

public sealed class CreateAssignmentValidator : AbstractValidator<CreateAssignmentDto>
{
    public CreateAssignmentValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.Deadline).GreaterThan(DateTime.UtcNow).WithMessage("Deadline must be in the future.");
        RuleFor(x => x.MaxMarks).GreaterThan(0);
        RuleFor(x => x.SubjectId).NotEmpty();
    }
}

public sealed class UpdateAssignmentValidator : AbstractValidator<UpdateAssignmentDto>
{
    public UpdateAssignmentValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.Deadline).GreaterThan(DateTime.UtcNow).WithMessage("Deadline must be in the future.");
        RuleFor(x => x.MaxMarks).GreaterThan(0);
    }
}
