using AssignmentSubmissionSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssignmentSubmissionSystem.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<SchoolClass> Classes => Set<SchoolClass>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<TeacherSubject> TeacherSubjects => Set<TeacherSubject>();
    public DbSet<StudentClass> StudentClasses => Set<StudentClass>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Name).IsRequired().HasMaxLength(200);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasOne(s => s.Class)
                .WithMany(c => c.Subjects)
                .HasForeignKey(s => s.ClassId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TeacherSubject>(entity =>
        {
            entity.HasIndex(ts => new { ts.TeacherId, ts.SubjectId }).IsUnique();

            entity.HasOne(ts => ts.Teacher)
                .WithMany(u => u.TeacherSubjects)
                .HasForeignKey(ts => ts.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ts => ts.Subject)
                .WithMany(s => s.TeacherSubjects)
                .HasForeignKey(ts => ts.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StudentClass>(entity =>
        {
            entity.HasIndex(sc => new { sc.StudentId, sc.ClassId }).IsUnique();

            entity.HasOne(sc => sc.Student)
                .WithMany(u => u.StudentClasses)
                .HasForeignKey(sc => sc.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(sc => sc.Class)
                .WithMany(c => c.StudentClasses)
                .HasForeignKey(sc => sc.ClassId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Assignment>(entity =>
        {
            entity.Property(a => a.Title).IsRequired().HasMaxLength(300);
            entity.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);

            entity.HasOne(a => a.Subject)
                .WithMany(s => s.Assignments)
                .HasForeignKey(a => a.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Teacher)
                .WithMany(u => u.AssignmentsCreated)
                .HasForeignKey(a => a.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Submission>(entity =>
        {
            // One submission per student per assignment.
            entity.HasIndex(s => new { s.AssignmentId, s.StudentId }).IsUnique();
            entity.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);

            entity.HasOne(s => s.Assignment)
                .WithMany(a => a.Submissions)
                .HasForeignKey(s => s.AssignmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.Student)
                .WithMany(u => u.Submissions)
                .HasForeignKey(s => s.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Attachment>(entity =>
        {
            entity.Property(a => a.FileName).IsRequired().HasMaxLength(255);
            entity.Property(a => a.ContentType).IsRequired().HasMaxLength(150);
            entity.Property(a => a.StorageKey).IsRequired().HasMaxLength(500);
            entity.Property(a => a.StorageProvider).IsRequired().HasMaxLength(30);

            // An attachment hangs off exactly one owner. Without this the table would happily
            // accept an orphan (both null) or a row claiming to belong to both.
            entity.ToTable(t => t.HasCheckConstraint(
                "CK_Attachments_ExactlyOneOwner",
                @"(""AssignmentId"" IS NOT NULL AND ""SubmissionId"" IS NULL)
                  OR (""AssignmentId"" IS NULL AND ""SubmissionId"" IS NOT NULL)"));

            // Every read is "the files for this owner", so both owner columns are indexed.
            entity.HasIndex(a => a.AssignmentId);
            entity.HasIndex(a => a.SubmissionId);

            // Cascade: deleting the owner removes its attachment rows. The stored bytes are
            // deleted separately by the service, which is why deletes go through it.
            entity.HasOne(a => a.Assignment)
                .WithMany(x => x.Attachments)
                .HasForeignKey(a => a.AssignmentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.Submission)
                .WithMany(x => x.Attachments)
                .HasForeignKey(a => a.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Restrict, matching the other user-owned relationships: an uploader is never
            // removed out from under the file that records who uploaded it.
            entity.HasOne(a => a.UploadedBy)
                .WithMany()
                .HasForeignKey(a => a.UploadedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.Property(n => n.Type).HasConversion<string>().HasMaxLength(30);
            entity.Property(n => n.Title).IsRequired().HasMaxLength(200);
            entity.Property(n => n.Message).IsRequired().HasMaxLength(1000);

            // The bell queries "my notifications, newest first" and "my unread count" on every
            // poll; this index serves both.
            entity.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt });

            entity.HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // SetNull, not Cascade: deleting an assignment should drop the deep link, not
            // erase the user's record that they were once notified.
            entity.HasOne(n => n.Assignment)
                .WithMany()
                .HasForeignKey(n => n.AssignmentId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(n => n.Submission)
                .WithMany()
                .HasForeignKey(n => n.SubmissionId)
                .OnDelete(DeleteBehavior.SetNull);

            // The deadline reminder worker re-scans on every tick and must not notify the same
            // student about the same assignment twice. A filtered unique index makes the
            // database the arbiter, so overlapping ticks or multiple replicas cannot duplicate.
            // Only DeadlineApproaching is constrained: the other types are legitimately repeatable.
            entity.HasIndex(n => new { n.UserId, n.AssignmentId })
                .IsUnique()
                .HasFilter(@"""Type"" = 'DeadlineApproaching'")
                .HasDatabaseName("IX_Notifications_DeadlineReminder_Once");
        });

        modelBuilder.Entity<User>().Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
    }
}
