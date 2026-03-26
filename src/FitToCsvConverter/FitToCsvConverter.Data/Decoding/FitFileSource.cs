namespace FitToCsvConverter.Data.Decoding;

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

    public string DisplayName { get; }

    public string? FilePath { get; }

    public long? ContentLength { get; }

    public DateTimeOffset? LastWriteTimeUtc { get; }
}
