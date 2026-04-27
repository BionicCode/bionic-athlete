namespace BionicAthlete.Training.Application.Sync;

using System.Collections.Generic;

/// <summary>
/// Pushes locally stored activities to a remote HTTPS sync endpoint.
/// </summary>
/// <remarks>
/// This is a future-facing application boundary only.
/// Implementations must translate local activity records into versioned HTTPS DTOs and must never send raw SQL,
/// SQL fragments, or database-specific semantics across the network.
/// </remarks>
public interface IActivitySyncClient
{
    /// <summary>
    /// Pushes locally stored activities to the remote sync endpoint.
    /// </summary>
    /// <param name="activities">The locally stored activities selected for sync.</param>
    /// <param name="requestMetadata">The request metadata that carries API versioning and idempotency values.</param>
    /// <param name="cancellationToken">A token that can cancel the sync operation.</param>
    /// <returns>The push result returned by the remote sync boundary.</returns>
    Task<PushActivitiesResult> PushActivitiesAsync(
        IReadOnlyCollection<StoredActivityRecord> activities,
        SyncRequestMetadata requestMetadata,
        CancellationToken cancellationToken = default);
}
