namespace AssignmentSubmissionSystem.Application.Abstractions;

/// <summary>What a provider hands back after it has taken the bytes.</summary>
/// <param name="StorageKey">Provider-specific identifier used to read or delete the file later.</param>
/// <param name="SizeBytes">Size as counted by the provider, not as claimed by the client.</param>
public sealed record StoredFile(string StorageKey, long SizeBytes);

/// <summary>An open stream over a stored file. The caller owns the stream and must dispose it.</summary>
public sealed record FileContent(Stream Content, string ContentType);

/// <summary>
/// Where uploaded bytes live. Abstracted so the application layer never references a vendor
/// SDK, and so tests can substitute an in-memory provider instead of calling a real service.
/// </summary>
public interface IFileStorage
{
    /// <summary>Identifies the backing provider; persisted on the attachment row.</summary>
    string ProviderName { get; }

    Task<StoredFile> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken);

    Task<FileContent> OpenReadAsync(string storageKey, CancellationToken cancellationToken);

    Task DeleteAsync(string storageKey, CancellationToken cancellationToken);
}
