namespace BionicAthlete.Training.Infrastructure.Persistence.Persistence;

using System.Collections.Immutable;
using BionicAthlete.Training.Domain.Activities;
using BionicAthlete.Training.Domain.Persistence;

/// <summary>
/// Stores and queries imported activities in the local application history.
/// </summary>
/// <remarks>
/// This abstraction models durable local history only.
/// Concrete SQLite storage is intentionally deferred and may later use EF Core or lower-level SQLite access without
/// changing this contract.
/// </remarks>
public interface IActivityHistoryStore
{
    /// <summary>
    /// Saves an imported activity into local history.
    /// </summary>
    /// <param name="importedActivity">The imported activity envelope to save.</param>
    /// <param name="cancellationToken">A token that can cancel the save operation.</param>
    /// <returns>The persistence result for the saved activity.</returns>
    Task<ActivityPersistenceResult> SaveImportedActivityAsync(ImportedActivityEnvelope importedActivity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tries to load one stored activity by local identifier.
    /// </summary>
    /// <param name="activityId">The local identifier of the stored activity.</param>
    /// <param name="cancellationToken">A token that can cancel the load operation.</param>
    /// <returns>The stored activity record, or <see langword="null"/> when no local record exists.</returns>
    Task<StoredActivityRecord?> TryGetActivityAsync(Guid activityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tries to load one stored activity by stable content fingerprint.
    /// </summary>
    /// <param name="fingerprint">The stable content fingerprint to look up.</param>
    /// <param name="cancellationToken">A token that can cancel the load operation.</param>
    /// <returns>The stored activity record, or <see langword="null"/> when no local record exists.</returns>
    Task<StoredActivityRecord?> TryGetByFingerprintAsync(ActivityFingerprint fingerprint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries locally stored activity summaries.
    /// </summary>
    /// <param name="query">The query that shapes the local result set.</param>
    /// <param name="cancellationToken">A token that can cancel the query operation.</param>
    /// <returns>The matching locally stored activity summaries.</returns>
    Task<ImmutableArray<StoredActivitySummary>> QueryActivitiesAsync(ActivityHistoryQuery query, CancellationToken cancellationToken = default);
}
