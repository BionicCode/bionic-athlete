namespace FitToCsvConverter.Data.Decoding;

public interface IFitActivityDecoder
{
    Task<FitActivityDecodeResult> DecodeFileAsync(string filePath, CancellationToken cancellationToken = default);

    Task<FitActivityDecodeResult> DecodeAsync(Stream stream, string? sourceName = null, CancellationToken cancellationToken = default);
}
