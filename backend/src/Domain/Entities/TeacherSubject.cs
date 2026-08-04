namespace AssignmentSubmissionSystem.Domain.Entities;

/// <summary>Join entity: teacher assigned to teach a subject (which belongs to a class).</summary>
public class TeacherSubject
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TeacherId { get; set; }
    public User Teacher { get; set; } = null!;

    public Guid SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;
}
