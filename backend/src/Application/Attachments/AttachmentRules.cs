using System.Text.RegularExpressions;
using AssignmentSubmissionSystem.Application.Attachments.Dtos;
using AssignmentSubmissionSystem.Application.Common.Exceptions;
using AssignmentSubmissionSystem.Application.Options;

namespace AssignmentSubmissionSystem.Application.Attachments;

/// <summary>
/// What this system is willing to accept as an upload.
/// </summary>
/// <remarks>
/// An allow-list, not a block-list: enumerating dangerous extensions is a game you lose, since
/// anything omitted is accepted by default. Coursework is documents, images and archives, so
/// that is what is permitted and everything else is refused.
/// </remarks>
public static partial class AttachmentRules
{
    /// <summary>Permitted extensions, lower-case and dot-prefixed.</summary>
    public static readonly IReadOnlySet<string> AllowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".odt", ".rtf", ".txt", ".md",
        ".xls", ".xlsx", ".ods", ".csv",
        ".ppt", ".pptx", ".odp",
        ".png", ".jpg", ".jpeg", ".gif", ".webp",
        ".zip"
    };

    /// <summary>
    /// Permitted media types. Checked alongside the extension so a client cannot smuggle a
    /// payload through by pairing an innocuous extension with an executable content type.
    /// </summary>
    public static readonly IReadOnlySet<string> AllowedContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.oasis.opendocument.text",
        "application/rtf", "text/rtf",
        "text/plain", "text/markdown", "text/csv",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.oasis.opendocument.spreadsheet",
        "application/vnd.ms-powerpoint",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "application/vnd.oasis.opendocument.presentation",
        "image/png", "image/jpeg", "image/gif", "image/webp",
        "application/zip", "application/x-zip-compressed",
        "application/octet-stream"
    };

    public const int MaxFileNameLength = 255;

    /// <summary>
    /// Validates one upload against the configured limits.
    /// Throws <see cref="BadRequestAppException"/> with a message meant for the end user.
    /// </summary>
    public static void EnsureAcceptable(FileUpload file, StorageOptions options)
    {
        if (string.IsNullOrWhiteSpace(file.FileName))
        {
            throw new BadRequestAppException("The uploaded file has no name.");
        }

        if (file.Length <= 0)
        {
            throw new BadRequestAppException($"'{file.FileName}' is empty.");
        }

        if (file.Length > options.MaxFileSizeBytes)
        {
            var limitMb = options.MaxFileSizeBytes / (1024d * 1024d);
            throw new BadRequestAppException(
                $"'{file.FileName}' exceeds the maximum upload size of {limitMb:0.#} MB.");
        }

        var extension = Path.GetExtension(SanitizeFileName(file.FileName));
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new BadRequestAppException(
                $"'{file.FileName}' has an unsupported file type. Allowed types: "
                + string.Join(", ", AllowedExtensions.OrderBy(e => e, StringComparer.Ordinal)) + ".");
        }

        if (!AllowedContentTypes.Contains(file.ContentType))
        {
            throw new BadRequestAppException($"'{file.FileName}' has an unsupported content type ({file.ContentType}).");
        }
    }

    /// <summary>
    /// Reduces a client-supplied name to something safe to store and to echo back in a
    /// Content-Disposition header.
    /// </summary>
    /// <remarks>
    /// Strips any directory component (a browser on Windows may send a full path, and
    /// "../" segments are hostile), then removes control and quoting characters that would let
    /// a crafted name break out of the header it is later written into. The result is display
    /// metadata only — it is never used to build a filesystem path.
    /// </remarks>
    public static string SanitizeFileName(string fileName)
    {
        // Take the last segment under either separator, so neither "a/b/c.pdf" nor
        // "a\\b\\c.pdf" leaves a directory component behind.
        var baseName = fileName
            .Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .LastOrDefault() ?? string.Empty;

        var cleaned = UnsafeFileNameCharacters().Replace(baseName, "_").Trim();

        // A name consisting only of dots ("." or "..") survives the pass above but is not a
        // usable name; fall back rather than store it.
        if (cleaned.Length == 0 || cleaned.All(c => c == '.'))
        {
            return "file";
        }

        return cleaned.Length > MaxFileNameLength
            ? cleaned[^MaxFileNameLength..]
            : cleaned;
    }

    /// <summary>Control characters, quotes, and separators that are unsafe in a header value.</summary>
    [GeneratedRegex(@"[\x00-\x1F\x7F""'`;:*?<>|\\/]")]
    private static partial Regex UnsafeFileNameCharacters();
}
