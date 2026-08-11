using AssignmentSubmissionSystem.Application.Abstractions;
using AssignmentSubmissionSystem.Application.Common.Paging;
using AssignmentSubmissionSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSubmissionSystem.Infrastructure.Persistence.Repositories;

public sealed class ClassRepository(AppDbContext db) : IClassRepository
{
    public Task<SchoolClass?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Classes.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<PagedResult<SchoolClass>> FindAllAsync(PageQuery page, CancellationToken cancellationToken) =>
        db.Classes
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Section)
            .ToPagedResultAsync(page, cancellationToken);

    public async Task<IReadOnlyList<User>> FindStudentsAsync(Guid classId, CancellationToken cancellationToken) =>
        await db.StudentClasses
            .AsNoTracking()
            .Where(sc => sc.ClassId == classId)
            .Select(sc => sc.Student)
            .OrderBy(u => u.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<StudentClass>> FindEnrollmentsByStudentAsync(Guid studentId, CancellationToken cancellationToken) =>
        await db.StudentClasses.Where(sc => sc.StudentId == studentId).ToListAsync(cancellationToken);

    // Plan §11 assumes a student belongs to exactly one class, so enrolling moves them.
    public async Task EnrollStudentAsync(Guid classId, Guid studentId, CancellationToken cancellationToken)
    {
        var existing = await db.StudentClasses
            .Where(sc => sc.StudentId == studentId)
            .ToListAsync(cancellationToken);

        db.StudentClasses.RemoveRange(existing);
        db.StudentClasses.Add(new StudentClass { ClassId = classId, StudentId = studentId });

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveEnrollmentAsync(StudentClass enrollment, CancellationToken cancellationToken)
    {
        db.StudentClasses.Remove(enrollment);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddAsync(SchoolClass schoolClass, CancellationToken cancellationToken)
    {
        db.Classes.Add(schoolClass);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task UpdateAsync(SchoolClass schoolClass, CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);

    public async Task DeleteAsync(SchoolClass schoolClass, CancellationToken cancellationToken)
    {
        db.Classes.Remove(schoolClass);
        await db.SaveChangesAsync(cancellationToken);
    }
}
