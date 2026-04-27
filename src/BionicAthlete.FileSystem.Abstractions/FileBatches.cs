namespace BionicAthlete.FileSystem.Abstractions;

using BionicCode.Utilities.Net;

public readonly struct FileBatches
{
    public IEnumerable<FileBatch> Batches { get; init; }
    public int BatchesCount { get; init; }

    public FileBatches(IEnumerable<FileBatch> batches, int batchesCount)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(batches);

        Batches = batches;
        BatchesCount = batchesCount;
    }
}
