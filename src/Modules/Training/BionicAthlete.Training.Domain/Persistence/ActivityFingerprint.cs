namespace BionicAthlete.Training.Domain.Persistence;

/// <summary>
/// Represents a stable content-based identity for an imported activity.
/// </summary>
public readonly record struct ActivityFingerprint
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityFingerprint"/> struct.
    /// </summary>
    /// <param name="value">The stable fingerprint value.</param>
    public ActivityFingerprint(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    /// <summary>
    /// Gets the stable fingerprint value.
    /// </summary>
    public string Value { get; }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
