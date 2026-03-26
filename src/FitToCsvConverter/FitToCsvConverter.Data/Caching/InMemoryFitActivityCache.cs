namespace FitToCsvConverter.Data.Caching;

using System.Collections.Concurrent;
using FitToCsvConverter.Data.Decoding;

public sealed class InMemoryFitActivityCache : IFitActivityCache
{
    private readonly ConcurrentDictionary<FitContentHash, FitActivityDecodeResult> cache = new();

    public bool TryGet(FitContentHash contentHash, FitFileSource source, out FitActivityDecodeResult result)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (cache.TryGetValue(contentHash, out FitActivityDecodeResult? cachedResult))
        {
            result = FitModelCloner.CloneResult(cachedResult, source, isFromCache: true);
            return true;
        }

        result = null!;
        return false;
    }

    public void Set(FitContentHash contentHash, FitActivityDecodeResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (!result.IsSuccess || result.Activity is null)
        {
            return;
        }

        cache[contentHash] = FitModelCloner.CloneResult(result, result.Source, isFromCache: false);
    }
}
