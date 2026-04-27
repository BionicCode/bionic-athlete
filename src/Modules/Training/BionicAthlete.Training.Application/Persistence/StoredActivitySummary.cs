namespace BionicAthlete.Training.Application.Persistence;

using BionicAthlete.Training.Domain.Activities;

/// <summary>
/// Represents summary information about a locally stored activity.
/// </summary>
public sealed class StoredActivitySummary
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StoredActivitySummary"/> class.
    /// </summary>
    public StoredActivitySummary(
        Guid activityId,
        ActivityFingerprint fingerprint,
        string sourceDisplayName,
        DateTimeOffset importedAtUtc,
        DateTimeOffset? activityStartTimeUtc,
        int sessionCount,
        bool isPendingSync)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDisplayName);

        if (sessionCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sessionCount), sessionCount, "Session count must be zero or greater.");
        }

        ActivityId = activityId;
        Fingerprint = fingerprint;
        SourceDisplayName = sourceDisplayName;
        ImportedAtUtc = importedAtUtc;
        ActivityStartTimeUtc = activityStartTimeUtc;
        SessionCount = sessionCount;
        IsPendingSync = isPendingSync;
    }

    /// <summary>
    /// Gets the local activity identifier.
    /// </summary>
    public Guid ActivityId { get; }

    /// <summary>
    /// Gets the stable content fingerprint.
    /// </summary>
    public ActivityFingerprint Fingerprint { get; }

    /// <summary>
    /// Gets the display name of the imported source file.
    /// </summary>
    public string SourceDisplayName { get; }

    /// <summary>
    /// Gets the UTC timestamp when the activity was imported into local history.
    /// </summary>
    public DateTimeOffset ImportedAtUtc { get; }

    /// <summary>
    /// Gets the canonical activity start time when known.
    /// </summary>
    public DateTimeOffset? ActivityStartTimeUtc { get; }

    /// <summary>
    /// Gets the number of sessions in the stored activity.
    /// </summary>
    public int SessionCount { get; }

    /// <summary>
    /// Gets a value indicating whether the activity still has local changes that need remote sync.
    /// </summary>
    public bool IsPendingSync { get; }
}
