using AssignmentSubmissionSystem.Application.Attachments;
using AssignmentSubmissionSystem.Application.Attachments.Dtos;
using AssignmentSubmissionSystem.Application.Common.Exceptions;
using AssignmentSubmissionSystem.Application.Options;

namespace AssignmentSubmissionSystem.UnitTests.Attachments;

public sealed class AttachmentRulesTests
{
    private static readonly StorageOptions Options = new()
    {
        MaxFileSizeBytes = 1024,
        MaxFilesPerOwner = 3
    };

    private static FileUpload Upload(
        string fileName = "essay.pdf",
        string contentType = "application/pdf",
        long length = 100) =>
        new(fileName, contentType, length, new MemoryStream());

    [Fact]
    public void EnsureAcceptable_Passes_ForAnAllowedDocument()
    {
        var exception = Record.Exception(() => AttachmentRules.EnsureAcceptable(Upload(), Options));

        Assert.Null(exception);
    }

    [Fact]
    public void EnsureAcceptable_Throws_WhenFileExceedsTheSizeLimit()
    {
        var oversized = Upload(length: Options.MaxFileSizeBytes + 1);

        var exception = Assert.Throws<BadRequestAppException>(
            () => AttachmentRules.EnsureAcceptable(oversized, Options));

        Assert.Contains("maximum upload size", exception.Message);
    }

    [Fact]
    public void EnsureAcceptable_Throws_WhenFileIsEmpty()
    {
        var exception = Assert.Throws<BadRequestAppException>(
            () => AttachmentRules.EnsureAcceptable(Upload(length: 0), Options));

        Assert.Contains("empty", exception.Message);
    }

    [Theory]
    [InlineData("payload.exe")]
    [InlineData("script.sh")]
    [InlineData("run.bat")]
    [InlineData("lib.dll")]
    [InlineData("noextension")]
    public void EnsureAcceptable_Rejects_ExecutableAndUnknownExtensions(string fileName)
    {
        var upload = Upload(fileName, "application/octet-stream");

        var exception = Assert.Throws<BadRequestAppException>(
            () => AttachmentRules.EnsureAcceptable(upload, Options));

        Assert.Contains("unsupported file type", exception.Message);
    }

    [Fact]
    public void EnsureAcceptable_Rejects_AnAllowedExtensionPairedWithADisallowedContentType()
    {
        // The pairing is what matters: a permitted extension must not smuggle through a
        // content type the allow-list does not cover.
        var upload = Upload("report.pdf", "application/x-msdownload");

        var exception = Assert.Throws<BadRequestAppException>(
            () => AttachmentRules.EnsureAcceptable(upload, Options));

        Assert.Contains("unsupported content type", exception.Message);
    }

    [Fact]
    public void EnsureAcceptable_JudgesTheFinalExtension_NotAnEarlierOne()
    {
        // "essay.pdf.exe" is an executable wearing a document's name.
        var upload = Upload("essay.pdf.exe", "application/pdf");

        Assert.Throws<BadRequestAppException>(() => AttachmentRules.EnsureAcceptable(upload, Options));
    }

    [Theory]
    [InlineData("../../etc/passwd.pdf", "passwd.pdf")]
    [InlineData("..\\..\\windows\\system32\\config.txt", "config.txt")]
    [InlineData("C:\\Users\\bob\\notes.md", "notes.md")]
    [InlineData("/absolute/path/report.pdf", "report.pdf")]
    public void SanitizeFileName_StripsEveryDirectoryComponent(string input, string expected)
    {
        Assert.Equal(expected, AttachmentRules.SanitizeFileName(input));
    }

    [Fact]
    public void SanitizeFileName_RemovesQuotesAndControlCharacters()
    {
        // A quote or newline surviving into Content-Disposition would let a crafted name
        // break out of the header value.
        var sanitized = AttachmentRules.SanitizeFileName("re\"port\r\n;name=evil.pdf");

        Assert.DoesNotContain('"', sanitized);
        Assert.DoesNotContain('\r', sanitized);
        Assert.DoesNotContain('\n', sanitized);
        Assert.DoesNotContain(';', sanitized);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("..")]
    [InlineData(".")]
    public void SanitizeFileName_FallsBack_WhenNothingUsableRemains(string input)
    {
        Assert.Equal("file", AttachmentRules.SanitizeFileName(input));
    }

    [Fact]
    public void SanitizeFileName_TruncatesOverlongNames()
    {
        var sanitized = AttachmentRules.SanitizeFileName(new string('a', 400) + ".pdf");

        Assert.Equal(AttachmentRules.MaxFileNameLength, sanitized.Length);
    }
}
