using System.ComponentModel.DataAnnotations;

namespace AssignmentSubmissionSystem.Application.Options;

/// <summary>
/// Controls the background worker that warns students about approaching deadlines.
/// Configured under <c>Notifications:DeadlineReminder</c>.
/// </summary>
public sealed class DeadlineReminderOptions
{
    public const string SectionName = "Notifications:DeadlineReminder";

    /// <summary>
    /// Lets a deployment turn the worker off. The integration suite disables it so a background
    /// scan cannot insert rows underneath a test that is asserting on notification counts.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>How often to scan. The default trades promptness for a near-idle query load.</summary>
    [Range(1, 1440)]
    public int ScanIntervalMinutes { get; set; } = 30;

    /// <summary>
    /// How far ahead a deadline counts as "approaching". Each student is reminded once per
    /// assignment, so this is the notice they get rather than a repeating nag.
    /// </summary>
    [Range(1, 720)]
    public int LeadTimeHours { get; set; } = 24;
}
