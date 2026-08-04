using FluentValidation.Results;

namespace AssignmentSubmissionSystem.Application.Common;

public static class ValidationResultExtensions
{
    public static string ToErrorMessage(this ValidationResult result) =>
        string.Join(" ", result.Errors.Select(e => e.ErrorMessage));
}
