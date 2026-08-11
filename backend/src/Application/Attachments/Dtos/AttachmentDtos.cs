namespace AssignmentSubmissionSystem.Application.Attachments.Dtos;

/// <summary>
/// An uploaded file as the client sees it. The storage key and provider are deliberately
/// absent: they are internal addressing, and exposing them would invite clients to bypass the
/// authorized download endpoint.
/// </summary>
public sealed record AttachmentDto(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    Guid UploadedById,
    DateTime UploadedAt);

/// <summary>
/// One incoming file, decoupled from ASP.NET's <c>IFormFile</c> so the application layer stays
/// free of web-host types and can be unit tested with a plain <see cref="MemoryStream"/>.
/// </summary>
/// <param name="FileName">The client-supplied name. Untrusted — treated as display text only.</param>
/// <param name="Length">Client-reported length, checked before the stream is read.</param>
public sealed record FileUpload(string FileName, string ContentType, long Length, Stream Content);
