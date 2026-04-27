namespace BionicAthlete.Training.Application.Persistence;

using BionicAthlete.Training.Domain.Activities;

/// <summary>
/// Represents a fully loaded activity from local persistence.
/// </summary>
public sealed class StoredActivityRecord
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StoredActivityRecord"/> class.
    /// </summary>
    public StoredActivityRecord(
        Guid activityId,
        ActivityFingerprint fingerprint,
        FitActivity activity,
        DateTimeOffset importedAtUtc,
        bool isPendingSync)
    {
        ArgumentNullException.ThrowIfNull(activity);

        ActivityId = activityId;
        Fingerprint = fingerprint;
        Activity = activity;
        ImportedAtUtc = importedAtUtc;
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
    /// Gets the fully loaded activity.
    /// </summary>
    public FitActivity Activity { get; }

    /// <summary>
    /// Gets the UTC timestamp when the activity was imported into local history.
    /// </summary>
    public DateTimeOffset ImportedAtUtc { get; }

    /// <summary>
    /// Gets a value indicating whether the activity still has local changes that need remote sync.
    /// </summary>
    public bool IsPendingSync { get; }
}
