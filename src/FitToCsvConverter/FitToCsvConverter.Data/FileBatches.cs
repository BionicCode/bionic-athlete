namespace FitToCsvConverter.Data;

using System.Collections.ObjectModel;
using BionicCode.Utilities.Net;

public readonly struct FileBatches
{
    public IEnumerable<FileBatch> Batches { get; init; }
    public int TotalConversionCount { get; init; }

    public FileBatches(IEnumerable<FileBatch> conversionBatches, int batchesCount)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(conversionBatches);

        Batches = conversionBatches;
        TotalConversionCount = batchesCount;
    }
}
