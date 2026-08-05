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

public sealed class GradeSubmissionValidator : AbstractValidator<GradeSubmissionDto>
{
    public GradeSubmissionValidator()
    {
        // The upper bound is the assignment's MaxMarks, which is only known at service level (business rule §7.6).
        RuleFor(x => x.Marks).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Feedback).MaximumLength(5_000);
    }
}

public sealed class SetSubmissionStatusValidator : AbstractValidator<SetSubmissionStatusDto>
{
    public SetSubmissionStatusValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
    }
}
