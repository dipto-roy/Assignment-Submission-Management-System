using AssignmentSubmissionSystem.Application.Abstractions;
using AssignmentSubmissionSystem.Application.Assignments.Dtos;
using AssignmentSubmissionSystem.Application.Common.Exceptions;
using AssignmentSubmissionSystem.Application.Common.Paging;
using AssignmentSubmissionSystem.Domain.Entities;
using AssignmentSubmissionSystem.Domain.Enums;

namespace AssignmentSubmissionSystem.Application.Assignments;

public interface IAssignmentService
{
    /// <summary>Role-filtered list: Admin sees all, Teacher sees own, Student sees Published for their class.</summary>
    Task<PagedResult<AssignmentSummaryDto>> GetAllAsync(Guid userId, UserRole role, AssignmentQuery query, CancellationToken cancellationToken);

    Task<AssignmentSummaryDto> GetByIdAsync(Guid id, Guid userId, UserRole role, CancellationToken cancellationToken);

    Task<AssignmentSummaryDto> CreateAsync(Guid teacherId, CreateAssignmentDto dto, CancellationToken cancellationToken);

    Task<AssignmentSummaryDto> UpdateAsync(Guid id, Guid teacherId, UpdateAssignmentDto dto, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, Guid teacherId, CancellationToken cancellationToken);

    Task<AssignmentSummaryDto> SetPublishStateAsync(Guid id, Guid teacherId, SetPublishStateDto dto, CancellationToken cancellationToken);
}

public sealed class AssignmentService(IAssignmentRepository assignmentRepository) : IAssignmentService
{
    public async Task<PagedResult<AssignmentSummaryDto>> GetAllAsync(
        Guid userId,
        UserRole role,
        AssignmentQuery query,
        CancellationToken cancellationToken)
    {
        var page = role switch
        {
            UserRole.Admin => await assignmentRepository.FindAllAsync(query, cancellationToken),
            UserRole.Teacher => await assignmentRepository.FindByTeacherAsync(userId, query, cancellationToken),
            UserRole.Student => await assignmentRepository.FindPublishedForStudentAsync(userId, query, cancellationToken),
            _ => throw new ForbiddenAppException("Unrecognized role.")
        };

        return page.Map(ToDto);
    }

    public async Task<AssignmentSummaryDto> GetByIdAsync(Guid id, Guid userId, UserRole role, CancellationToken cancellationToken)
    {
        var assignment = await assignmentRepository.FindByIdAsync(id, cancellationToken)
            ?? throw new NotFoundAppException($"Assignment {id} was not found.");

        await EnsureVisibleAsync(assignment, userId, role, cancellationToken);
        return ToDto(assignment);
    }

    public async Task<AssignmentSummaryDto> CreateAsync(Guid teacherId, CreateAssignmentDto dto, CancellationToken cancellationToken)
    {
        var isAssigned = await assignmentRepository.IsTeacherAssignedToSubjectAsync(teacherId, dto.SubjectId, cancellationToken);
        if (!isAssigned)
        {
            throw new ForbiddenAppException("You are not assigned to teach this subject.");
        }

        var assignment = new Assignment
        {
            Title = dto.Title,
            Description = dto.Description,
            Deadline = dto.Deadline,
            MaxMarks = dto.MaxMarks,
            SubjectId = dto.SubjectId,
            TeacherId = teacherId,
            Status = AssignmentStatus.Draft
        };

        await assignmentRepository.AddAsync(assignment, cancellationToken);

        // Re-fetch so Subject/Class/Teacher navigation properties are populated for the response.
        var created = await assignmentRepository.FindByIdAsync(assignment.Id, cancellationToken);
        return ToDto(created!);
    }

    public async Task<AssignmentSummaryDto> UpdateAsync(Guid id, Guid teacherId, UpdateAssignmentDto dto, CancellationToken cancellationToken)
    {
        var assignment = await FindOwnedAsync(id, teacherId, cancellationToken);

        assignment.Title = dto.Title;
        assignment.Description = dto.Description;
        assignment.Deadline = dto.Deadline;
        assignment.MaxMarks = dto.MaxMarks;
        assignment.UpdatedAt = DateTime.UtcNow;

        await assignmentRepository.UpdateAsync(assignment, cancellationToken);
        return ToDto(assignment);
    }

    public async Task DeleteAsync(Guid id, Guid teacherId, CancellationToken cancellationToken)
    {
        var assignment = await FindOwnedAsync(id, teacherId, cancellationToken);

        // Restrict FK on Submission.AssignmentId → 409 via middleware if submissions already exist.
        await assignmentRepository.DeleteAsync(assignment, cancellationToken);
    }

    public async Task<AssignmentSummaryDto> SetPublishStateAsync(
        Guid id,
        Guid teacherId,
        SetPublishStateDto dto,
        CancellationToken cancellationToken)
    {
        var assignment = await FindOwnedAsync(id, teacherId, cancellationToken);

        assignment.Status = dto.Publish ? AssignmentStatus.Published : AssignmentStatus.Draft;
        assignment.UpdatedAt = DateTime.UtcNow;

        await assignmentRepository.UpdateAsync(assignment, cancellationToken);
        return ToDto(assignment);
    }

    /// <summary>Loads the assignment and enforces teacher-ownership (business rule §7.5).</summary>
    private async Task<Assignment> FindOwnedAsync(Guid id, Guid teacherId, CancellationToken cancellationToken)
    {
        var assignment = await assignmentRepository.FindByIdAsync(id, cancellationToken)
            ?? throw new NotFoundAppException($"Assignment {id} was not found.");

        if (assignment.TeacherId != teacherId)
        {
            throw new ForbiddenAppException("You do not own this assignment.");
        }

        return assignment;
    }

    private async Task EnsureVisibleAsync(Assignment assignment, Guid userId, UserRole role, CancellationToken cancellationToken)
    {
        switch (role)
        {
            case UserRole.Admin:
                return;
            case UserRole.Teacher when assignment.TeacherId == userId:
                return;
            case UserRole.Teacher:
                throw new ForbiddenAppException("You do not own this assignment.");
            case UserRole.Student:
                {
                    var visible = assignment.Status == AssignmentStatus.Published &&
                        await assignmentRepository.IsStudentEnrolledInClassAsync(userId, assignment.Subject.ClassId, cancellationToken);
                    if (!visible)
                    {
                        throw new ForbiddenAppException("This assignment is not available to you.");
                    }
                    return;
                }
            default:
                throw new ForbiddenAppException("Unrecognized role.");
        }
    }

    private static AssignmentSummaryDto ToDto(Assignment assignment) => new(
        assignment.Id,
        assignment.Title,
        assignment.Description,
        assignment.Deadline,
        assignment.MaxMarks,
        assignment.Status.ToString(),
        assignment.SubjectId,
        assignment.Subject.Name,
        assignment.Subject.ClassId,
        assignment.Subject.Class.Name,
        assignment.TeacherId,
        assignment.Teacher.Name,
        assignment.CreatedAt,
        assignment.UpdatedAt);
}
