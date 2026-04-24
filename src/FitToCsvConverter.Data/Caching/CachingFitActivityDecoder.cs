namespace FitToCsvConverter.Data.Caching;

using System.Security.Cryptography;
using FitToCsvConverter.Data.Decoding;

public sealed class CachingFitActivityDecoder : IFitActivityDecoder
{
    private readonly IFitActivityDecoder _innerDecoder;
    private readonly IFitActivityCache _cache;

    public CachingFitActivityDecoder(IFitActivityDecoder innerDecoder, IFitActivityCache cache)
    {
        ArgumentNullException.ThrowIfNull(innerDecoder);
        ArgumentNullException.ThrowIfNull(cache);

        _innerDecoder = innerDecoder;
        _cache = cache;
    }

    public async Task<FitActivityDecodeResult> DecodeFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        string fullPath = Path.GetFullPath(filePath);
        FileInfo fileInfo = new(fullPath);
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException($"FIT file '{fullPath}' does not exist.", fullPath);
        }

        FitFileSource source = new(fileInfo.Name, fileInfo.FullName, fileInfo.Length, fileInfo.LastWriteTimeUtc);

        await using FileStream fitStream = new(
            fullPath,
            new FileStreamOptions
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Share = FileShare.Read,
                Options = FileOptions.Asynchronous | FileOptions.SequentialScan
            });

        FitContentHash contentHash = await ComputeContentHashAsync(fitStream, cancellationToken).ConfigureAwait(false);
        if (_cache.TryGet(contentHash, source, out FitActivityDecodeResult cachedResult))
        {
            return cachedResult;
        }

        fitStream.Position = 0;
        FitActivityDecodeResult decodedResult = await _innerDecoder.DecodeAsync(fitStream, source.DisplayName, cancellationToken).ConfigureAwait(false);
        FitActivityDecodeResult normalizedResult = NormalizeResultSource(decodedResult, source, isFromCache: false);

        _cache.Set(contentHash, normalizedResult);
        return normalizedResult;
    }

    public async Task<FitActivityDecodeResult> DecodeAsync(Stream stream, string? sourceName = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        Stream preparedStream = await EnsureSeekableStreamAsync(stream, cancellationToken).ConfigureAwait(false);
        bool disposePreparedStream = !ReferenceEquals(preparedStream, stream);

        try
        {
            preparedStream.Position = 0;

            FitFileSource source = new(
                string.IsNullOrWhiteSpace(sourceName) ? "stream" : sourceName.Trim(),
                contentLength: preparedStream.Length);

            FitContentHash contentHash = await ComputeContentHashAsync(preparedStream, cancellationToken).ConfigureAwait(false);
            if (_cache.TryGet(contentHash, source, out FitActivityDecodeResult cachedResult))
            {
                return cachedResult;
            }

            preparedStream.Position = 0;
            FitActivityDecodeResult decodedResult = await _innerDecoder.DecodeAsync(preparedStream, source.DisplayName, cancellationToken).ConfigureAwait(false);
            FitActivityDecodeResult normalizedResult = NormalizeResultSource(decodedResult, source, isFromCache: false);

            _cache.Set(contentHash, normalizedResult);
            return normalizedResult;
        }
        finally
        {
            if (disposePreparedStream)
            {
                await preparedStream.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    private static FitActivityDecodeResult NormalizeResultSource(FitActivityDecodeResult result, FitFileSource source, bool isFromCache)
        => result.Source.FilePath == source.FilePath
            && result.Source.DisplayName == source.DisplayName
            && result.Source.ContentLength == source.ContentLength
            && result.Source.LastWriteTimeUtc == source.LastWriteTimeUtc
            && result.IsFromCache == isFromCache
                ? result
                : FitModelCloner.CloneResult(result, source, isFromCache);

    private static async Task<FitContentHash> ComputeContentHashAsync(Stream stream, CancellationToken cancellationToken)
    {
        long originalPosition = stream.Position;
        byte[] hashBytes = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        stream.Position = originalPosition;
        return FitContentHash.FromHashBytes(hashBytes);
    }

    private static async Task<Stream> EnsureSeekableStreamAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (stream.CanSeek)
        {
            return stream;
        }

        MemoryStream bufferedStream = new();
        await stream.CopyToAsync(bufferedStream, cancellationToken).ConfigureAwait(false);
        bufferedStream.Position = 0;
        return bufferedStream;
    }
}
