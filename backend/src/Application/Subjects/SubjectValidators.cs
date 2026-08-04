using AssignmentSubmissionSystem.Application.Subjects.Dtos;
using FluentValidation;

namespace AssignmentSubmissionSystem.Application.Subjects;

public sealed class CreateSubjectValidator : AbstractValidator<CreateSubjectDto>
{
    public CreateSubjectValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ClassId).NotEmpty();
    }
}

public sealed class UpdateSubjectValidator : AbstractValidator<UpdateSubjectDto>
{
    public UpdateSubjectValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
    }
}

public sealed class AssignTeacherValidator : AbstractValidator<AssignTeacherDto>
{
    public AssignTeacherValidator()
    {
        RuleFor(x => x.TeacherId).NotEmpty();
    }
}
