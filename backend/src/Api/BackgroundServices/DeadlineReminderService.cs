using AssignmentSubmissionSystem.Application.Abstractions;
using AssignmentSubmissionSystem.Application.Notifications;
using AssignmentSubmissionSystem.Application.Options;
using Microsoft.Extensions.Options;

namespace AssignmentSubmissionSystem.Api.BackgroundServices;

/// <summary>
/// Periodically notifies students whose deadline is near and who have not submitted.
/// </summary>
/// <remarks>
/// The only trigger that is not event-driven: nothing happens when a deadline approaches, so
/// something has to look. Each scan reads the assignments due inside the lead-time window,
/// subtracts the students who have already submitted, and notifies the rest.
/// <para>
/// Re-notification is prevented by a filtered unique index on
/// (UserId, AssignmentId) for <c>DeadlineApproaching</c>, not by remembering state here. That
/// keeps the worker stateless and safe to run in more than one replica: a duplicate insert is
/// rejected by the database and absorbed by the repository.
/// </para>
/// </remarks>
public sealed class DeadlineReminderService(
    IServiceScopeFactory scopeFactory,
    IOptions<DeadlineReminderOptions> options,
    ILogger<DeadlineReminderService> logger) : BackgroundService
{
    private readonly DeadlineReminderOptions settings = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!settings.Enabled)
        {
            logger.LogInformation("Deadline reminder worker is disabled by configuration.");
            return;
        }

        logger.LogInformation(
            "Deadline reminder worker started: scanning every {ScanInterval} minutes for deadlines within {LeadTime} hours.",
            settings.ScanIntervalMinutes,
            settings.LeadTimeHours);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(settings.ScanIntervalMinutes));

        // Scan immediately on boot, then on the timer, so a restart does not delay reminders
        // by a full interval.
        do
        {
            try
            {
                await ScanOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown.
                break;
            }
            catch (Exception ex)
            {
                // A failed scan must not take the worker — or the host — down with it. The next
                // tick retries from scratch, and the window is wide enough that a transient
                // database blip does not cost anyone their reminder.
                logger.LogError(ex, "Deadline reminder scan failed. Retrying on the next interval.");
            }
        }
        while (await SafeWaitAsync(timer, stoppingToken));
    }

    private async Task ScanOnceAsync(CancellationToken cancellationToken)
    {
        // A scope per scan: the repositories and DbContext are scoped services, and a singleton
        // background service has no ambient scope to resolve them from.
        using var scope = scopeFactory.CreateScope();
        var provider = scope.ServiceProvider;

        var assignmentRepository = provider.GetRequiredService<IAssignmentRepository>();
        var submissionRepository = provider.GetRequiredService<ISubmissionRepository>();
        var notificationService = provider.GetRequiredService<INotificationService>();

        var now = DateTime.UtcNow;
        var horizon = now.AddHours(settings.LeadTimeHours);

        var dueSoon = await assignmentRepository.FindPublishedDueBetweenAsync(now, horizon, cancellationToken);
        if (dueSoon.Count == 0)
        {
            return;
        }

        var notified = 0;

        foreach (var assignment in dueSoon)
        {
            var roster = await assignmentRepository.FindStudentIdsInClassAsync(
                assignment.Subject.ClassId, cancellationToken);

            var submitted = await submissionRepository.FindStudentIdsWithSubmissionAsync(
                assignment.Id, cancellationToken);

            var pending = roster.Except(submitted).ToList();
            if (pending.Count == 0)
            {
                continue;
            }

            await notificationService.NotifyDeadlineApproachingAsync(assignment, pending, cancellationToken);
            notified += pending.Count;
        }

        if (notified > 0)
        {
            logger.LogInformation(
                "Deadline reminder scan covered {AssignmentCount} assignment(s) and reached up to {StudentCount} student(s).",
                dueSoon.Count,
                notified);
        }
    }

    /// <summary>
    /// Waits for the next tick, translating shutdown cancellation into a clean loop exit rather
    /// than an exception escaping <see cref="ExecuteAsync"/>.
    /// </summary>
    private static async Task<bool> SafeWaitAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
