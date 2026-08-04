namespace AssignmentSubmissionSystem.Domain.Entities;

/// <summary>Represents a class/course, e.g. "Class 10 - Section A".</summary>
public class SchoolClass
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Section { get; set; }

    public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
    public ICollection<StudentClass> StudentClasses { get; set; } = new List<StudentClass>();
}
