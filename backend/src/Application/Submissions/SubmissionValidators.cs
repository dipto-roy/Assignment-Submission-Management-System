using AssignmentSubmissionSystem.Application.Submissions.Dtos;
using FluentValidation;

namespace AssignmentSubmissionSystem.Application.Submissions;

public sealed class CreateSubmissionValidator : AbstractValidator<CreateSubmissionDto>
{
    public CreateSubmissionValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(20_000);
    }
}

public sealed class UpdateSubmissionValidator : AbstractValidator<UpdateSubmissionDto>
{
    public UpdateSubmissionValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(20_000);
    }
}
