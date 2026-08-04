using AssignmentSubmissionSystem.Application.Abstractions;
using AssignmentSubmissionSystem.Domain.Entities;
using AssignmentSubmissionSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSubmissionSystem.Infrastructure.Persistence.Repositories;

public sealed class AssignmentRepository(AppDbContext db) : IAssignmentRepository
{
    public Task<Assignment?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Assignments
            .Include(a => a.Subject).ThenInclude(s => s.Class)
            .Include(a => a.Teacher)
            .SingleOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Assignment>> FindAllAsync(CancellationToken cancellationToken) =>
        await db.Assignments
            .AsNoTracking()
            .Include(a => a.Subject).ThenInclude(s => s.Class)
            .Include(a => a.Teacher)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Assignment>> FindByTeacherAsync(Guid teacherId, CancellationToken cancellationToken) =>
        await db.Assignments
            .AsNoTracking()
            .Where(a => a.TeacherId == teacherId)
            .Include(a => a.Subject).ThenInclude(s => s.Class)
            .Include(a => a.Teacher)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Assignment>> FindPublishedForStudentAsync(Guid studentId, CancellationToken cancellationToken) =>
        await db.Assignments
            .AsNoTracking()
            .Where(a => a.Status == AssignmentStatus.Published &&
                        db.StudentClasses.Any(sc => sc.StudentId == studentId && sc.ClassId == a.Subject.ClassId))
            .Include(a => a.Subject).ThenInclude(s => s.Class)
            .Include(a => a.Teacher)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<bool> IsTeacherAssignedToSubjectAsync(Guid teacherId, Guid subjectId, CancellationToken cancellationToken) =>
        db.TeacherSubjects.AnyAsync(ts => ts.TeacherId == teacherId && ts.SubjectId == subjectId, cancellationToken);

    public Task<bool> IsStudentEnrolledInClassAsync(Guid studentId, Guid classId, CancellationToken cancellationToken) =>
        db.StudentClasses.AnyAsync(sc => sc.StudentId == studentId && sc.ClassId == classId, cancellationToken);

    public async Task AddAsync(Assignment assignment, CancellationToken cancellationToken)
    {
        db.Assignments.Add(assignment);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task UpdateAsync(Assignment assignment, CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);

    public async Task DeleteAsync(Assignment assignment, CancellationToken cancellationToken)
    {
        db.Assignments.Remove(assignment);
        await db.SaveChangesAsync(cancellationToken);
    }
}
