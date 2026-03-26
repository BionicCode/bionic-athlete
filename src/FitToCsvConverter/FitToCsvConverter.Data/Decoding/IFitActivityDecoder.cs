namespace FitToCsvConverter.Data.Decoding;

public interface IFitActivityDecoder
{
    /// <summary>
    /// Decodes a FIT activity file from disk.
    /// </summary>
    Task<FitActivityDecodeResult> DecodeFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decodes a FIT activity stream.
    /// </summary>
    Task<FitActivityDecodeResult> DecodeAsync(Stream stream, string? sourceName = null, CancellationToken cancellationToken = default);
}
