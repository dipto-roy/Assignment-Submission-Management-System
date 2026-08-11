using AssignmentSubmissionSystem.Domain.Enums;

namespace AssignmentSubmissionSystem.Domain.Entities;

public class Submission
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AssignmentId { get; set; }
    public Assignment Assignment { get; set; } = null!;

    public Guid StudentId { get; set; }
    public User Student { get; set; } = null!;

    public string Content { get; set; } = string.Empty;
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Submitted;

    public int? Marks { get; set; }
    public string? Feedback { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? GradedAt { get; set; }

    /// <summary>Files the student uploaded alongside the text content.</summary>
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
}
