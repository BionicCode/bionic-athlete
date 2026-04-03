namespace BionicCode.Utilities.Net;

using System.Collections.ObjectModel;
using System.Collections.Specialized;

public class SetChangedEventArgs<TItem> : EventArgs
{
    public SetChangedEventArgs(NotifyCollectionChangedAction action, IList<TItem> addedItems, IList<TItem> removedItems)
    {
        ArgumentExceptionAdvanced.ThrowIfEnumIsNotDefined<NotifyCollectionChangedAction>(action);

        Action = action;
        Item = default!;
        AddedItems = addedItems ?? [];
        RemovedItems = removedItems ?? [];
    }

    public SetChangedEventArgs(NotifyCollectionChangedAction action, TItem item)
    {
        ArgumentExceptionAdvanced.ThrowIfEnumIsNotDefined<NotifyCollectionChangedAction>(action);

        Action = action;
        Item = item;
        AddedItems = [];
        RemovedItems = [];
    }

    public NotifyCollectionChangedAction Action { get; }
    public TItem Item { get; }
    public IList<TItem> AddedItems { get; }
    public IList<TItem> RemovedItems { get; }
}