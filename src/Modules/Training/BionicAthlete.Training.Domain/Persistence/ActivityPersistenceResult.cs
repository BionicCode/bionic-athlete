namespace BionicAthlete.Training.Domain.Persistence;

/// <summary>
/// Describes the outcome of saving an imported activity into local history.
/// </summary>
public sealed class ActivityPersistenceResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityPersistenceResult"/> class.
    /// </summary>
    public ActivityPersistenceResult(Guid activityId, ActivityFingerprint fingerprint, bool wasCreated, bool isPendingSync)
    {
        ActivityId = activityId;
        Fingerprint = fingerprint;
        WasCreated = wasCreated;
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
    /// Gets a value indicating whether the save operation created a new local record.
    /// </summary>
    public bool WasCreated { get; }

    /// <summary>
    /// Gets a value indicating whether the stored activity should be treated as pending remote sync.
    /// </summary>
    public bool IsPendingSync { get; }
}
