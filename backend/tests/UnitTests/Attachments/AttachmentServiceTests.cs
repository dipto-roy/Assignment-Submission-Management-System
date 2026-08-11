using AssignmentSubmissionSystem.Application.Abstractions;
using AssignmentSubmissionSystem.Application.Attachments;
using AssignmentSubmissionSystem.Application.Attachments.Dtos;
using AssignmentSubmissionSystem.Application.Common.Exceptions;
using AssignmentSubmissionSystem.Application.Options;
using AssignmentSubmissionSystem.Domain.Entities;
using AssignmentSubmissionSystem.Domain.Enums;
using Microsoft.Extensions.Options;
using Moq;

namespace AssignmentSubmissionSystem.UnitTests.Attachments;

public sealed class AttachmentServiceTests
{
    private readonly Mock<IAttachmentRepository> _attachmentRepository = new();
    private readonly Mock<IAssignmentRepository> _assignmentRepository = new();
    private readonly Mock<ISubmissionRepository> _submissionRepository = new();
    private readonly Mock<IFileStorage> _fileStorage = new();
    private readonly AttachmentService _sut;

    private readonly StorageOptions _options = new()
    {
        Provider = StorageOptions.ProviderLocal,
        MaxFileSizeBytes = 10 * 1024,
        MaxFilesPerOwner = 2
    };

    public AttachmentServiceTests()
    {
        _fileStorage.SetupGet(s => s.ProviderName).Returns("Test");
        _fileStorage
            .Setup(s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoredFile("stored-key", 42));

        _sut = new AttachmentService(
            _attachmentRepository.Object,
            _assignmentRepository.Object,
            _submissionRepository.Object,
            _fileStorage.Object,
            Options.Create(_options));
    }

    private static Assignment BuildAssignment(Guid teacherId, DateTime? deadline = null, AssignmentStatus status = AssignmentStatus.Published)
    {
        var schoolClass = new SchoolClass { Name = "Class 10", Section = "A" };
        var subject = new Subject { Name = "Mathematics", Code = "MATH101", Class = schoolClass, ClassId = schoolClass.Id };

        return new Assignment
        {
            Title = "Algebra",
            Deadline = deadline ?? DateTime.UtcNow.AddDays(3),
            MaxMarks = 100,
            Status = status,
            TeacherId = teacherId,
            Subject = subject,
            SubjectId = subject.Id
        };
    }

    private static Submission BuildSubmission(Guid studentId, Assignment assignment) => new()
    {
        StudentId = studentId,
        Assignment = assignment,
        AssignmentId = assignment.Id
    };

    private static FileUpload Upload() =>
        new("essay.pdf", "application/pdf", 100, new MemoryStream(new byte[100]));

