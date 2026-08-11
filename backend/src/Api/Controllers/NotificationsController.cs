using System.Security.Claims;
using AssignmentSubmissionSystem.Application.Common;
using AssignmentSubmissionSystem.Application.Common.Exceptions;
using AssignmentSubmissionSystem.Application.Notifications;
using AssignmentSubmissionSystem.Application.Notifications.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentSubmissionSystem.Api.Controllers;

/// <summary>
/// In-app notifications for the signed-in user.
/// </summary>
/// <remarks>
/// Every action is scoped to the caller's own id taken from the token. There is no route that
/// accepts a user id, so no role check is needed beyond being authenticated: a user cannot
/// address anyone else's notifications in the first place.
/// </remarks>
[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public sealed class NotificationsController(INotificationService notificationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<NotificationDto>>>> GetMine(
        [FromQuery] NotificationQuery query,
        CancellationToken ct)
    {
        var page = await notificationService.GetMineAsync(CurrentUserId, query, ct);
        return Ok(ApiResponse<IReadOnlyList<NotificationDto>>.Ok(page.Items, page.ToMeta()));
    }

    /// <summary>Badge count. Kept separate so the bell can poll it without fetching a page of rows.</summary>
    [HttpGet("unread-count")]
    public async Task<ActionResult<ApiResponse<UnreadCountDto>>> GetUnreadCount(CancellationToken ct)
    {
        var unread = await notificationService.GetUnreadCountAsync(CurrentUserId, ct);
        return Ok(ApiResponse<UnreadCountDto>.Ok(new UnreadCountDto(unread)));
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<ActionResult<ApiResponse<NotificationDto>>> MarkRead(Guid id, CancellationToken ct)
    {
        var updated = await notificationService.MarkReadAsync(id, CurrentUserId, ct);
        return Ok(ApiResponse<NotificationDto>.Ok(updated));
    }

    [HttpPost("read-all")]
    public async Task<ActionResult<ApiResponse<UnreadCountDto>>> MarkAllRead(CancellationToken ct)
    {
        await notificationService.MarkAllReadAsync(CurrentUserId, ct);

        // Returns the resulting count (zero) so the client can settle the badge from the
        // response instead of firing another request.
        return Ok(ApiResponse<UnreadCountDto>.Ok(new UnreadCountDto(0)));
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
}
