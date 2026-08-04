using AssignmentSubmissionSystem.Application.Abstractions;
using AssignmentSubmissionSystem.Application.Classes.Dtos;
using AssignmentSubmissionSystem.Application.Common.Exceptions;
using AssignmentSubmissionSystem.Domain.Entities;

namespace AssignmentSubmissionSystem.Application.Classes;

public interface IClassService
{
    Task<IReadOnlyList<ClassSummaryDto>> GetAllAsync(CancellationToken cancellationToken);

    Task<ClassSummaryDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ClassSummaryDto> CreateAsync(CreateClassDto dto, CancellationToken cancellationToken);

    Task<ClassSummaryDto> UpdateAsync(Guid id, UpdateClassDto dto, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class ClassService(IClassRepository classRepository) : IClassService
{
    public async Task<IReadOnlyList<ClassSummaryDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var classes = await classRepository.FindAllAsync(cancellationToken);
        return classes.Select(ToDto).ToList();
    }

    public async Task<ClassSummaryDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var schoolClass = await classRepository.FindByIdAsync(id, cancellationToken)
            ?? throw new NotFoundAppException($"Class {id} was not found.");
        return ToDto(schoolClass);
    }

    public async Task<ClassSummaryDto> CreateAsync(CreateClassDto dto, CancellationToken cancellationToken)
    {
        var schoolClass = new SchoolClass { Name = dto.Name, Section = dto.Section };
        await classRepository.AddAsync(schoolClass, cancellationToken);
        return ToDto(schoolClass);
    }

    public async Task<ClassSummaryDto> UpdateAsync(Guid id, UpdateClassDto dto, CancellationToken cancellationToken)
    {
        var schoolClass = await classRepository.FindByIdAsync(id, cancellationToken)
            ?? throw new NotFoundAppException($"Class {id} was not found.");

        schoolClass.Name = dto.Name;
        schoolClass.Section = dto.Section;

        await classRepository.UpdateAsync(schoolClass, cancellationToken);
        return ToDto(schoolClass);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var schoolClass = await classRepository.FindByIdAsync(id, cancellationToken)
            ?? throw new NotFoundAppException($"Class {id} was not found.");

        // Subjects cascade; a Subject with Assignments is Restrict-protected, so deleting a
        // class that still has assignments anywhere underneath it 409s via the middleware.
        await classRepository.DeleteAsync(schoolClass, cancellationToken);
    }

    private static ClassSummaryDto ToDto(SchoolClass schoolClass) =>
        new(schoolClass.Id, schoolClass.Name, schoolClass.Section);
}
