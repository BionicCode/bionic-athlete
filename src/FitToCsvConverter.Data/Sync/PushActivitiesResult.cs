namespace FitToCsvConverter.Data.Sync;

using System.Collections.Immutable;
using FitToCsvConverter.Data.Persistence;

/// <summary>
/// Represents the outcome of a future activity sync push operation.
/// </summary>
public sealed class PushActivitiesResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PushActivitiesResult"/> class.
    /// </summary>
    public PushActivitiesResult(
        string apiVersion,
        ImmutableArray<ActivityFingerprint> acceptedFingerprints,
        ImmutableArray<SyncConflict> conflicts,
        bool isIdempotentReplay)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiVersion);

        ApiVersion = apiVersion;
        AcceptedFingerprints = acceptedFingerprints.IsDefault ? ImmutableArray<ActivityFingerprint>.Empty : acceptedFingerprints;
        Conflicts = conflicts.IsDefault ? ImmutableArray<SyncConflict>.Empty : conflicts;
        IsIdempotentReplay = isIdempotentReplay;
    }

    /// <summary>
    /// Gets the API version that processed the request.
    /// </summary>
    public string ApiVersion { get; }

    /// <summary>
    /// Gets the accepted activity fingerprints.
    /// </summary>
    public ImmutableArray<ActivityFingerprint> AcceptedFingerprints { get; }

    /// <summary>
    /// Gets the conflicts that require explicit client handling.
    /// </summary>
    public ImmutableArray<SyncConflict> Conflicts { get; }

    /// <summary>
    /// Gets a value indicating whether the remote endpoint treated the request as an idempotent replay.
    /// </summary>
    public bool IsIdempotentReplay { get; }
}
