namespace BionicCode.Utilities.Net;

using System.Collections.ObjectModel;

public partial class ObservableHashSet<TItem>
{
    internal readonly struct HashSetDelta<TItem>
    {
        public HashSetDelta(ReadOnlyCollection<TItem> removedItems, ReadOnlyCollection<TItem> addedItems, bool hasChanges)
        {
            RemovedItems = removedItems;
            AddedItems = addedItems;
            HasChanges = hasChanges;
        }

        public ReadOnlyCollection<TItem> RemovedItems { get; }
        public ReadOnlyCollection<TItem> AddedItems { get; }
        public bool HasChanges { get; }
    }
}
