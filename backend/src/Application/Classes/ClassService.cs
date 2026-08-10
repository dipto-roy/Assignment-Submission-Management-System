using AssignmentSubmissionSystem.Application.Abstractions;
using AssignmentSubmissionSystem.Application.Classes.Dtos;
using AssignmentSubmissionSystem.Application.Common.Exceptions;
using AssignmentSubmissionSystem.Domain.Entities;
using AssignmentSubmissionSystem.Domain.Enums;

namespace AssignmentSubmissionSystem.Application.Classes;

public interface IClassService
{
    Task<IReadOnlyList<ClassSummaryDto>> GetAllAsync(CancellationToken cancellationToken);

    Task<ClassSummaryDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ClassSummaryDto> CreateAsync(CreateClassDto dto, CancellationToken cancellationToken);

    Task<ClassSummaryDto> UpdateAsync(Guid id, UpdateClassDto dto, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<EnrolledStudentDto>> GetStudentsAsync(Guid classId, CancellationToken cancellationToken);

    /// <summary>Enrolls a student, moving them out of any class they were previously in (plan §11).</summary>
    Task<EnrolledStudentDto> EnrollStudentAsync(Guid classId, EnrollStudentDto dto, CancellationToken cancellationToken);

    Task UnenrollStudentAsync(Guid classId, Guid studentId, CancellationToken cancellationToken);
}

public sealed class ClassService(IClassRepository classRepository, IUserRepository userRepository) : IClassService
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

    public async Task<IReadOnlyList<EnrolledStudentDto>> GetStudentsAsync(Guid classId, CancellationToken cancellationToken)
    {
        await EnsureClassExistsAsync(classId, cancellationToken);

        var students = await classRepository.FindStudentsAsync(classId, cancellationToken);
        return students.Select(s => new EnrolledStudentDto(s.Id, s.Name, s.Email)).ToList();
    }

    public async Task<EnrolledStudentDto> EnrollStudentAsync(Guid classId, EnrollStudentDto dto, CancellationToken cancellationToken)
    {
        await EnsureClassExistsAsync(classId, cancellationToken);

        var student = await userRepository.FindByIdAsync(dto.StudentId, cancellationToken)
            ?? throw new NotFoundAppException($"User {dto.StudentId} was not found.");

        if (student.Role != UserRole.Student)
        {
            throw new BadRequestAppException("Only users with the Student role can be enrolled in a class.");
        }

        var enrollments = await classRepository.FindEnrollmentsByStudentAsync(student.Id, cancellationToken);
        if (enrollments.Any(e => e.ClassId == classId))
        {
            throw new ConflictAppException("This student is already enrolled in this class.");
        }

        await classRepository.EnrollStudentAsync(classId, student.Id, cancellationToken);
        return new EnrolledStudentDto(student.Id, student.Name, student.Email);
    }

    public async Task UnenrollStudentAsync(Guid classId, Guid studentId, CancellationToken cancellationToken)
    {
        await EnsureClassExistsAsync(classId, cancellationToken);

        var enrollments = await classRepository.FindEnrollmentsByStudentAsync(studentId, cancellationToken);
        var enrollment = enrollments.SingleOrDefault(e => e.ClassId == classId)
            ?? throw new NotFoundAppException("This student is not enrolled in this class.");

        await classRepository.RemoveEnrollmentAsync(enrollment, cancellationToken);
    }

    private async Task EnsureClassExistsAsync(Guid classId, CancellationToken cancellationToken) =>
        _ = await classRepository.FindByIdAsync(classId, cancellationToken)
            ?? throw new NotFoundAppException($"Class {classId} was not found.");

    private static ClassSummaryDto ToDto(SchoolClass schoolClass) =>
        new(schoolClass.Id, schoolClass.Name, schoolClass.Section);
}
