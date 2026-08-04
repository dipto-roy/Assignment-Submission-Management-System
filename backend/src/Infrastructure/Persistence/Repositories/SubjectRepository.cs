using AssignmentSubmissionSystem.Application.Abstractions;
using AssignmentSubmissionSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSubmissionSystem.Infrastructure.Persistence.Repositories;

public sealed class SubjectRepository(AppDbContext db) : ISubjectRepository
{
    public Task<Subject?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Subjects
            .Include(s => s.Class)
            .Include(s => s.TeacherSubjects).ThenInclude(ts => ts.Teacher)
            .SingleOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Subject>> FindAllAsync(CancellationToken cancellationToken) =>
        await db.Subjects
            .AsNoTracking()
            .Include(s => s.Class)
            .Include(s => s.TeacherSubjects).ThenInclude(ts => ts.Teacher)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Subject subject, CancellationToken cancellationToken)
    {
        db.Subjects.Add(subject);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task UpdateAsync(Subject subject, CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);

    public async Task DeleteAsync(Subject subject, CancellationToken cancellationToken)
    {
        db.Subjects.Remove(subject);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task AssignTeacherAsync(Guid subjectId, Guid teacherId, CancellationToken cancellationToken)
    {
        db.TeacherSubjects.Add(new TeacherSubject { SubjectId = subjectId, TeacherId = teacherId });
        await db.SaveChangesAsync(cancellationToken);
    }
}
