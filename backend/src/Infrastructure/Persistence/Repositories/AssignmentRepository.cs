using AssignmentSubmissionSystem.Application.Abstractions;
using AssignmentSubmissionSystem.Application.Assignments.Dtos;
using AssignmentSubmissionSystem.Application.Common.Paging;
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

    public Task<PagedResult<Assignment>> FindAllAsync(AssignmentQuery query, CancellationToken cancellationToken) =>
        Paged(db.Assignments.AsNoTracking(), query, cancellationToken);

    public Task<PagedResult<Assignment>> FindByTeacherAsync(Guid teacherId, AssignmentQuery query, CancellationToken cancellationToken) =>
        Paged(db.Assignments.AsNoTracking().Where(a => a.TeacherId == teacherId), query, cancellationToken);

    public Task<PagedResult<Assignment>> FindPublishedForStudentAsync(Guid studentId, AssignmentQuery query, CancellationToken cancellationToken) =>
        Paged(
            db.Assignments
                .AsNoTracking()
                .Where(a => a.Status == AssignmentStatus.Published &&
                            db.StudentClasses.Any(sc => sc.StudentId == studentId && sc.ClassId == a.Subject.ClassId)),
            query,
            cancellationToken);

    /// <summary>
    /// Applies the caller-supplied filters on top of an already role-scoped query, so a filter
    /// can only narrow what the role allows — never widen it (business rules §7.3, §7.5).
    /// </summary>
    private static Task<PagedResult<Assignment>> Paged(
        IQueryable<Assignment> scoped,
        AssignmentQuery query,
        CancellationToken cancellationToken)
    {
        if (query.Status is { } status)
        {
            scoped = scoped.Where(a => a.Status == status);
        }

        if (query.SubjectId is { } subjectId)
        {
            scoped = scoped.Where(a => a.SubjectId == subjectId);
        }

        if (query.ClassId is { } classId)
        {
            scoped = scoped.Where(a => a.Subject.ClassId == classId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            scoped = scoped.Where(a => EF.Functions.ILike(a.Title, pattern));
        }

        return scoped
            .Include(a => a.Subject).ThenInclude(s => s.Class)
            .Include(a => a.Teacher)
            .OrderByDescending(a => a.CreatedAt)
            .ToPagedResultAsync(query, cancellationToken);
    }

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
