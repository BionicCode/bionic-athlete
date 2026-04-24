namespace FitToCsvConverter.Data.Sync;

using FitToCsvConverter.Data.Persistence;

/// <summary>
/// Describes one conflict returned by a future remote sync operation.
/// </summary>
public sealed class SyncConflict
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SyncConflict"/> class.
    /// </summary>
    public SyncConflict(ActivityFingerprint fingerprint, string code, string message, string? serverVersion = null, string? localVersion = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Fingerprint = fingerprint;
        Code = code;
        Message = message;
        ServerVersion = serverVersion;
        LocalVersion = localVersion;
    }

    /// <summary>
    /// Gets the activity fingerprint associated with the conflict when known.
    /// </summary>
    public ActivityFingerprint Fingerprint { get; }

    /// <summary>
    /// Gets the machine-readable conflict code.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the human-readable conflict description.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the current server-side version marker when supplied by the remote endpoint.
    /// </summary>
    public string? ServerVersion { get; }

    /// <summary>
    /// Gets the client-side version marker that the request was based on when known.
    /// </summary>
    public string? LocalVersion { get; }
}
