namespace BionicAthlete.Training.Domain.Caching;

using BionicAthlete.Training.Domain.Decoding;

public interface IFitActivityCache
{
    bool TryGet(FitContentHash contentHash, FitFileSource source, out FitActivityDecodeResult result);

    void Set(FitContentHash contentHash, FitActivityDecodeResult result);
}
