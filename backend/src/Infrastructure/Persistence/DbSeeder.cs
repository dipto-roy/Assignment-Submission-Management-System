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
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.MigrateAsync();

        if (await db.Users.AnyAsync())
        {
            return; // already seeded
        }

        var admin = new User
        {
            Name = "System Admin",
            Email = "admin@lms.test",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@12345"),
            Role = UserRole.Admin
        };

        var teacher = new User
        {
            Name = "Jane Teacher",
            Email = "teacher@lms.test",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Teacher@12345"),
            Role = UserRole.Teacher
        };

        var student = new User
        {
            Name = "John Student",
            Email = "student@lms.test",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Student@12345"),
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
