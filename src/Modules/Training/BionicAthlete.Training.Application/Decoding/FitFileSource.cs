namespace BionicAthlete.Training.Application.Decoding;

public sealed class FitFileSource
{
    public FitFileSource(string displayName, string? filePath = null, long? contentLength = null, DateTimeOffset? lastWriteTimeUtc = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        DisplayName = displayName;
        FilePath = filePath;
        ContentLength = contentLength;
        LastWriteTimeUtc = lastWriteTimeUtc;
    }

    /// <summary>
    /// Display name used in the application for this decoded source.
    /// </summary>
    public string DisplayName { get; }

    public string? FilePath { get; }

    public long? ContentLength { get; }

    public DateTimeOffset? LastWriteTimeUtc { get; }
}
