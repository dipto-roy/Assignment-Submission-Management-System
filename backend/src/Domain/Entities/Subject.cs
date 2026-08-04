namespace AssignmentSubmissionSystem.Domain.Entities;

public class Subject
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    public Guid ClassId { get; set; }
    public SchoolClass Class { get; set; } = null!;

    public ICollection<TeacherSubject> TeacherSubjects { get; set; } = new List<TeacherSubject>();
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
}
