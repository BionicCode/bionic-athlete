namespace BionicAthlete.Training.Application.Sync;
/// <summary>
/// Carries request metadata that future HTTPS sync operations must preserve for versioning and idempotency.
/// </summary>
/// <remarks>
/// Authentication and authorization are intentionally handled by the sync client implementation and the hosted
/// endpoint, not by embedding database or SQL details into the desktop request contract.
/// </remarks>
public sealed class SyncRequestMetadata
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SyncRequestMetadata"/> class.
    /// </summary>
    public SyncRequestMetadata(string apiVersion, string clientApplicationId, string clientRequestId, DateTimeOffset sentAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientApplicationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientRequestId);

        ApiVersion = apiVersion;
        ClientApplicationId = clientApplicationId;
        ClientRequestId = clientRequestId;
        SentAtUtc = sentAtUtc;
    }

    /// <summary>
    /// Gets the version of the remote sync contract that the request targets.
    /// </summary>
    public string ApiVersion { get; }

    /// <summary>
    /// Gets the identifier of the desktop application instance or installation.
    /// </summary>
    public string ClientApplicationId { get; }

    /// <summary>
    /// Gets the client-generated idempotency token for the request.
    /// </summary>
    public string ClientRequestId { get; }

    /// <summary>
    /// Gets the UTC timestamp when the client emitted the request.
    /// </summary>
    public DateTimeOffset SentAtUtc { get; }
}
