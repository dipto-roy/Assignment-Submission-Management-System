namespace AssignmentSubmissionSystem.Domain.Entities;

/// <summary>Join entity: student enrolled in a class.</summary>
public class StudentClass
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid StudentId { get; set; }
    public User Student { get; set; } = null!;

    public Guid ClassId { get; set; }
    public SchoolClass Class { get; set; } = null!;
}
