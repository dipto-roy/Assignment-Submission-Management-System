namespace AssignmentSubmissionSystem.Domain.Enums;

/// <summary>
/// What caused a notification. Persisted as a string so a new member never renumbers the
/// existing rows, and so the value is readable straight out of the database.
/// </summary>
public enum NotificationType
{
    /// <summary>A teacher published an assignment — sent to every student in the class.</summary>
    AssignmentPublished,

    /// <summary>A student submitted work — sent to the teacher who owns the assignment.</summary>
    SubmissionReceived,

    /// <summary>A teacher graded work — sent to the student who submitted it.</summary>
    SubmissionGraded,

    /// <summary>A deadline is near and the student has not submitted — sent once per assignment.</summary>
    DeadlineApproaching
}
