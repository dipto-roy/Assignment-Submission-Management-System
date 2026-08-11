using AssignmentSubmissionSystem.Application.Abstractions;
using AssignmentSubmissionSystem.Application.Common.Paging;
using AssignmentSubmissionSystem.Application.Notifications.Dtos;
using AssignmentSubmissionSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AssignmentSubmissionSystem.Infrastructure.Persistence.Repositories;

public sealed class NotificationRepository(AppDbContext db) : INotificationRepository
{
    /// <summary>Postgres unique-violation SQLSTATE.</summary>
    private const string UniqueViolation = "23505";

    public Task<PagedResult<Notification>> FindByUserAsync(
        Guid userId,
        NotificationQuery query,
        CancellationToken cancellationToken)
    {
        var scoped = db.Notifications.AsNoTracking().Where(n => n.UserId == userId);

        if (query.UnreadOnly)
        {
            scoped = scoped.Where(n => !n.IsRead);
        }

        return scoped
            .OrderByDescending(n => n.CreatedAt)
            .ToPagedResultAsync(query, cancellationToken);
    }

    public Task<Notification?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Notifications.SingleOrDefaultAsync(n => n.Id == id, cancellationToken);

    public Task<int> CountUnreadAsync(Guid userId, CancellationToken cancellationToken) =>
        db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);

    /// <summary>
    /// Inserts the batch, tolerating the deadline-reminder unique index.
    /// </summary>
    /// <remarks>
    /// The reminder worker re-scans on every tick, so a row it already created will collide.
    /// That collision is the index doing its job, not an error: it is swallowed deliberately
    /// and only for the unique-violation SQLSTATE, so every other database failure still
    /// propagates. On collision the batch is retried one row at a time, which keeps the
    /// non-colliding notifications in a mixed batch from being lost with it.
    /// </remarks>
    public async Task AddRangeAsync(IReadOnlyCollection<Notification> notifications, CancellationToken cancellationToken)
    {
        if (notifications.Count == 0)
        {
            return;
        }

        db.Notifications.AddRange(notifications);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Abandon the failed batch: the change tracker still holds every row from it, so
            // reusing this context would just replay the same conflict.
            foreach (var entry in db.ChangeTracker.Entries<Notification>().ToList())
            {
                entry.State = EntityState.Detached;
            }
        }

        foreach (var notification in notifications)
        {
            db.Notifications.Add(notification);

            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                // Already notified about this assignment — the intended outcome.
                db.Entry(notification).State = EntityState.Detached;
            }
        }
    }

    public Task UpdateAsync(Notification notification, CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);

    public Task<int> MarkAllReadAsync(Guid userId, DateTime readAt, CancellationToken cancellationToken) =>
        db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(
                set => set
                    .SetProperty(n => n.IsRead, true)
                    .SetProperty(n => n.ReadAt, readAt),
                cancellationToken);

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: UniqueViolation };
}
