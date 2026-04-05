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

    /// <summary>
    /// If <see cref="Action"/> is <see cref="NotifyCollectionChangedAction.Add"/> or <see cref="NotifyCollectionChangedAction.Remove"/>,
    /// this property contains the item that was added or removed for non-bulk operations.
    /// </summary>
    public TItem Item { get; }

    /// <summary>
    /// If <see cref="Action"/> is <see cref="NotifyCollectionChangedAction.Reset"/> or <see cref="NotifyCollectionChangedAction.Add"/>,
    /// this property contains the items that were added during the add operation if that operation was a bulk operation.
    /// </summary>
    public IList<TItem> AddedItems { get; }
    /// <summary>
    /// If <see cref="Action"/> is <see cref="NotifyCollectionChangedAction.Reset"/> or <see cref="NotifyCollectionChangedAction.Remove"/>,
    /// this property contains the items that were removed during the remove operation if that operation was a bulk operation.
    /// </summary>
    public IList<TItem> RemovedItems { get; }
}