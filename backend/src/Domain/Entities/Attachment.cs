namespace AssignmentSubmissionSystem.Domain.Entities;

/// <summary>
/// A file uploaded against exactly one owner: an assignment (the teacher's brief) or a
/// submission (the student's work).
/// </summary>
/// <remarks>
/// Two nullable foreign keys rather than a polymorphic (OwnerType, OwnerId) pair, so the
/// database still enforces referential integrity and cascades on both sides. A check
/// constraint in <c>AppDbContext</c> asserts that exactly one of them is set.
/// <para>
/// The bytes live in the storage provider, not here. <see cref="StorageKey"/> is the
/// provider's identifier for them and <see cref="StorageProvider"/> records which provider
/// wrote it, so rows uploaded before a provider switch stay resolvable.
/// </para>
/// </remarks>
public class Attachment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Original client file name, kept for display and download only — never used as a path.</summary>
    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    /// <summary>Provider-specific identifier for the stored bytes (Cloudinary public id, or a relative path locally).</summary>
    public string StorageKey { get; set; } = string.Empty;

    /// <summary>Which provider holds the bytes, so a later provider change does not orphan existing rows.</summary>
    public string StorageProvider { get; set; } = string.Empty;

    public Guid? AssignmentId { get; set; }
    public Assignment? Assignment { get; set; }

    public Guid? SubmissionId { get; set; }
    public Submission? Submission { get; set; }

    public Guid UploadedById { get; set; }
    public User UploadedBy { get; set; } = null!;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
