namespace FitToCsvConverter.Data.Caching;

using FitToCsvConverter.Data.Decoding;

public interface IFitActivityCache
{
    bool TryGet(FitContentHash contentHash, FitFileSource source, out FitActivityDecodeResult result);

    void Set(FitContentHash contentHash, FitActivityDecodeResult result);
}
