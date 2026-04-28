namespace BionicAthlete.Training.Application.Caching;

using BionicAthlete.Training.Application.Decoding;
using BionicAthlete.Training.Domain;
using BionicAthlete.Training.Domain.Activities;

public interface IFitActivityCache
{
    bool TryGet(FitContentHash contentHash, FitFileSource source, out FitActivityDecodeResult result);

    void Set(FitContentHash contentHash, FitActivityDecodeResult result);
}
