using System.Net.Mime;
using System.Security.Claims;
using AssignmentSubmissionSystem.Application.Attachments;
using AssignmentSubmissionSystem.Application.Attachments.Dtos;
using AssignmentSubmissionSystem.Application.Common;
using AssignmentSubmissionSystem.Application.Common.Constants;
using AssignmentSubmissionSystem.Application.Common.Exceptions;
using AssignmentSubmissionSystem.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSubmissionSystem.Api.Controllers;

/// <summary>
/// File uploads for assignments and submissions.
/// </summary>
/// <remarks>
/// Routes are declared per action rather than on the controller because uploads are addressed
/// through their owner (<c>/assignments/{id}/attachments</c>) while reads and deletes are
/// addressed by the file's own id.
/// </remarks>
[ApiController]
[Authorize]
public sealed class AttachmentsController(IAttachmentService attachmentService) : ControllerBase
{
    [HttpPost("api/v1/assignments/{assignmentId:guid}/attachments")]
    [Authorize(Roles = $"{Roles.Teacher},{Roles.Admin}")]
    public async Task<ActionResult<ApiResponse<AttachmentDto>>> UploadToAssignment(
        Guid assignmentId,
        IFormFile file,
        CancellationToken ct)
    {
        var upload = ToUpload(file);
        await using var content = upload.Content;

        var created = await attachmentService.UploadToAssignmentAsync(assignmentId, CurrentUserId, CurrentRole, upload, ct);
        return Ok(ApiResponse<AttachmentDto>.Ok(created));
    }

    [HttpPost("api/v1/submissions/{submissionId:guid}/attachments")]
    [Authorize(Roles = Roles.Student)]
    public async Task<ActionResult<ApiResponse<AttachmentDto>>> UploadToSubmission(
        Guid submissionId,
        IFormFile file,
        CancellationToken ct)
    {
        var upload = ToUpload(file);
        await using var content = upload.Content;

        var created = await attachmentService.UploadToSubmissionAsync(submissionId, CurrentUserId, upload, ct);
        return Ok(ApiResponse<AttachmentDto>.Ok(created));
    }

    /// <summary>
    /// Streams the file back through the API after checking the caller may see it.
    /// </summary>
    /// <remarks>
    /// The bytes are proxied rather than the client being redirected to the storage provider,
    /// so access stays governed by this application's rules. A provider URL handed to the
    /// browser would remain usable by anyone it was later forwarded to.
    /// </remarks>
    [HttpGet("api/v1/attachments/{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var download = await attachmentService.DownloadAsync(id, CurrentUserId, CurrentRole, ct);

        // "attachment" so the browser saves the file instead of rendering it in place; an
        // inline HTML or SVG upload would otherwise execute on this API's origin.
        var disposition = new ContentDisposition
        {
            FileName = download.FileName,
            Inline = false
        };

        Response.Headers.ContentDisposition = disposition.ToString();

        // Nosniff matters here specifically: it stops a browser from second-guessing the
        // declared content type and executing a file that was uploaded as something benign.
        Response.Headers.XContentTypeOptions = "nosniff";

        return File(download.Content, download.ContentType);
    }

    [HttpDelete("api/v1/attachments/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await attachmentService.DeleteAsync(id, CurrentUserId, CurrentRole, ct);
        return NoContent();
    }

    /// <summary>
    /// Adapts ASP.NET's <c>IFormFile</c> to the application-layer upload record.
    /// </summary>
    private static FileUpload ToUpload(IFormFile? file)
    {
        // Model binding yields null when the multipart part is absent or misnamed. Without
        // this the failure would surface as a NullReferenceException and a 500.
        if (file is null)
        {
            throw new BadRequestAppException("No file was uploaded. Send one multipart/form-data part named 'file'.");
        }

        return new FileUpload(
            file.FileName ?? string.Empty,
            file.ContentType ?? "application/octet-stream",
            file.Length,
            file.OpenReadStream());
    }

    private Guid CurrentUserId
    {
        get
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw, out var id)
                ? id
                : throw new UnauthorizedAppException("Token is missing a valid user id.");
        }
    }

    private UserRole CurrentRole
    {
        get
        {
            var raw = User.FindFirstValue(ClaimTypes.Role);
            return Enum.TryParse<UserRole>(raw, out var role)
                ? role
                : throw new UnauthorizedAppException("Token is missing a valid role.");
        }
    }
}
