namespace BionicAthlete.FileSystem.Abstractions;

using BionicCode.Utilities.Net;

public readonly struct FileBatches<T>
{
    public IEnumerable<T> Batches { get; init; }
    public int BatchesCount { get; init; }

    public FileBatches(IEnumerable<T> batches, int batchesCount)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(batches);

        Batches = batches;
        BatchesCount = batchesCount;
    }
}
