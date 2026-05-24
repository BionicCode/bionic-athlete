namespace BionicCode.Utilities.Net;

using System.Collections.Generic;
using System.Collections.ObjectModel;

public partial class ObservableHashSet<TItem>
{
    internal readonly struct IndexedHashSetDelta<TItem>
    {
        public IndexedHashSetDelta(ReadOnlyCollection<KeyValuePair<int, TItem>> removedItems, ReadOnlyCollection<KeyValuePair<int, TItem>> addedItems, bool hasChanges)
        {
            RemovedItems = removedItems;
            AddedItems = addedItems;
            HasChanges = hasChanges;
        }

        public ReadOnlyCollection<KeyValuePair<int, TItem>> RemovedItems { get; }
        public ReadOnlyCollection<KeyValuePair<int, TItem>> AddedItems { get; }
        public bool HasChanges { get; }
    }
}
