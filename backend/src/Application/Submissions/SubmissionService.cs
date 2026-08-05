using AssignmentSubmissionSystem.Application.Abstractions;
using AssignmentSubmissionSystem.Application.Common.Exceptions;
using AssignmentSubmissionSystem.Application.Submissions.Dtos;
using AssignmentSubmissionSystem.Domain.Entities;
using AssignmentSubmissionSystem.Domain.Enums;

namespace AssignmentSubmissionSystem.Application.Submissions;

public interface ISubmissionService
{
    /// <summary>Student's own submissions only (business rule §7.4).</summary>
    Task<IReadOnlyList<SubmissionSummaryDto>> GetMineAsync(Guid studentId, CancellationToken cancellationToken);

    Task<SubmissionSummaryDto> SubmitAsync(Guid assignmentId, Guid studentId, CreateSubmissionDto dto, CancellationToken cancellationToken);

    Task<SubmissionSummaryDto> UpdateAsync(Guid submissionId, Guid studentId, UpdateSubmissionDto dto, CancellationToken cancellationToken);

    /// <summary>Every submission for one assignment — Admin sees any, Teacher only their own assignment (business rule §7.5).</summary>
    Task<IReadOnlyList<SubmissionDetailDto>> GetForAssignmentAsync(Guid assignmentId, Guid userId, UserRole role, CancellationToken cancellationToken);

    Task<SubmissionDetailDto> GradeAsync(Guid submissionId, Guid teacherId, GradeSubmissionDto dto, CancellationToken cancellationToken);

    Task<SubmissionDetailDto> SetStatusAsync(Guid submissionId, Guid teacherId, SetSubmissionStatusDto dto, CancellationToken cancellationToken);
}

