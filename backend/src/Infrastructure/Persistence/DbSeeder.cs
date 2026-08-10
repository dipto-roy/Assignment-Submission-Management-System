using AssignmentSubmissionSystem.Application.Abstractions;
using AssignmentSubmissionSystem.Domain.Entities;
using AssignmentSubmissionSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSubmissionSystem.Infrastructure.Persistence;

/// <summary>
/// Seeds demo data so the evaluator never has to create rows by hand.
/// Demo credentials (documented in README):
///   Admin:   admin@lms.test   / Admin@12345
///   Teacher: teacher@lms.test / Teacher@12345
///   Student: student@lms.test / Student@12345
/// The passwords are public, so demo data must only ever run in Development.
/// Migration is kept separate so non-development environments can apply schema without demo users.
/// </summary>
public static class DbSeeder
{
    /// <summary>
    /// Advisory-lock key guarding start-up. Any 64-bit constant works as long as every process
    /// that migrates this database uses the same one.
    /// </summary>
    private const long StartupLockKey = 0x41534D53; // "ASMS"

    /// <summary>
    /// Applies migrations and, when <paramref name="includeDemoData"/> is set, the demo rows —
    /// holding a PostgreSQL advisory lock for the duration.
    /// </summary>
    /// <remarks>
    /// Without the lock, two processes starting against the same empty database race: both
    /// apply the initial migration ("relation already exists") and both pass the
    /// already-seeded check before either commits ("duplicate key on IX_Users_Email"). That
    /// happens for real with several API replicas, and every time the integration suite boots
    /// more than one test host. The lock is session-scoped, so it is released when the
    /// connection closes even if this method throws.
    /// </remarks>
    public static async Task MigrateAndSeedAsync(
        AppDbContext db,
        IPasswordHasher passwordHasher,
        bool includeDemoData,
        CancellationToken cancellationToken = default)
    {
        // Opened explicitly so the migration, the seed and the lock all share one connection —
        // an advisory lock taken on a pooled connection that is then returned would be lost.
        await db.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await db.Database.ExecuteSqlRawAsync(
                $"SELECT pg_advisory_lock({StartupLockKey})", cancellationToken);

            await MigrateAsync(db, cancellationToken);

            if (includeDemoData)
            {
                await SeedAsync(db, passwordHasher);
            }

            await db.Database.ExecuteSqlRawAsync(
                $"SELECT pg_advisory_unlock({StartupLockKey})", cancellationToken);
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    public static Task MigrateAsync(AppDbContext db, CancellationToken cancellationToken = default) =>
        db.Database.MigrateAsync(cancellationToken);

    public static async Task SeedAsync(AppDbContext db, IPasswordHasher passwordHasher)
    {
        if (await db.Users.AnyAsync())
        {
            return; // already seeded
        }

        var admin = new User
        {
            Name = "System Admin",
            Email = "admin@lms.test",
            PasswordHash = passwordHasher.Hash("Admin@12345"),
            Role = UserRole.Admin
        };

        var teacher = new User
        {
            Name = "Jane Teacher",
            Email = "teacher@lms.test",
            PasswordHash = passwordHasher.Hash("Teacher@12345"),
            Role = UserRole.Teacher
        };

        var student = new User
        {
            Name = "John Student",
            Email = "student@lms.test",
            PasswordHash = passwordHasher.Hash("Student@12345"),
            Role = UserRole.Student
        };

        db.Users.AddRange(admin, teacher, student);

        var schoolClass = new SchoolClass { Name = "Class 10", Section = "A" };
        db.Classes.Add(schoolClass);

        var subject = new Subject
        {
            Name = "Mathematics",
            Code = "MATH101",
            Class = schoolClass
        };
        db.Subjects.Add(subject);

        db.TeacherSubjects.Add(new TeacherSubject { Teacher = teacher, Subject = subject });
        db.StudentClasses.Add(new StudentClass { Student = student, Class = schoolClass });

        var assignment = new Assignment
        {
            Title = "Algebra Basics — Problem Set 1",
            Description = "Solve the attached algebra problems and submit your working.",
            Deadline = DateTime.UtcNow.AddDays(7),
            MaxMarks = 100,
            Status = AssignmentStatus.Published,
            Subject = subject,
            Teacher = teacher
        };
        db.Assignments.Add(assignment);

        await db.SaveChangesAsync();
    }
}