    [Fact]
    public async Task UploadToAssignment_Throws_WhenTeacherDoesNotOwnTheAssignment()
    {
        var assignment = BuildAssignment(teacherId: Guid.NewGuid());
        _assignmentRepository.Setup(r => r.FindByIdAsync(assignment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(assignment);

        await Assert.ThrowsAsync<ForbiddenAppException>(
            () => _sut.UploadToAssignmentAsync(assignment.Id, Guid.NewGuid(), UserRole.Teacher, Upload(), CancellationToken.None));
    }

    [Fact]
    public async Task UploadToSubmission_Throws_WhenStudentDoesNotOwnTheSubmission()
    {
        var assignment = BuildAssignment(Guid.NewGuid());
        var submission = BuildSubmission(Guid.NewGuid(), assignment);
        _submissionRepository.Setup(r => r.FindByIdAsync(submission.Id, It.IsAny<CancellationToken>())).ReturnsAsync(submission);

        await Assert.ThrowsAsync<ForbiddenAppException>(
            () => _sut.UploadToSubmissionAsync(submission.Id, Guid.NewGuid(), Upload(), CancellationToken.None));
    }

    [Fact]
    public async Task UploadToSubmission_Throws_AfterTheDeadlineHasPassed()
    {
        // Otherwise the deadline is trivially bypassed: text is locked, but work could still
        // be added as a file.
        var studentId = Guid.NewGuid();
        var assignment = BuildAssignment(Guid.NewGuid(), deadline: DateTime.UtcNow.AddMinutes(-1));
        var submission = BuildSubmission(studentId, assignment);
        _submissionRepository.Setup(r => r.FindByIdAsync(submission.Id, It.IsAny<CancellationToken>())).ReturnsAsync(submission);

        var exception = await Assert.ThrowsAsync<BadRequestAppException>(
            () => _sut.UploadToSubmissionAsync(submission.Id, studentId, Upload(), CancellationToken.None));

        Assert.Contains("locked", exception.Message);
    }

    [Fact]
    public async Task UploadToSubmission_Throws_WhenThePerOwnerCapIsReached()
    {
        var studentId = Guid.NewGuid();
        var assignment = BuildAssignment(Guid.NewGuid());
        var submission = BuildSubmission(studentId, assignment);
        _submissionRepository.Setup(r => r.FindByIdAsync(submission.Id, It.IsAny<CancellationToken>())).ReturnsAsync(submission);
        _attachmentRepository
            .Setup(r => r.CountForSubmissionAsync(submission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_options.MaxFilesPerOwner);

        var exception = await Assert.ThrowsAsync<BadRequestAppException>(
            () => _sut.UploadToSubmissionAsync(submission.Id, studentId, Upload(), CancellationToken.None));

        Assert.Contains($"maximum of {_options.MaxFilesPerOwner}", exception.Message);
    }

    [Fact]
    public async Task UploadToSubmission_RecordsTheProviderReportedSize_NotTheClientClaim()
    {
        var studentId = Guid.NewGuid();
        var assignment = BuildAssignment(Guid.NewGuid());
        var submission = BuildSubmission(studentId, assignment);
        _submissionRepository.Setup(r => r.FindByIdAsync(submission.Id, It.IsAny<CancellationToken>())).ReturnsAsync(submission);

        // Client claims 100 bytes; the storage provider counted 42.
        var result = await _sut.UploadToSubmissionAsync(submission.Id, studentId, Upload(), CancellationToken.None);

        Assert.Equal(42, result.SizeBytes);
    }

    [Fact]
    public async Task Download_Throws_ForAStudentWhoIsNotTheSubmissionOwner()
    {
        // Business rule §7.4: a student never sees another student's work.
        var assignment = BuildAssignment(Guid.NewGuid());
        var submission = BuildSubmission(Guid.NewGuid(), assignment);
        var attachment = new Attachment { Submission = submission, SubmissionId = submission.Id, StorageKey = "k" };
        _attachmentRepository.Setup(r => r.FindByIdAsync(attachment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(attachment);

        await Assert.ThrowsAsync<ForbiddenAppException>(
            () => _sut.DownloadAsync(attachment.Id, Guid.NewGuid(), UserRole.Student, CancellationToken.None));

        _fileStorage.Verify(
            s => s.OpenReadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "storage must not be touched when authorization fails");
    }

    [Fact]
    public async Task Download_Allows_TheTeacherWhoSetTheAssignment()
    {
        var teacherId = Guid.NewGuid();
        var assignment = BuildAssignment(teacherId);
        var submission = BuildSubmission(Guid.NewGuid(), assignment);
        var attachment = new Attachment
        {
            Submission = submission,
            SubmissionId = submission.Id,
            StorageKey = "k",
            ContentType = "application/pdf",
            FileName = "essay.pdf"
        };
        _attachmentRepository.Setup(r => r.FindByIdAsync(attachment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(attachment);
        _fileStorage
            .Setup(s => s.OpenReadAsync("k", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileContent(new MemoryStream(), "application/octet-stream"));

        var download = await _sut.DownloadAsync(attachment.Id, teacherId, UserRole.Teacher, CancellationToken.None);

        // The recorded content type wins over whatever the provider reported.
        Assert.Equal("application/pdf", download.ContentType);
        Assert.Equal("essay.pdf", download.FileName);
    }

    [Fact]
    public async Task Download_Throws_ForAStudentNotEnrolledInTheAssignmentsClass()
    {
        var assignment = BuildAssignment(Guid.NewGuid());
        var attachment = new Attachment { Assignment = assignment, AssignmentId = assignment.Id, StorageKey = "k" };
        _attachmentRepository.Setup(r => r.FindByIdAsync(attachment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(attachment);
        _assignmentRepository
            .Setup(r => r.IsStudentEnrolledInClassAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await Assert.ThrowsAsync<ForbiddenAppException>(
            () => _sut.DownloadAsync(attachment.Id, Guid.NewGuid(), UserRole.Student, CancellationToken.None));
    }

    [Fact]
    public async Task Download_Throws_ForAStudent_WhenTheAssignmentIsStillADraft()
    {
        // Enrolled, but the brief has not been published yet.
        var assignment = BuildAssignment(Guid.NewGuid(), status: AssignmentStatus.Draft);
        var attachment = new Attachment { Assignment = assignment, AssignmentId = assignment.Id, StorageKey = "k" };
        _attachmentRepository.Setup(r => r.FindByIdAsync(attachment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(attachment);
        _assignmentRepository
            .Setup(r => r.IsStudentEnrolledInClassAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<ForbiddenAppException>(
            () => _sut.DownloadAsync(attachment.Id, Guid.NewGuid(), UserRole.Student, CancellationToken.None));
    }

    [Fact]
    public async Task Download_Allows_AnAdmin_ForAnySubmission()
    {
        var assignment = BuildAssignment(Guid.NewGuid());
        var submission = BuildSubmission(Guid.NewGuid(), assignment);
        var attachment = new Attachment
        {
            Submission = submission,
            SubmissionId = submission.Id,
            StorageKey = "k",
            ContentType = "application/pdf",
            FileName = "essay.pdf"
        };
        _attachmentRepository.Setup(r => r.FindByIdAsync(attachment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(attachment);
        _fileStorage
            .Setup(s => s.OpenReadAsync("k", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileContent(new MemoryStream(), "application/pdf"));

        var download = await _sut.DownloadAsync(attachment.Id, Guid.NewGuid(), UserRole.Admin, CancellationToken.None);

        Assert.Equal("essay.pdf", download.FileName);
    }

    [Fact]
    public async Task Delete_RemovesTheRowBeforeTheStoredBytes()
    {
        // Ordering matters: a surviving row pointing at deleted bytes is a broken download,
        // whereas an orphaned object is merely wasted space.
        var studentId = Guid.NewGuid();
        var assignment = BuildAssignment(Guid.NewGuid());
        var submission = BuildSubmission(studentId, assignment);
        var attachment = new Attachment { Submission = submission, SubmissionId = submission.Id, StorageKey = "k" };
        _attachmentRepository.Setup(r => r.FindByIdAsync(attachment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(attachment);

        var sequence = new List<string>();
        _attachmentRepository
            .Setup(r => r.DeleteAsync(attachment, It.IsAny<CancellationToken>()))
            .Callback(() => sequence.Add("row"))
            .Returns(Task.CompletedTask);
        _fileStorage
            .Setup(s => s.DeleteAsync("k", It.IsAny<CancellationToken>()))
            .Callback(() => sequence.Add("bytes"))
            .Returns(Task.CompletedTask);

        await _sut.DeleteAsync(attachment.Id, studentId, UserRole.Student, CancellationToken.None);

        Assert.Equal(new[] { "row", "bytes" }, sequence);
    }

    [Fact]
    public async Task Delete_Throws_WhenATeacherTargetsAStudentsSubmissionFile()
    {
        var teacherId = Guid.NewGuid();
        var assignment = BuildAssignment(teacherId);
        var submission = BuildSubmission(Guid.NewGuid(), assignment);
        var attachment = new Attachment { Submission = submission, SubmissionId = submission.Id, StorageKey = "k" };
        _attachmentRepository.Setup(r => r.FindByIdAsync(attachment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(attachment);

        // Reading it to mark the work is allowed; destroying it is not.
        await Assert.ThrowsAsync<ForbiddenAppException>(
            () => _sut.DeleteAsync(attachment.Id, teacherId, UserRole.Teacher, CancellationToken.None));
    }
}
