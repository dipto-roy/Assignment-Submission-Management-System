using AssignmentSubmissionSystem.Application.Abstractions;
using AssignmentSubmissionSystem.Application.Common.Exceptions;
using AssignmentSubmissionSystem.Application.Common.Paging;
using AssignmentSubmissionSystem.Application.Subjects.Dtos;
using AssignmentSubmissionSystem.Domain.Entities;
using AssignmentSubmissionSystem.Domain.Enums;

namespace AssignmentSubmissionSystem.Application.Subjects;

public interface ISubjectService
{
    Task<PagedResult<SubjectSummaryDto>> GetAllAsync(PageQuery page, CancellationToken cancellationToken);

    Task<SubjectSummaryDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<SubjectSummaryDto> CreateAsync(CreateSubjectDto dto, CancellationToken cancellationToken);

    Task<SubjectSummaryDto> UpdateAsync(Guid id, UpdateSubjectDto dto, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task<SubjectSummaryDto> AssignTeacherAsync(Guid subjectId, AssignTeacherDto dto, CancellationToken cancellationToken);
}

public sealed class SubjectService(
    ISubjectRepository subjectRepository,
    IClassRepository classRepository,
    IUserRepository userRepository) : ISubjectService
{
    public async Task<PagedResult<SubjectSummaryDto>> GetAllAsync(PageQuery page, CancellationToken cancellationToken)
    {
        var subjects = await subjectRepository.FindAllAsync(page, cancellationToken);
        return subjects.Map(ToDto);
    }

    public async Task<SubjectSummaryDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var subject = await subjectRepository.FindByIdAsync(id, cancellationToken)
            ?? throw new NotFoundAppException($"Subject {id} was not found.");
        return ToDto(subject);
    }

    public async Task<SubjectSummaryDto> CreateAsync(CreateSubjectDto dto, CancellationToken cancellationToken)
    {
        _ = await classRepository.FindByIdAsync(dto.ClassId, cancellationToken)
            ?? throw new NotFoundAppException($"Class {dto.ClassId} was not found.");

        var subject = new Subject { Name = dto.Name, Code = dto.Code, ClassId = dto.ClassId };
        await subjectRepository.AddAsync(subject, cancellationToken);

        // Re-fetch so ClassName/Teachers navigation properties are populated for the response.
        var created = await subjectRepository.FindByIdAsync(subject.Id, cancellationToken);
        return ToDto(created!);
    }

    public async Task<SubjectSummaryDto> UpdateAsync(Guid id, UpdateSubjectDto dto, CancellationToken cancellationToken)
    {
        var subject = await subjectRepository.FindByIdAsync(id, cancellationToken)
            ?? throw new NotFoundAppException($"Subject {id} was not found.");

        subject.Name = dto.Name;
        subject.Code = dto.Code;

        await subjectRepository.UpdateAsync(subject, cancellationToken);
        return ToDto(subject);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var subject = await subjectRepository.FindByIdAsync(id, cancellationToken)
            ?? throw new NotFoundAppException($"Subject {id} was not found.");

        // Restrict FK on Assignment.SubjectId → 409 via middleware if assignments still exist.
        await subjectRepository.DeleteAsync(subject, cancellationToken);
    }

    public async Task<SubjectSummaryDto> AssignTeacherAsync(
        Guid subjectId,
        AssignTeacherDto dto,
        CancellationToken cancellationToken)
    {
        var subject = await subjectRepository.FindByIdAsync(subjectId, cancellationToken)
            ?? throw new NotFoundAppException($"Subject {subjectId} was not found.");

        var teacher = await userRepository.FindByIdAsync(dto.TeacherId, cancellationToken)
            ?? throw new NotFoundAppException($"User {dto.TeacherId} was not found.");

        if (teacher.Role != UserRole.Teacher)
        {
            throw new BadRequestAppException($"User {dto.TeacherId} is not a Teacher.");
        }

        // Unique (TeacherId, SubjectId) index → 409 via middleware if already assigned.
        await subjectRepository.AssignTeacherAsync(subjectId, dto.TeacherId, cancellationToken);

        var updated = await subjectRepository.FindByIdAsync(subjectId, cancellationToken);
        return ToDto(updated!);
    }

    private static SubjectSummaryDto ToDto(Subject subject) => new(
        subject.Id,
        subject.Name,
        subject.Code,
        subject.ClassId,
        subject.Class.Name,
        subject.TeacherSubjects.Select(ts => new TeacherRefDto(ts.Teacher.Id, ts.Teacher.Name, ts.Teacher.Email)).ToList());
}
