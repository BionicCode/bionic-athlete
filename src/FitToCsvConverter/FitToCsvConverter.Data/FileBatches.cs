namespace FitToCsvConverter.Data;

using System.Collections.ObjectModel;
using BionicCode.Utilities.Net;

public readonly struct FileBatches
{
    private readonly List<FileBatch> _batchesInternal;
    public ReadOnlyCollection<FileBatch> Batches { get; init; }
    public int TotalConversionCount { get; init; }

    public FileBatches(IEnumerable<FileBatch> conversionBatches)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(conversionBatches);

        _batchesInternal = [.. conversionBatches];
        Batches = _batchesInternal.AsReadOnly();
        TotalConversionCount = _batchesInternal.Sum(batch => batch.ConversionCount);
    }

    public FileBatches AddConversionInfoBatch(FileBatch conversionBatch)
    {
        ArgumentNullExceptionAdvanced.ThrowIfDefault(conversionBatch);

        // Preserve read-only-ness of this struct type
        var seed = _batchesInternal.ToList();
        seed.Add(conversionBatch);
        return this with
        {
            Batches = new ReadOnlyCollection<FileBatch>(seed),
            TotalConversionCount = TotalConversionCount + conversionBatch.ConversionCount
        };
    }
}
