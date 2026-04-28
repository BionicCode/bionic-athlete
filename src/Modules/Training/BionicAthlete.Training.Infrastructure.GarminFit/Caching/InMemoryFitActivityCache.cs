namespace BionicAthlete.Training.Infrastructure.GarminFit.Caching;

using System.Collections.Concurrent;
using BionicAthlete.Training.Application.Caching;
using BionicAthlete.Training.Application.Decoding;
using BionicAthlete.Training.Domain;
using BionicAthlete.Training.Domain.Activities;
using BionicAthlete.Training.Application.Utilities;

public sealed class InMemoryFitActivityCache : IFitActivityCache
{
    private readonly ConcurrentDictionary<FitContentHash, FitActivityDecodeResult> _cache = new();

    public bool TryGet(FitContentHash contentHash, FitFileSource source, out FitActivityDecodeResult result)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (_cache.TryGetValue(contentHash, out FitActivityDecodeResult? cachedResult))
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

        _cache[contentHash] = FitModelCloner.CloneResult(result, result.Source, isFromCache: false);
    }
}
