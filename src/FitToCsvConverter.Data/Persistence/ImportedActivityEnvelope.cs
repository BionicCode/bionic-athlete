namespace FitToCsvConverter.Data.Persistence;

using FitToCsvConverter.Data.Activities;

/// <summary>
/// Represents an imported activity together with local import metadata.
/// </summary>
public sealed class ImportedActivityEnvelope
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImportedActivityEnvelope"/> class.
    /// </summary>
    /// <param name="activity">The decoded activity to persist.</param>
    /// <param name="fingerprint">The stable import fingerprint for deduplication.</param>
    /// <param name="importedAtUtc">The UTC timestamp when the activity was imported into local history.</param>
    public ImportedActivityEnvelope(FitActivity activity, ActivityFingerprint fingerprint, DateTimeOffset importedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(activity);

        Activity = activity;
        Fingerprint = fingerprint;
        ImportedAtUtc = importedAtUtc;
    }

    /// <summary>
    /// Gets the decoded activity to persist.
    /// </summary>
    public FitActivity Activity { get; }

    /// <summary>
    /// Gets the stable import fingerprint for deduplication.
    /// </summary>
    public ActivityFingerprint Fingerprint { get; }

    /// <summary>
    /// Gets the UTC timestamp when the activity was imported into local history.
    /// </summary>
    public DateTimeOffset ImportedAtUtc { get; }
}