public sealed class SubmissionService(
    ISubmissionRepository submissionRepository,
    IAssignmentRepository assignmentRepository) : ISubmissionService
{
    public async Task<IReadOnlyList<SubmissionSummaryDto>> GetMineAsync(Guid studentId, CancellationToken cancellationToken)
    {
        var submissions = await submissionRepository.FindByStudentAsync(studentId, cancellationToken);
        return submissions.Select(ToDto).ToList();
    }

    public async Task<SubmissionSummaryDto> SubmitAsync(
        Guid assignmentId,
        Guid studentId,
        CreateSubmissionDto dto,
        CancellationToken cancellationToken)
    {
        var assignment = await assignmentRepository.FindByIdAsync(assignmentId, cancellationToken)
            ?? throw new NotFoundAppException($"Assignment {assignmentId} was not found.");

        if (assignment.Status != AssignmentStatus.Published)
        {
            throw new ForbiddenAppException("This assignment is not available for submission.");
        }

        var enrolled = await assignmentRepository.IsStudentEnrolledInClassAsync(studentId, assignment.Subject.ClassId, cancellationToken);
        if (!enrolled)
        {
            throw new ForbiddenAppException("This assignment is not available to you.");
        }

        // Business rule §7.1: no submission accepted after the deadline.
        if (DateTime.UtcNow > assignment.Deadline)
        {
            throw new BadRequestAppException("The deadline for this assignment has passed.");
        }

        // Business rule §7.9: one submission row per (assignment, student) — resubmit via PUT, not POST.
        var existing = await submissionRepository.FindByAssignmentAndStudentAsync(assignmentId, studentId, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictAppException("You have already submitted this assignment. Use update instead.");
        }

        var submission = new Submission
        {
            AssignmentId = assignmentId,
            StudentId = studentId,
            Content = dto.Content,
            Status = SubmissionStatus.Submitted,
            SubmittedAt = DateTime.UtcNow
        };

        await submissionRepository.AddAsync(submission, cancellationToken);

        submission.Assignment = assignment;
        return ToDto(submission);
    }

    public async Task<SubmissionSummaryDto> UpdateAsync(
        Guid submissionId,
        Guid studentId,
        UpdateSubmissionDto dto,
        CancellationToken cancellationToken)
    {
        var submission = await submissionRepository.FindByIdAsync(submissionId, cancellationToken)
            ?? throw new NotFoundAppException($"Submission {submissionId} was not found.");

        // Business rule §7.4: a student may only touch their own submission.
        if (submission.StudentId != studentId)
        {
            throw new ForbiddenAppException("You do not own this submission.");
        }

        // Business rule §7.2: locked once the assignment deadline has passed.
        if (DateTime.UtcNow > submission.Assignment.Deadline)
        {
            throw new BadRequestAppException("The deadline for this assignment has passed; the submission is locked.");
        }

        submission.Content = dto.Content;
        submission.UpdatedAt = DateTime.UtcNow;

        await submissionRepository.UpdateAsync(submission, cancellationToken);
        return ToDto(submission);
    }

    public async Task<IReadOnlyList<SubmissionDetailDto>> GetForAssignmentAsync(
        Guid assignmentId,
        Guid userId,
        UserRole role,
        CancellationToken cancellationToken)
    {
        var assignment = await assignmentRepository.FindByIdAsync(assignmentId, cancellationToken)
            ?? throw new NotFoundAppException($"Assignment {assignmentId} was not found.");

        // Business rule §7.5: only the owning teacher (or an Admin) may review submissions.
        if (role != UserRole.Admin && !(role == UserRole.Teacher && assignment.TeacherId == userId))
        {
            throw new ForbiddenAppException("You do not own this assignment.");
        }

        var submissions = await submissionRepository.FindByAssignmentAsync(assignmentId, cancellationToken);
        return submissions.Select(ToDetailDto).ToList();
    }

    public async Task<SubmissionDetailDto> GradeAsync(
        Guid submissionId,
        Guid teacherId,
        GradeSubmissionDto dto,
        CancellationToken cancellationToken)
    {
        var submission = await FindOwnedByTeacherAsync(submissionId, teacherId, cancellationToken);

        // Business rule §7.6: awarded marks may never exceed the assignment's maximum.
        if (dto.Marks > submission.Assignment.MaxMarks)
        {
            throw new BadRequestAppException($"Marks cannot exceed the assignment maximum of {submission.Assignment.MaxMarks}.");
        }

        submission.Marks = dto.Marks;
        submission.Feedback = dto.Feedback;
        submission.Status = SubmissionStatus.Graded;
        submission.GradedAt = DateTime.UtcNow;

        await submissionRepository.UpdateAsync(submission, cancellationToken);
        return ToDetailDto(submission);
    }

    public async Task<SubmissionDetailDto> SetStatusAsync(
        Guid submissionId,
        Guid teacherId,
        SetSubmissionStatusDto dto,
        CancellationToken cancellationToken)
    {
        var submission = await FindOwnedByTeacherAsync(submissionId, teacherId, cancellationToken);

        // Graded/Returned both assert "this work has been marked", so marks must already exist.
        var requiresMarks = dto.Status is SubmissionStatus.Graded or SubmissionStatus.Returned;
        if (requiresMarks && submission.Marks is null)
        {
            throw new BadRequestAppException($"Grade the submission before setting its status to {dto.Status}.");
        }

        submission.Status = dto.Status;

        await submissionRepository.UpdateAsync(submission, cancellationToken);
        return ToDetailDto(submission);
    }

    /// <summary>Loads a submission and enforces that the caller owns its parent assignment (business rule §7.5).</summary>
    private async Task<Submission> FindOwnedByTeacherAsync(Guid submissionId, Guid teacherId, CancellationToken cancellationToken)
    {
        var submission = await submissionRepository.FindByIdAsync(submissionId, cancellationToken)
            ?? throw new NotFoundAppException($"Submission {submissionId} was not found.");

        if (submission.Assignment.TeacherId != teacherId)
        {
            throw new ForbiddenAppException("You do not own the assignment this submission belongs to.");
        }

        return submission;
    }

    private static SubmissionDetailDto ToDetailDto(Submission submission) => new(
        submission.Id,
        submission.AssignmentId,
        submission.Assignment.Title,
        submission.Assignment.Deadline,
        submission.Assignment.MaxMarks,
        submission.StudentId,
        submission.Student.Name,
        submission.Student.Email,
        submission.Content,
        submission.Status.ToString(),
        submission.Marks,
        submission.Feedback,
        submission.SubmittedAt,
        submission.UpdatedAt,
        submission.GradedAt);

    private static SubmissionSummaryDto ToDto(Submission submission) => new(
        submission.Id,
        submission.AssignmentId,
        submission.Assignment.Title,
        submission.Assignment.Deadline,
        submission.StudentId,
        submission.Content,
        submission.Status.ToString(),
        submission.Marks,
        submission.Feedback,
        submission.SubmittedAt,
        submission.UpdatedAt,
        submission.GradedAt);
}
