namespace BionicCode.Utilities.Net;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

public class ObservableHashSet<TItem> :
    ICollection<TItem>,
    IEnumerable<TItem>,
    IReadOnlyCollection<TItem>,
    ISet<TItem>,
    IList,
    IList<TItem>,
    IEnumerable,
    IReadOnlySet<TItem>,
    IDeserializationCallback,
    ISerializable,
    INotifyCollectionChanged,
    INotifyPropertyChanged
{
    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;
    /// <summary>When in hybrid mode, this event is raised to to provide index based collection change reports 
    /// the way they are required by data binding driven frameworks like WPF.
    /// <br/>In default hash set mode <see cref="CollectionChanged"/> event is broadcasted index agnostic, 
    /// meaning index is always reported as '-1'.</summary> 
    /// <remarks>The <see cref="ObservableHashSet{TItem}"/> operates as a true observable hash set by default. 
    /// But accessing any of the explicitly implemented <see cref="IList"/> or <see cref="IList{T}"/> member 
    /// will transition it into hybrid mode.
    /// <para/>In hybrid mode, <see cref="ObservableHashSet{TItem}"/> maintains a list projection in the background 
    /// in order to provide index based <see cref="CollectionChanged"/> events. This is to support enhanced UI optimizations like UI virtualization. 
    /// However, this support incurs performance penalties for remove operations and for <see cref="CollectionChanged"/> event broadcasting.
    /// The following list shows how the time complexity of various operations is affected when the collection is in hybrid mode compared to the default hash set mode (for operations not listed here since their are not affected see <see cref="HashSet{T}"/> online documentation):
    /// <list type="table">
    ///   <listheader>
    ///     <term>Operation</term>
    ///     <term>Default HashSet mode complexity</term>
    ///     <term>Hybrid mode complexity</term>
    ///   </listheader>
    ///   <item>
    ///     <term>Add</term>
    ///     <term>O(1)</term>
    ///     <term>O(1)</term>
    ///   </item>
    ///   <item>
    ///     <term>AddRange/UnionWith</term>
    ///     <term>O(n)</term>
    ///     <term>O(n+k)</term>
    ///   </item>
    ///   <item>
    ///     <term>Remove</term>
    ///     <term>O(1)</term>
    ///     <term>O(n)</term>
    ///   </item>
    ///   <item>
    ///     <term>RemoveRange/RemoveWhere</term>
    ///     <term>O(n)</term>
    ///     <term>O(n+k)</term>
    ///   </item>
    ///   <item>
    ///     <term>RemoveRange/RemoveWhere</term>
    ///     <term>If the other parameter is a HashSet<T> collection with the same equality comparer as the current HashSet<T> object, this method is an O(n) operation. Otherwise, this method is an O(n + m) operation, where n is the number of elements in other and m is Count.</term>
    ///     <term>O(n+k)</term>
    ///   </item>
    /// <item>Remove operations have degraded performance due to the need to maintain the list projection and index tables. The original O(1) operation becomes a O(n) operation if the removed item is not at the end. Removing the last item still yields a O(1) operation.</item>
    /// <item>Broadcasting <see cref="CollectionChanged"/> events incurs additional overhead due to the partitioning of changed index range into contiguous batches to improve event consumption. For non-contiguous indices <see cref="CollectionChanged"/> is raised per index.
    /// This will slightly degrade range operations from original O(n)</item>
    /// </list>
    /// When the collection is in hybrid mode, the <see cref="SetChanged"/> event is raised to provide index-based collection change notifications that are compatible with data-binding frameworks. This allows consumers to receive more granular notifications about changes to the collection, such as item additions, removals, and resets, along with the corresponding indices of the changes. When not in hybrid mode, only the <see cref="CollectionChanged"/> event is raised with index information where applicable.</remarks>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;
    public event EventHandler<SetChangedEventArgs<TItem>>? SetChanged;
    protected HashSet<TItem> Items { get; }
    private readonly Dictionary<TItem, int> _indexTable = [];
    private readonly Dictionary<int, TItem> _reverseIndexTable = [];
    private readonly List<TItem> _listProjection = [];
    private int _blockReentrancyCount;

    // Collection transitions into hybrid mode when IList or IList<T> API surface is used.
    // In this mode, the collection maintains the internal index tables and list projection
    // to support the list API, which incurs additional overhead and degraded remove complexity.
    // The collection can transition back to pure hash set mode when the list API is no longer used, at which point the index tables and list projection are cleared to optimize performance for hash set operations.
    private readonly WriteOnce<bool> _isInHybridMode;

    public ObservableHashSet() => Items = [];

#pragma warning disable IDE0028 // Simplify collection initialization. Feature not available (available only in preview).
    [SuppressMessage("Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>")]
    public ObservableHashSet(IEnumerable<TItem> collection)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(collection);

        Items = new HashSet<TItem>(collection);
        _listProjection = new(collection);
        _indexTable = new(Items.Comparer);
        _reverseIndexTable = new(EqualityComparer<int>.Default);
    }

    [SuppressMessage("Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>")]
    public ObservableHashSet(IEqualityComparer<TItem>? comparer)
    {
        Items = new(comparer);
        _indexTable = new(Items.Comparer);
        _reverseIndexTable = new(EqualityComparer<int>.Default);
    }

    [SuppressMessage("Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>")]
    public ObservableHashSet(int capacity)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(capacity);

        Items = new(capacity);
        _listProjection = new(capacity);
        _indexTable = new(capacity, Items.Comparer);
        _reverseIndexTable = new(EqualityComparer<int>.Default);
    }

    public ObservableHashSet(IEnumerable<TItem> collection, IEqualityComparer<TItem>? comparer)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(collection);

        Items = new HashSet<TItem>(collection, comparer);
        _indexTable = new(Items.Comparer);
        _reverseIndexTable = new(EqualityComparer<int>.Default);
    }

    public ObservableHashSet(int capacity, IEqualityComparer<TItem>? comparer)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(capacity);

        Items = new(capacity, comparer);
        _listProjection = new(capacity);
        _indexTable = new(capacity, Items.Comparer);
        _reverseIndexTable = new(EqualityComparer<int>.Default);
    }
#pragma warning restore IDE0028 // Simplify collection initialization. Feature not available (available only in preview).

    public int Capacity => Items.Capacity;
    /// <summary>
    /// The equality comparer used to determine equality of items in the set. This comparer is used for all operations that involve comparing items, such as adding, removing, and checking for the presence of items in the set.
    /// </summary>
    /// <removedItem>The equality <see cref="IEqualityComparer{T}"/> used by the set.</removedItem>
    public IEqualityComparer<TItem> Comparer => Items.Comparer;

    public int Count => Items.Count;

    /// <summary>Adds an item to the <see cref="ObservableHashSet{T}"/> if it is not already present.</summary>
    /// <param name="item">The item to add to the set. The removedItem can be <see langword="null"/> for reference types.</param>
    /// <remarks>Use this method to add an item to the set. If the item is already present, the set remains unchanged and the method returns <see langword="false"/>; otherwise, the item is added and the method returns <see langword="true"/>.
    /// <para/>This method raises the <see cref="CollectionChanged"/> event with <see cref="NotifyCollectionChangedAction.Add"/> action and the start index of the changes to support the <see cref="IList{T}"/> API surface.
    /// <para/>This method raises the <see cref="PropertyChanged"/> event for the <see cref="Count"/> property.</remarks>
    /// <returns><see langword="true"/> if the item was added to the set; <see langword="false"/> if the item was already present.</returns>
    /// <exception cref="InvalidOperationException">Thrown when called from an <see cref="CollectionChanged"/> event  handler.</exception>
    public bool Add(TItem item)
    {
        CheckReentrancy();

        if (AddInternal(item, out int newIndex))
        {
            OnCountChanged();
            OnIndexerChanged();
            OnCollectionChanged(NotifyCollectionChangedAction.Add, item, newIndex);

            return true;
        }

        return false;
    }

    private bool AddInternal(TItem item, out int newIndex)
    {
        if (AddItem(item))
        {
            _ = RegisterItem(item, out newIndex);

            return true;
        }

        newIndex = -1;
        return false;
    }

    /// <summary>Adds an item to the <see cref="ObservableHashSet{T}"/> if it is not already present.</summary>
    /// <param name="item">The item to add to the set. The removedItem can be <see langword="null"/> for reference types.</param>
    /// <remarks>Override this method to extend the behavior of the <see cref="ObservableHashSet{TItem}.Add(TItem)"/> member without affecting the notification behavior.</remarks>
    /// <returns><see langword="true"/> if the item was added to the set; <see langword="false"/> if the item was already present.</returns>
    protected virtual bool AddItem(TItem item) => Items.Add(item);

    /// <summary>
    /// Adds the elements of the specified collection to the current collection.
    /// </summary>
    /// <remarks>Only items that are successfully added are included in the operation. Duplicate or invalid
    /// items may be ignored depending on the collection's rules.
    /// <para/>This method raises the <see cref="CollectionChanged"/> event with <see cref="NotifyCollectionChangedAction.Add"/> action and the start index of the changes to support the <see cref="IList{T}"/> API surface.
    /// <para/>This method raises the <see cref="PropertyChanged"/> event for the <see cref="Count"/> property.</remarks>
    /// <param name="items">The collection of items to add. Cannot be <see langword="null"/>.</param>
    public void AddRange(IEnumerable<TItem> items)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(items);

        CheckReentrancy();

        if (AddRangeInternal(items, out IList<TItem> addedItems, out int rangeStartIndex))
        {
            OnCountChanged();
            OnIndexerChanged();
            OnCollectionChanged(NotifyCollectionChangedAction.Add, addedItems, rangeStartIndex);
        }
    }

    private bool AddRangeInternal(IEnumerable<TItem> items, out IList<TItem> addedItems, out int rangeStartIndex)
    {
        addedItems = [];
        rangeStartIndex = _listProjection.Count;
        foreach (TItem item in items)
        {
            if (AddItem(item))
            {
                _ = RegisterItem(item, out _);
                addedItems.Add(item);
            }
        }

        return addedItems.Count > 0;
    }

    /// <summary>
    /// Attempts to find a removedItem in the set that is equal to the specified item.
    /// </summary>
    /// <param name="equalValue">The item to search for in the set. Equality is determined by the set's comparer.</param>
    /// <param name="actualValue">When this method returns <see langword="true"/>, contains the removedItem from the set that is equal to <paramref
    /// name="equalValue"/>; otherwise, contains the default removedItem for the type.</param>
    /// <returns><see langword="true"/> if a removedItem equal to <paramref name="equalValue"/> was found in the set; otherwise, <see
    /// langword="false"/>.</returns>
    public bool TryGetValue(TItem equalValue, [MaybeNullWhen(false)] out TItem actualValue) => TryGetItem(equalValue, out actualValue);

    /// <summary>
    /// Attempts to find an item in the collection that is equal to the specified removedItem.
    /// </summary>
    /// <remarks>Override this method to extend the behavior of the <see cref="ObservableHashSet{TItem}.TryGetValue(TItem, out TItem)"/> member without affecting the notification behavior.</remarks>
    /// <param name="equalValue">The removedItem to search for in the collection. Equality is determined by the collection's comparer.</param>
    /// <param name="actualValue">When this method returns, contains the actual item from the collection that is equal to <paramref
    /// name="equalValue"/>, if found; otherwise, the default removedItem for the type of the item.</param>
    /// <returns><see langword="true"/> if an item equal to <paramref name="equalValue"/> is found; otherwise, <see
    /// langword="false"/>.</returns>
    protected virtual bool TryGetItem(TItem equalValue, [MaybeNullWhen(false)] out TItem actualValue) => Items.TryGetValue(equalValue, out actualValue);

    /// <summary>
    /// Attempts to remove the specified item from the set.
    /// </summary>
    /// <param name="item">The item to remove from the set. The removedItem can be <see langword="null"/> for reference types.</param>
    /// <returns><see langword="true"/> if the item was successfully removed; otherwise, <see langword="false"/>.</returns>
    /// <remarks>Use this method to remove the item <paramref name="item"/> from the set and return a removedItem indicating whether the removal was successful.
    /// <para/>This method raises the <see cref="CollectionChanged"/> event with <see cref="NotifyCollectionChangedAction.Remove"/> action and a change index for the <see cref="IList{T}"/> API surface.
    /// <para/>This method raises the <see cref="PropertyChanged"/> event for the <see cref="Count"/> property.</remarks>
    public bool Remove(TItem item)
    {
        CheckReentrancy();

        if (RemoveInternal(item, isRebuildIndexRequired: true, out int removeIndex))
        {
            OnCountChanged();
            OnIndexerChanged();
            OnCollectionChanged(NotifyCollectionChangedAction.Remove, item, removeIndex);

            return true;
        }

        return false;
    }

    private bool RemoveInternal(TItem item, bool isRebuildIndexRequired, out int itemIndex)
    {
        itemIndex = -1;
        if (RemoveItem(item))
        {
            _ = UnregisterItem(item, isRebuildIndexRequired, out itemIndex);

            return true;
        }

        return false;
    }

    /// <summary>
    /// Removes the specified items from the collection.
    /// </summary>
    /// <remarks>Only items that exist in the collection are removed. The method has no effect for items that
    /// are not present.
    /// <para/>This method raises the <see cref="CollectionChanged"/> event with <see cref="NotifyCollectionChangedAction.Reset"/> action.
    /// <para/>This method raises the <see cref="PropertyChanged"/> event for the <see cref="Count"/> property.</remarks>
    /// <param name="items">The collection of items to remove from the collection. Cannot be null.</param>
    public void RemoveRange(IEnumerable<TItem> items, out IList<TItem> removedItems)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(items);

        CheckReentrancy();

        if (RemoveRangeInternal(items, isRebuildIndexRequired: true, out removedItems, out _))
        {
            OnCountChanged();
            OnIndexerChanged();
            OnCollectionChangedReset();
        }
    }

    private bool RemoveRangeInternal(IEnumerable<TItem> items, bool isRebuildIndexRequired, out IList<TItem> removedItems, out IList<int> removedIndices)
    {
        removedIndices = [];
        removedItems = [];

        int smallestChangeIndex = int.MaxValue;
        foreach (TItem item in items)
        {
            if (RemoveItem(item))
            {
                removedItems.Add(item);

                if (UnregisterItem(item, isRebuildIndexRequired: false, out int itemIndex))
                {
                    removedIndices.Add(itemIndex);
                    smallestChangeIndex = Math.Min(smallestChangeIndex, itemIndex);
                }
            }
        }

        bool hasChanges = removedItems.Count > 0;
        if (hasChanges && isRebuildIndexRequired)
        {
            BuildIndex(smallestChangeIndex);
        }

        return hasChanges;
    }

    /// <summary>
    /// Removes the specified item from the collection.
    /// </summary>
    /// <remarks>Override this method to extend the behavior of the <see cref="ObservableHashSet{TItem}.RemoveItem(TItem)"/> member without affecting the notification behavior.</remarks>
    /// <param name="item">The item to remove from the set. The removedItem can be <see langword="null"/> for reference types.</param>
    /// <returns><see langword="true"/> if the item was successfully removed; otherwise, <see langword="false"/>.</returns>
    protected virtual bool RemoveItem(TItem item) => Items.Remove(item);

    /// <summary>
    /// Removes all elements from the collection that match the conditions defined by the specified predicate.
    /// </summary>
    /// <remarks>If one or more elements are removed, the collection raises change notifications. Use this
    /// method to efficiently remove multiple items based on custom criteria.
    /// <para/>This method raises the <see cref="CollectionChanged"/> event with <see cref="NotifyCollectionChangedAction.Reset"/> action.
    /// <para/>This method raises the <see cref="PropertyChanged"/> event for the <see cref="Count"/> property.</remarks>
    /// <param name="match">A delegate that defines the conditions of the elements to remove. Cannot be <see langword="null"/>.</param>
    /// <returns>The number of elements removed from the collection.</returns>
    public int RemoveWhere([DisallowNull] Predicate<TItem> match)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(match);

        CheckReentrancy();

        int removedCount = 0;
        int smallestChangeIndex = int.MaxValue;
        foreach (TItem item in Items)
        {
            if (match.Invoke(item)
                && RemoveInternal(item, isRebuildIndexRequired: false, out int itemIndex))
            {
                removedCount++;
                smallestChangeIndex = Math.Min(smallestChangeIndex, itemIndex);
            }
        }

        if (removedCount > 0)
        {
            BuildIndex(smallestChangeIndex);

            OnCountChanged();
            OnIndexerChanged();
            OnCollectionChangedReset();
        }

        return removedCount;
    }

    /// <summary>
    /// Removes all elements from the collection that match the conditions defined by the specified predicate.
    /// </summary>
    /// <remarks>Override this method to extend the behavior of the <see cref="ObservableHashSet{TItem}.RemoveWhere(Predicate{TItem})"/> member without affecting the notification behavior.</remarks>
    /// <param name="match">A delegate that defines the conditions of the elements to remove. Cannot be <see langword="null"/>.</param>
    /// <returns>The number of elements removed from the collection.</returns>
    protected virtual int RemoveItemWhere([DisallowNull] Predicate<TItem> match) => Items.RemoveWhere(match);

    /// <summary>
    /// Removes all objects from the <see cref="ObservableHashSet{TItem}"/>.
    /// </summary>
    /// <remarks>Use this method to clear the set. This method raises the <see cref="CollectionChanged"/> event with <see cref="NotifyCollectionChangedAction.Reset"/> action.
    /// <para/>This method raises the <see cref="PropertyChanged"/> event for the <see cref="Count"/> property.</remarks>
    public void Clear()
    {
        CheckReentrancy();

        if (Count > 0)
        {
            ClearItems();
            _indexTable.Clear();
            _reverseIndexTable.Clear();
            _listProjection.Clear();

            OnCountChanged();
            OnIndexerChanged();
            OnCollectionChangedReset();
        }
    }

    /// <summary>
    /// Removes all objects from the <see cref="ObservableHashSet{TItem}"/>.
    /// </summary>
    /// <remarks>Override this method to extend the behavior of the <see cref="ObservableHashSet{TItem}.Clear"/> member without affecting the notification behavior.</remarks>
    protected virtual void ClearItems() => Items.Clear();

    /// <summary>
    /// Determines whether an element is in the <see cref="ObservableHashSet{T}"/>.
    /// </summary>
    /// <param name="item">The object to locate in the <see cref="ObservableHashSet{T}"/>. The removedItem can be <see langword="null"/> for reference types.</param>
    /// <returns><see langword="true"/> if the <see cref="ObservableHashSet{T}"/> contains the specified element; otherwise, <see langword="false"/>.</returns>
    public bool Contains(TItem item) => Items.Contains(item);
    /// <summary>
    /// Copies the elements of the collection to the specified array, starting at the given array index.
    /// </summary>
    /// <param name="array">The destination array that will receive the copied elements. Must be large enough to contain the elements from
    /// the collection.</param>
    /// <param name="arrayIndex">The zero-based index in the destination array at which copying begins.</param>
    public void CopyTo(TItem[] array, int arrayIndex) => Items.CopyTo(array, arrayIndex);
    /// <summary>
    /// Creates an equality comparer that can be used to compare two hash sets for set equality.
    /// </summary>
    /// <remarks>The returned comparer considers two hash sets equal if they have the same elements, even if
    /// the order differs. This is useful for scenarios where set semantics are required, such as using hash sets as
    /// keys in dictionaries.</remarks>
    /// <returns>An equality comparer that determines whether two hash sets contain the same elements, regardless of order.</returns>
    public static IEqualityComparer<ObservableHashSet<TItem>> CreateSetComparer() => ObservableHashSetEqualityComparer<TItem>.Instance;
    /// <summary>
    /// Returns an array containing the elements of the set in the order they would be enumerated.
    /// </summary>
    /// <returns>An array of type TItem containing all elements in the set. The array will be empty if the set contains no
    /// elements.</returns>
    public TItem[] ToArray() => Items.ToArray();
    /// <summary>
    /// Returns a read-only wrapper around the current set.
    /// </summary>
    /// <remarks>The returned <see cref="ReadOnlyObservableHashSet{TItem}"/> reflects all subsequent changes made to the current set and forwards the corresponding collection and property change notifications.</remarks>
    /// <returns>A read-only wrapper over the current <see cref="ObservableHashSet{TItem}"/>.</returns>
    public ReadOnlyObservableHashSet<TItem> AsReadOnly() => new(this);
    /// <summary>
    /// Reduces the capacity of the <see cref="ObservableHashSet{TItem}"/> to match the actual number of elements, minimizing memory overhead.
    /// </summary>
    /// <remarks>Use this method to optimize memory usage after removing a significant number of elements from
    /// the set. Calling this method may incur a performance cost due to internal array resizing.
    /// <para/>This method raises the <see cref="PropertyChanged"/> event for the <see cref="Capacity"/> property.</remarks>
    public void TrimExcess()
    {
        Items.TrimExcess();
        OnCapacityChanged();
    }
    /// <summary>
    /// Reduces the memory overhead by adjusting the internal storage to the specified capacity, if possible.
    /// </summary>
    /// <remarks>Use this method to minimize memory usage when the queue is expected to remain at or below the
    /// specified capacity. If the current number of elements exceeds the specified capacity, no trimming
    /// occurs.
    /// <para/>This method raises the <see cref="PropertyChanged"/> event for the <see cref="Capacity"/> property.</remarks>
    /// <param name="capacity">The target capacity for the internal storage after trimming. Must be non-negative.</param>
    public void TrimExcess(int capacity)
    {
        Items.TrimExcess(capacity);
        OnCapacityChanged();
    }

    /// <summary>
    /// Ensures that the <see cref="ObservableHashSet{T}"/> can accommodate at least the specified number of elements without resizing.
    /// </summary>
    /// <param name="capacity">The minimum number of elements that the hash set should be able to hold. Must be non-negative.</param>
    /// <returns>The new capacity of the hash set after ensuring the specified minimum capacity.</returns>
    /// <remarks>Use this method to optimize performance when you know in advance that the set will grow to a certain size. Ensuring capacity can reduce the number of internal resizes, which can improve performance when adding a large number of elements.
    /// <para/>This method raises the <see cref="PropertyChanged"/> event for the <see cref="Capacity"/> property.</remarks>
    public int EnsureCapacity(int capacity)
    {
        int newCapacity = Items.EnsureCapacity(capacity);
        OnCapacityChanged();

        return newCapacity;
    }

    /// <summary>
    /// Removes all elements in the specified collection from the current set.
    /// </summary>
    /// <remarks>If the specified collection contains elements that are not present in the set, those elements
    /// are ignored. The operation modifies the current set and does not return a removedItem.
    /// <para/>This method raises the <see cref="CollectionChanged"/> event with <see cref="NotifyCollectionChangedAction.Reset"/> action.
    /// <para/>This method raises the <see cref="PropertyChanged"/> event for the <see cref="Count"/> property.</remarks>
    /// <param name="other">The collection of elements to remove from the set. Cannot be <see langword="null"/>.</param>
    public void ExceptWith(IEnumerable<TItem> other)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(other);

        CheckReentrancy();

        HashSet<TItem> oldState = TakeSnapshot();
        Items.ExceptWith(other);

        HashSetDelta<TItem> hashSetDelta = GetDelta(oldState, DeltaType.Remove);
        _ = UnregisterItems(hashSetDelta.RemovedItems, out _);

        OnCountChanged();
        OnIndexerChanged();
        OnCollectionChangedReset();
    }

    /// <summary>
    /// Creates an alternate lookup structure for items in the set using the specified alternate type.
    /// </summary>
    /// <remarks>Use this method to efficiently perform lookups based on a different removedItemIndex or representation of
    /// the items. The alternate lookup is valid only within the scope of the <see langword="ref"/> <see langword="struct"/> and cannot be stored or used
    /// outside its lifetime.</remarks>
    /// <typeparam name="TAlternate">The alternate type used for lookup. Must be a <see langword="ref"/> <see langword="struct"/>.</typeparam>
    /// <returns>An alternate lookup object that enables searching for items using the specified alternate type.</returns>
    public HashSet<TItem>.AlternateLookup<TAlternate> GetAlternateLookup<TAlternate>() where TAlternate : allows ref struct => Items.GetAlternateLookup<TAlternate>();

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the items in the collection.</returns>
    public Enumerator GetEnumerator() => new(_listProjection);
    /// <summary>
    /// Modifies the current set to contain only elements that are also in the specified collection.
    /// </summary>
    /// <remarks>This method removes any elements from the current set that are not present in the specified
    /// collection. The operation does not preserve the order of elements.
    /// <para/>This method raises the <see cref="CollectionChanged"/> event with <see cref="NotifyCollectionChangedAction.Reset"/> action.
    /// <para/>This method raises the <see cref="PropertyChanged"/> event for the <see cref="Count"/> property.</remarks>
    /// <param name="other">The collection to compare to the current set. Cannot be null.</param>
    public void IntersectWith(IEnumerable<TItem> other)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(other);

        CheckReentrancy();

        HashSet<TItem> oldState = TakeSnapshot();
        Items.IntersectWith(other);

        HashSetDelta<TItem> hashSetDelta = GetDelta(oldState, DeltaType.AddAndRemove);
        _ = UnregisterItems(hashSetDelta.RemovedItems, out _);
        _ = RegisterItems(hashSetDelta.AddedItems, out _);

        OnCountChanged();
        OnIndexerChanged();
        OnCollectionChangedReset();
    }

    /// <summary>
    /// Determines whether the current set is a proper subset of the specified collection.
    /// </summary>
    /// <remarks>A set is a proper subset of another collection if all elements of the set are contained in
    /// the collection and the collection contains at least one element not in the set. If the specified collection is
    /// <see langword="null"/>, an <see cref="ArgumentNullException"/> is thrown.</remarks>
    /// <param name="other">The collection to compare to the current set. Cannot be <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the current set is a proper subset of the specified collection; otherwise, <see langword="false"/>.</returns>
    public bool IsProperSubsetOf(IEnumerable<TItem> other) => Items.IsProperSubsetOf(other);
    /// <summary>
    /// Determines whether the current set is a proper superset of the specified collection.
    /// </summary>
    /// <remarks>A set is a proper superset of another collection if all elements of the other collection are contained in
    /// the set and the set contains at least one element not in the other collection. If the specified collection is
    /// <see langword="null"/>, an <see cref="ArgumentNullException"/> is thrown.</remarks>
    /// <param name="other">The collection to compare to the current set. Cannot be <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the current set is a proper superset of the specified collection; otherwise, <see langword="false"/>.</returns>
    public bool IsProperSupersetOf(IEnumerable<TItem> other) => Items.IsProperSupersetOf(other);
    /// <summary>
    /// Determines whether the current set is a subset of the specified collection.
    /// </summary>
    /// <param name="other">The collection to compare to the current set. Cannot be <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the current set is a subset of the specified collection; otherwise, <see langword="false"/>.</returns>
    public bool IsSubsetOf(IEnumerable<TItem> other) => Items.IsSubsetOf(other);
    /// <summary>
    /// Determines whether the current set is a superset of the specified collection.
    /// </summary>
    /// <param name="other">The collection to compare to the current set. Cannot be <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the current set is a superset of the specified collection; otherwise, <see langword="false"/>.</returns>
    public bool IsSupersetOf(IEnumerable<TItem> other) => Items.IsSupersetOf(other);
    /// <summary>
    /// Handles the deserialization event for the collection, restoring its state after being deserialized.
    /// </summary>
    /// <remarks>Call this method after the collection has been deserialized to ensure its internal state is
    /// properly restored. This is commonly used when implementing custom serialization logic.</remarks>
    /// <param name="sender">The source of the deserialization event. This parameter is typically not used.</param>
    public void OnDeserialization(object? sender) => Items.OnDeserialization(sender);
    /// <summary>
    /// Determines whether the current set and the specified collection share any common elements.
    /// </summary>
    /// <param name="other">The collection to compare to the current set. Cannot be <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the current set and the specified collection share at least one common element; otherwise, <see langword="false"/>.</returns>
    public bool Overlaps(IEnumerable<TItem> other) => Items.Overlaps(other);
    /// <summary>
    /// Determines whether the current set contains exactly the same elements as the specified collection.
    /// </summary>
    /// <remarks>Set equality is determined by comparing the unique elements in both collections, regardless
    /// of order. The comparison ignores duplicate elements in the input collection.</remarks>
    /// <param name="other">The collection to compare to the current set. The elements are compared for equality, and duplicate elements are
    /// ignored.</param>
    /// <returns><see langword="true"/> if the current set and the specified collection contain the same elements; otherwise, <see langword="false"/>.</returns>
    public bool SetEquals(IEnumerable<TItem> other) => Items.SetEquals(other);
    /// <summary>
    /// Modifies the current set so that it contains only elements that are present in either the set or the specified
    /// collection, but not both. This operation is also known as the symmetric difference of two sets. 
    /// <br/>If item is present in both sets, it will be removed; if it is present in only one of the sets, it will be added to the current set.
    /// </summary>
    /// <remarks>The symmetric difference operation removes elements that appear in both the current set and
    /// the specified collection, and adds elements that appear in either set but not both. If the specified collection
    /// contains duplicate elements, only unique elements are considered. This method does not return a removedItem; it
    /// modifies the current set in place.
    /// <para/>This method raises the <see cref="CollectionChanged"/> event with <see cref="NotifyCollectionChangedAction.Reset"/> action.
    /// <para/>This method raises the <see cref="PropertyChanged"/> event for the <see cref="Count"/> property.</remarks>
    /// <param name="other">The collection whose symmetric difference with the current set is to be computed. Cannot be <see langword="null"/>.</param>
    public void SymmetricExceptWith(IEnumerable<TItem> other)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(other);

        CheckReentrancy();

        // If set is empty, then symmetric difference is other.
        if (Count == 0)
        {
            UnionWith(other);
            return;
        }

        // Special-case this; the symmetric difference of a set with itself is the empty set.
        if (ReferenceEquals(other, this))
        {
            Clear();

            return;
        }

        HashSet<TItem> otherHashSet = NormalizeEnumerableArgument(other);

        if (_isInHybridMode)
        {
            IndexedHashSetDelta<TItem> hashSetDelta = HybridModeSymmetricExceptWithUniqueHashSetInternal(otherHashSet);
            BroadcastIndexBasedCollectionChanges(hashSetDelta);
        }
        else
        {
            HashSetDelta<TItem> hashSetDelta = SymmetricExceptWithUniqueHashSetInternal(otherHashSet);
            BroadcastDefaultSetChangedEvents(hashSetDelta.AddedItems, hashSetDelta.RemovedItems);
        }
    }

    // Normalize to HashSet<T>
    private HashSet<TItem> NormalizeEnumerableArgument(IEnumerable<TItem> other) => other is ObservableHashSet<TItem> observableHashSet && Comparer.Equals(observableHashSet.Comparer)
        ? observableHashSet.Items
        : other is HashSet<TItem> hashSet && Comparer.Equals(hashSet.Comparer)
            ? hashSet
            : new HashSet<TItem>(other, Comparer);

    private void BroadcastIndexBasedCollectionChanges(IndexedHashSetDelta<TItem> hashSetDelta)
    {
        if (!hashSetDelta.HasChanges)
        {
            return;
        }

        List<TItem> removedItems = [];
        List<TItem> addedItems = [];

        if (hashSetDelta.RemovedItems.Count > 0)
        {
            removedItems = DispatchIndexGroups(hashSetDelta.RemovedItems, NotifyCollectionChangedAction.Remove);
        }

        if (hashSetDelta.AddedItems.Count > 0)
        {
            addedItems = DispatchIndexGroups(hashSetDelta.AddedItems, NotifyCollectionChangedAction.Add);
        }

        BroadcastDefaultSetChangedEvents(addedItems, removedItems);
    }

    private List<TItem> DispatchIndexGroups(ReadOnlyDictionary<int, TItem> items, NotifyCollectionChangedAction collectionChangedAction)
    {
        List<TItem> processedItems = [];
        List<TItem> currentItemsGroup = [];
        int firstIndexInGroup = -1;
        int lastIndexInGroup = -1;
        foreach (KeyValuePair<int, TItem> itemEntry in items)
        {
            int itemIndex = itemEntry.Key;
            TItem item = itemEntry.Value;
            processedItems.Add(item);

            if (firstIndexInGroup == -1)
            {
                firstIndexInGroup = itemIndex;
                lastIndexInGroup = firstIndexInGroup;

                currentItemsGroup.Add(item);
                continue;
            }

            if (itemIndex - 1 == lastIndexInGroup)
            {
                lastIndexInGroup = itemIndex;
                currentItemsGroup.Add(item);
                continue;
            }

            // Close group and raise events
            if (currentItemsGroup.Count > 1)
            {
                OnCollectionChanged(collectionChangedAction, currentItemsGroup, firstIndexInGroup);
            }
            else
            {
                OnCollectionChanged(collectionChangedAction, currentItemsGroup[0], firstIndexInGroup);
            }

            // Start new group and initialize with current item
            currentItemsGroup.Clear();
            currentItemsGroup.Add(item);
            firstIndexInGroup = itemIndex;
            lastIndexInGroup = firstIndexInGroup;
        }

        // Finish last group if needed
        if (currentItemsGroup.Count > 0)
        {
            if (currentItemsGroup.Count > 1)
            {
                OnCollectionChanged(collectionChangedAction, currentItemsGroup, firstIndexInGroup);
            }
            else
            {
                OnCollectionChanged(collectionChangedAction, currentItemsGroup[0], firstIndexInGroup);
            }
        }

        return processedItems;
    }

    private void BroadcastDefaultSetChangedEvents(IList<TItem> addedItems, IList<TItem> removedItems)
    {
        if (addedItems.Count == 0
            && removedItems.Count == 0)
        {
            return;
        }

        OnCountChanged();
        OnIndexerChanged();
        if (addedItems.Count > 0
            && removedItems.Count > 0)
        {
            OnSetChanged(NotifyCollectionChangedAction.Reset, addedItems, removedItems);

            if (!_isInHybridMode)
            {
                OnCollectionChangedReset();
            }
        }
        else if (addedItems.Count > 0)
        {
            OnSetChanged(NotifyCollectionChangedAction.Add, addedItems, []);

            if (!_isInHybridMode)
            {
                OnCollectionChanged(NotifyCollectionChangedAction.Add, addedItems, -1);
            }
        }
        else if (removedItems.Count > 0)
        {
            OnSetChanged(NotifyCollectionChangedAction.Remove, [], removedItems);

            if (!_isInHybridMode)
            {
                OnCollectionChanged(NotifyCollectionChangedAction.Remove, removedItems, -1);
            }
        }
    }

    private IndexedHashSetDelta<TItem> HybridModeSymmetricExceptWithUniqueHashSetInternal(HashSet<TItem> other)
    {
        var removedItems = new SortedList<int, TItem>();
        var addedItems = new SortedList<int, TItem>();

        int smallestChangeIndex = int.MaxValue;
        foreach (TItem item in other)
        {
            if (RemoveInternal(item, isRebuildIndexRequired: false, out int itemIndex))
            {
                removedItems.Add(itemIndex, item);
                smallestChangeIndex = Math.Min(smallestChangeIndex, itemIndex);
            }
            else
            {
                _ = AddInternal(item, out itemIndex);

                addedItems.Add(itemIndex, item);
            }
        }

        bool hasChanges = addedItems.Count > 0 || removedItems.Count > 0;
        if (hasChanges)
        {
            BuildIndex(smallestChangeIndex);
        }

        return new IndexedHashSetDelta<TItem>(addedItems.AsReadOnly(), removedItems.AsReadOnly(), hasChanges);
    }

    private HashSetDelta<TItem> SymmetricExceptWithUniqueHashSetInternal(HashSet<TItem> other)
    {
        var removedItems = new List<TItem>();
        var addedItems = new List<TItem>();

        foreach (TItem item in other)
        {
            if (RemoveInternal(item, isRebuildIndexRequired: false, out _))
            {
                removedItems.Add(item);
            }
            else
            {
                _ = AddItem(item);
                addedItems.Add(item);
            }
        }

        bool hasChanges = addedItems.Count > 0 || removedItems.Count > 0;
        return new HashSetDelta<TItem>(addedItems.AsReadOnly(), removedItems.AsReadOnly(), hasChanges);
    }

    /// <summary>
    /// Determines whether the current set is a subset of, a superset of, or equal to the specified collection.
    /// </summary>
    /// <typeparam name="TAlternate">The type of the elements in the alternate lookup.</typeparam>
    /// <param name="lookup">The alternate lookup to compare with the current set.</param>
    /// <returns><see langword="true"/> if the alternate lookup is valid; otherwise, <see langword="false"/>.</returns>
    public bool TryGetAlternateLookup<TAlternate>(out HashSet<TItem>.AlternateLookup<TAlternate> lookup) where TAlternate : allows ref struct => Items.TryGetAlternateLookup(out lookup);
    /// <summary>
    /// Adds all elements from the specified collection to the current set.
    /// </summary>
    /// <remarks>Duplicate elements in the specified collection are ignored. The set will contain each unique
    /// element from both the original set and the specified collection after the operation completes.
    /// <para/>This method raises the <see cref="CollectionChanged"/> event with <see cref="NotifyCollectionChangedAction.Add"/> action and the start index of the changes to support the <see cref="IList{T}"/> API surface.
    /// <para/>This method raises the <see cref="PropertyChanged"/> event for the <see cref="Count"/> property.</remarks>
    /// <param name="other">The collection whose elements are to be added to the set. Cannot be <see langword="null"/>.</param>
    public void UnionWith(IEnumerable<TItem> other)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(other);

        CheckReentrancy();

        int changeStartIndex = _listProjection.Count;
        List<TItem> addedItems = [];
        foreach (TItem item in other)
        {
            if (Items.Add(item))
            {
                _ = RegisterItem(item, out _);
                addedItems.Add(item);
            }
        }

        if (addedItems.Count == 0)
        {
            return;
        }

        OnCountChanged();
        OnIndexerChanged();
        OnCollectionChanged(NotifyCollectionChangedAction.Add, addedItems, changeStartIndex);
    }

    /// <summary>
    /// Registers the specified items with the index tables and list projection, and returns the indices of the registered items.
    /// </summary>
    /// <remarks>The result indices list <paramref name="indices"/> will be populated with the indices of the registered items <b>in the original order</b> in which they were provided by the <paramref name="addedItems"/> collection.</remarks>
    /// <param name="addedItems">The collection of items to be registered.</param>
    /// <param name="indices">The list that will be populated with the indices of the registered items <b>in the original order</b> in which they were provided by the <paramref name="addedItems"/> collection.</param>
    /// <returns><see langword="true"/> if any items were registered; otherwise, <see langword="false"/>.</returns>
    private bool RegisterItems(IEnumerable<TItem> addedItems, out IList<int> indices)
    {
        indices = [];
        if (!_isInHybridMode)
        {
            return false;
        }

        foreach (TItem removedItem in addedItems)
        {
            if (RegisterItem(removedItem, out int itemIndex))
            {
                indices.Add(itemIndex);
            }
        }

        return true;
    }

    private bool RegisterItem(TItem item, out int itemIndex)
    {
        itemIndex = -1;
        if (!_isInHybridMode)
        {
            return false;
        }

        _listProjection.Add(item);
        int newIndex = _listProjection.Count - 1;
        _indexTable[item] = newIndex;
        _reverseIndexTable[newIndex] = item;
        itemIndex = newIndex;

        return true;
    }

    private void UpdateItemAt(TItem newItem, TItem oldItem, int index)
    {
        if (!_isInHybridMode)
        {
            return;
        }

        _listProjection[index] = newItem;
        _ = _indexTable.Remove(oldItem);
        _indexTable[newItem] = index;
        _reverseIndexTable[index] = newItem;
    }

    private void RegisterInsertedItem(TItem item, int changeIndex, bool isRebuildIndexRequired)
    {
        if (!_isInHybridMode)
        {
            return;
        }

        _listProjection.Insert(changeIndex, item);
        _indexTable[item] = changeIndex;
        _reverseIndexTable[changeIndex] = item;
        if (isRebuildIndexRequired)
        {
            BuildIndex(changeIndex);
        }
    }

    /// <summary>
    /// Unregisters the specified items from the index tables and list projection, and returns the indices of the removed items.
    /// </summary>
    /// <remarks>The result indices list <paramref name="indices"/> will be populated with the indices of the removed items <b>in the original order</b> in which they were provided by the <paramref name="removedItems"/> collection.</remarks>
    /// <param name="removedItems">The collection of items to be removed.</param>
    /// <param name="indices">The list that will be populated with the indices of the removed items <b>in the original order</b> in which they were provided by the <paramref name="removedItems"/> collection.</param>
    /// <returns><see langword="true"/> if any items were removed; otherwise, <see langword="false"/>.</returns>
    private bool UnregisterItems(IEnumerable<TItem> removedItems, out IList<int> indices)
    {
        indices = [];
        if (!_isInHybridMode)
        {
            return false;
        }

        int changeStartIndex = int.MaxValue;
        foreach (TItem removedItem in removedItems)
        {
            if (UnregisterItem(removedItem, isRebuildIndexRequired: false, out int itemIndex))
            {
                indices.Add(itemIndex);
                changeStartIndex = Math.Min(changeStartIndex, itemIndex);
            }
        }

        if (indices.Count == 0)
        {
            return false;
        }

        BuildIndex(changeStartIndex);

        return true;
    }

    private bool UnregisterItem(TItem item, bool isRebuildIndexRequired, out int itemIndex)
    {
        itemIndex = -1;
        if (!_isInHybridMode)
        {
            return false;
        }

        if (_indexTable.TryGetValue(item, out itemIndex))
        {
            _ = _indexTable.Remove(item);
            _ = _reverseIndexTable.Remove(itemIndex);
            _listProjection.RemoveAt(itemIndex);

            // If removed item was last not index rebuild is required
            if (isRebuildIndexRequired && itemIndex != _listProjection.Count)
            {
                BuildIndex(itemIndex);
            }

            return true;
        }

        return false;
    }

    private void BuildIndex(int changeIndex)
    {
        for (int index = changeIndex; index < _listProjection.Count; index++)
        {
            TItem indexedItem = _listProjection[index];
            _indexTable[indexedItem] = index;
            _reverseIndexTable[index] = indexedItem;
        }
    }

    private void InitializeListSurface()
    {
        if (_isInHybridMode)
        {
            return;
        }

        _listProjection.AddRange([.. Items]);
        BuildIndex(0);
        _isInHybridMode.SetValue(true);
    }

    #region ISerializable
    /// <summary>
    /// Populates a SerializationInfo object with the data needed to serialize the HashSet.
    /// </summary>
    /// <param name="info">The SerializationInfo object to populate with serialization data for the HashSet.</param>
    /// <param name="context">The StreamingContext that contains contextual information about the serialization operation.</param>
    public void GetObjectData(SerializationInfo info, StreamingContext context) => ((ISerializable)Items).GetObjectData(info, context);
    #endregion ISerializable

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection.</returns>
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Items).GetEnumerator();
    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator for the collection of items.</returns>
    IEnumerator<TItem> IEnumerable<TItem>.GetEnumerator() => ((IEnumerable<TItem>)Items).GetEnumerator();

    private HashSetDelta<TItem> GetDelta(HashSet<TItem> oldState, DeltaType deltaType = DeltaType.AddAndRemove)
    {
        HashSet<TItem> newState = Items;

        var removedItems = new List<TItem>();
        var addedItems = new List<TItem>();

        if ((deltaType & DeltaType.Remove) == DeltaType.Remove && oldState.Count > 0)
        {
            foreach (TItem item in oldState)
            {
                if (!newState.Contains(item))
                {
                    removedItems.Add(item);
                }
            }
        }

        if ((deltaType & DeltaType.Add) == DeltaType.Add && newState.Count > 0)
        {
            foreach (TItem item in newState)
            {
                if (!oldState.Contains(item))
                {
                    addedItems.Add(item);
                }
            }
        }

        bool hasChanges = removedItems.Count != 0 || addedItems.Count != 0;
        return new(removedItems.AsReadOnly(), addedItems.AsReadOnly(), hasChanges);
    }

    /// <summary> Check and assert for reentrant attempts to change this collection. </summary>
    /// <exception cref="InvalidOperationException"> raised when changing the collection
    /// while another collection change is still being notified to other listeners </exception>
    protected void CheckReentrancy()
    {
        if (_blockReentrancyCount > 0)
        {
            // we can allow changes if there's only one listener - the problem
            // only arises if reentrant changes make the original event args
            // invalid for later listeners.  This keeps existing code working
            // (e.g. Selector.SelectedItems).
            NotifyCollectionChangedEventHandler? handler = CollectionChanged;
            if (handler != null && !handler.HasSingleTarget)
            {
                throw new InvalidOperationException("Cannot modify the collection during a collection change notification.");
            }
        }
    }
    /// <summary>
    /// Disallow reentrant attempts to change this collection. E.g. an event handler
    /// of the CollectionChanged event is not allowed to make changes to this collection.
    /// </summary>
    /// <remarks>
    /// typical usage is to wrap e.g. a OnCollectionChanged call with a using() scope or using expression:
    /// <code>
    ///         using var monitor = BlockReentrancy()
    ///         CollectionChanged(this, new NotifyCollectionChangedEventArgs(action, item, index));
    /// </code>
    /// </remarks>
    protected IDisposable BlockReentrancy()
    {
        _blockReentrancyCount++;
        return new ReentrancyMonitor(this);
    }

    private HashSet<TItem> TakeSnapshot() => new(Items, Comparer);

    private void OnCollectionChanged(NotifyCollectionChangedAction action, TItem item, int index)
    {
        using IDisposable monitor = BlockReentrancy();
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, item, index));
    }

    private void OnCollectionChanged(NotifyCollectionChangedAction action, IList<TItem> changedItems, int startingIndex)
    {
        using IDisposable monitor = BlockReentrancy();
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, changedItems, startingIndex));
    }

    private void OnCollectionChangedReset()
    {
        using IDisposable monitor = BlockReentrancy();
        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    private void OnSetChanged(NotifyCollectionChangedAction action, TItem item) => SetChanged?.Invoke(this, new SetChangedEventArgs<TItem>(action, item));
    private void OnSetChanged(NotifyCollectionChangedAction action, IList<TItem> addedItems, IList<TItem> removedItems) => SetChanged?.Invoke(this, new SetChangedEventArgs<TItem>(action, addedItems, removedItems));

    private void OnCountChanged() => OnPropertyChanged(nameof(Count));
    private void OnIndexerChanged() => OnPropertyChanged("Item[]");

    private void OnCapacityChanged() => OnPropertyChanged(nameof(Capacity));
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    #region ICollection<T>
    /// <summary>
    /// Adds an item to the collection and raises change notifications if the collection is modified.
    /// </summary>
    /// <remarks>This method triggers collection change and count change notifications only if the item is
    /// successfully added. If the item already exists in the collection, no notifications are raised.
    /// <para/>This method raises the <see cref="CollectionChanged"/> event with <see cref="NotifyCollectionChangedAction.Add"/> action where the change index is always '-1'.
    /// <para/>This method raises the <see cref="PropertyChanged"/> event for the <see cref="Count"/> property.</remarks>
    /// <param name="item">The item to add to the collection. Cannot be null if the collection does not accept null values.</param>
    void ICollection<TItem>.Add(TItem item) => Add(item);
    int ICollection<TItem>.Count => Count;
    bool ICollection<TItem>.IsReadOnly { get; } // false; 
    #endregion ICollection<T>

    #region ICollection
    int ICollection.Count => Count;
    bool ICollection.IsSynchronized { get; } // false;
    object ICollection.SyncRoot { get; } = new object();
    #endregion ICollection

    #region IList<T>
    [SuppressMessage("Design", "CA1033:Interface methods should be callable by child types", Justification = "Not overridable behavior. IList<T> implementation only exist to add performance boost for WPF data binding support.")]
    int IList<TItem>.IndexOf(TItem item)
    {
        InitializeListSurface();
        return _indexTable.TryGetValue(item, out int index)
            ? index
            : -1;
    }

    [SuppressMessage("Design", "CA1033:Interface methods should be callable by child types", Justification = "Not overridable behavior. IList<T> implementation only exist to add performance boost for WPF data binding support.")]
    void IList<TItem>.Insert(int index, TItem item)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfIndexOutOfRange(index, _listProjection);

        InitializeListSurface();

        if (AddItem(item))
        {
            RegisterInsertedItem(item, index, isRebuildIndexRequired: true);

            OnCountChanged();
            OnIndexerChanged();
            OnCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
        }
    }

    [SuppressMessage("Design", "CA1033:Interface methods should be callable by child types", Justification = "Not overridable behavior. IList<T> implementation only exist to add performance boost for WPF data binding support.")]
    void IList<TItem>.RemoveAt(int index)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfIndexOutOfRange(index, _listProjection);

        InitializeListSurface();

        if (_reverseIndexTable.TryGetValue(index, out TItem? item))
        {
            if (RemoveInternal(item, isRebuildIndexRequired: true, out _))
            {
                OnCountChanged();
                OnIndexerChanged();
                OnCollectionChanged(NotifyCollectionChangedAction.Remove, item, index);
            }
        }
    }

    TItem IList<TItem>.this[int index]
    {
        [SuppressMessage("Design", "CA1033:Interface methods should be callable by child types", Justification = "Not overridable behavior. IList<T> implementation only exist to add performance boost for WPF data binding aupport.")]
        get
        {
            ArgumentOutOfRangeExceptionAdvanced.ThrowIfIndexOutOfRange(index, _listProjection);

            InitializeListSurface();

            return _listProjection[index];
        }

        [SuppressMessage("Design", "CA1033:Interface methods should be callable by child types", Justification = "Not overridable behavior. IList<T> implementation only exist to add performance boost for WPF data binding aupport.")]
        set
        {
            ArgumentOutOfRangeExceptionAdvanced.ThrowIfIndexOutOfRange(index, _listProjection);

            InitializeListSurface();

            if (_reverseIndexTable.TryGetValue(index, out TItem? existingItem))
            {
                UpdateItemAt(value, existingItem, index);

                OnIndexerChanged();
                OnCollectionChanged(NotifyCollectionChangedAction.Replace, value, index);
            }
        }
    }
    #endregion IList<T>

    #region IList
    bool IList.IsFixedSize { get; } // false
    bool IList.IsReadOnly { get; } // false
    int IList.Add(object? value)
    {
        if (value is not TItem item)
        {
            throw new InvalidCastException($"Unable to convert '{value?.GetType().FullName ?? "NULL"}' to '{typeof(TItem).FullName}'.");
        }

        return AddInternal(item, out int itemIndex)
            ? itemIndex
            : -1;
    }

    void IList.Clear() => Clear();
    bool IList.Contains(object? value)
    {
        if (value is not TItem item)
        {
            throw new InvalidCastException($"Unable to convert '{value?.GetType().FullName ?? "NULL"}' to '{typeof(TItem).FullName}'.");
        }

        return Contains(item);
    }

    [SuppressMessage("Design", "CA1033:Interface methods should be callable by child types", Justification = "Not overridable behavior. IList<T> implementation only exist to add performance boost for WPF data binding support.")]
    int IList.IndexOf(object? value)
    {
        if (value is not TItem item)
        {
            throw new InvalidCastException($"Unable to convert '{value?.GetType().FullName ?? "NULL"}' to '{typeof(TItem).FullName}'.");
        }

        InitializeListSurface();
        return _indexTable.TryGetValue(item, out int index)
            ? index
            : -1;
    }

    [SuppressMessage("Design", "CA1033:Interface methods should be callable by child types", Justification = "Not overridable behavior. IList<T> implementation only exist to add performance boost for WPF data binding support.")]
    void IList.Insert(int index, object? value)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfIndexOutOfRange(index, _listProjection);

        if (value is not TItem item)
        {
            throw new InvalidCastException($"Unable to convert '{value?.GetType().FullName ?? "NULL"}' to '{typeof(TItem).FullName}'.");
        }

        InitializeListSurface();

        ((IList<TItem>)this).Insert(index, item);
    }

    void IList.Remove(object? value)
    {
        if (value is not TItem item)
        {
            throw new InvalidCastException($"Unable to convert '{value?.GetType().FullName ?? "NULL"}' to '{typeof(TItem).FullName}'.");
        }

        InitializeListSurface();

        _ = Remove(item);
    }

    [SuppressMessage("Design", "CA1033:Interface methods should be callable by child types", Justification = "Not overridable behavior. IList<T> implementation only exist to add performance boost for WPF data binding support.")]
    void IList.RemoveAt(int index)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfIndexOutOfRange(index, _listProjection);

        InitializeListSurface();

        ((IList<TItem>)this).RemoveAt(index);
    }

    void ICollection.CopyTo(Array array, int index)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(array);

        if (array.Rank is not 1)
        {
            throw new ArgumentException("Array must be one-dimensional.", nameof(array));
        }

        if (array.GetLowerBound(0) is not 0)
        {
            throw new ArgumentException("Array must have zero lower bound.", nameof(array));
        }

        if (index < 0 || index > array.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (array.Length - index < Items.Count)
        {
            throw new ArgumentException("The destination array has insufficient space.", nameof(array));
        }

        if (array is TItem[] itemArray)
        {
            CopyTo(itemArray, index);
            return;
        }

        // Fallback: copy through Array.SetValue / element checks
        foreach (TItem item in Items)
        {
            array.SetValue(item, index++);
        }
    }

    object? IList.this[int index]
    {
        [SuppressMessage("Design", "CA1033:Interface methods should be callable by child types", Justification = "Not overridable behavior. IList<T> implementation only exist to add performance boost for WPF data binding support.")]
        get
        {
            InitializeListSurface();
            return ((IList<TItem>)this)[index];
        }

        [SuppressMessage("Design", "CA1033:Interface methods should be callable by child types", Justification = "Not overridable behavior. IList<T> implementation only exist to add performance boost for WPF data binding support.")]
        set
        {
            if (value is not TItem item)
            {
                throw new InvalidCastException($"Unable to convert '{value?.GetType().FullName ?? "NULL"}' to '{typeof(TItem).FullName}'.");
            }

            InitializeListSurface();

            ((IList<TItem>)this)[index] = item;
        }
    }
    #endregion IList

    #region Enumerator
    public struct Enumerator : IEnumerator<TItem>
    {
        private List<TItem>.Enumerator _enumerator;
        internal Enumerator(List<TItem> hashSet)
        {
            ArgumentNullExceptionAdvanced.ThrowIfNull(hashSet);
            _enumerator = hashSet.GetEnumerator();
        }

        public TItem Current => _enumerator.Current;
        object IEnumerator.Current => Current!;
        public void Dispose() => _enumerator.Dispose();
        public bool MoveNext() => _enumerator.MoveNext();
        public void Reset() => throw new NotSupportedException();
    }
    #endregion Enumerator

    private sealed class ReentrancyMonitor : IDisposableAdvanced
    {
        private ObservableHashSet<TItem> _owner;

        public ReentrancyMonitor(ObservableHashSet<TItem> owner)
        {
            ArgumentNullExceptionAdvanced.ThrowIfNull(owner);
            _owner = owner;
        }

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            _owner._blockReentrancyCount--;
            _owner = null!;
            IsDisposed = true;
        }
    }

    internal sealed class ObservableHashSetEqualityComparer<TItem> : IEqualityComparer<ObservableHashSet<TItem>?>, IEqualityComparer<HashSet<TItem>?>
    {
        public static ObservableHashSetEqualityComparer<TItem> Instance { get; } = new();

        private ObservableHashSetEqualityComparer() { }

        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "NULL is allowed and handled as primary condition for equality. Equality check ends (fast exit) if either of the arguments is NULL without dereferencing any instance members.")]
        public bool Equals(ObservableHashSet<TItem>? x, ObservableHashSet<TItem>? y) => SetEqualityComparerHelpers.IsSetEqual(x, y, () => x!.Comparer, () => y!.Comparer);

        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "NULL is allowed and handled as primary condition for equality. Equality check ends (fast exit) if either of the arguments is NULL without dereferencing any instance members.")]
        public bool Equals(HashSet<TItem>? x, HashSet<TItem>? y) => SetEqualityComparerHelpers.IsSetEqual(x, y, () => x!.Comparer, () => y!.Comparer);

        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "NULL is allowed and handled as primary condition for equality. Equality check ends (fast exit) if either of the arguments is NULL without dereferencing any instance members.")]
        public static bool Equals(ObservableHashSet<TItem>? x, HashSet<TItem>? y) => SetEqualityComparerHelpers.IsSetEqual(x, y, () => x!.Comparer, () => y!.Comparer);

        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "NULL is allowed and handled as primary condition for equality. Equality check ends (fast exit) if either of the arguments is NULL without dereferencing any instance members.")]
        public static bool Equals(HashSet<TItem>? x, ObservableHashSet<TItem>? y) => SetEqualityComparerHelpers.IsSetEqual(x, y, () => x!.Comparer, () => y!.Comparer);

        public int GetHashCode([DisallowNull] ObservableHashSet<TItem> obj)
        {
            ArgumentNullExceptionAdvanced.ThrowIfNull(obj);

            return SetEqualityComparerHelpers.ComputeHashCode(obj, obj.Comparer);
        }

        public int GetHashCode([DisallowNull] HashSet<TItem>? obj)
        {
            ArgumentNullExceptionAdvanced.ThrowIfNull(obj);

            return SetEqualityComparerHelpers.ComputeHashCode(obj, obj.Comparer);
        }
    }

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

    internal readonly struct IndexedHashSetDelta<TItem>
    {
        public IndexedHashSetDelta(ReadOnlyDictionary<int, TItem> removedItems, ReadOnlyDictionary<int, TItem> addedItems, bool hasChanges)
        {
            RemovedItems = removedItems;
            AddedItems = addedItems;
            HasChanges = hasChanges;
        }

        public ReadOnlyDictionary<int, TItem> RemovedItems { get; }
        public ReadOnlyDictionary<int, TItem> AddedItems { get; }
        public bool HasChanges { get; }
    }

    [Flags]
    internal enum DeltaType
    {
        None = 0,
        Add = 1,
        Remove = 2,
        AddAndRemove = Add | Remove
    }
}

public sealed class ObservableFileSystemPathHashSet : ObservableHashSet<string>
{
    public ObservableFileSystemPathHashSet() : base(FileSystemPathEqualityComparer.Instance) { }
    public ObservableFileSystemPathHashSet(IEnumerable<string> collection) : base(collection, FileSystemPathEqualityComparer.Instance) { }

    public ObservableFileSystemPathHashSet(int capacity) : base(capacity, FileSystemPathEqualityComparer.Instance)
    {
    }

    public ObservableFileSystemPathHashSet(IEqualityComparer<string>? comparer) : base(comparer ?? FileSystemPathEqualityComparer.Instance)
    {
    }

    public ObservableFileSystemPathHashSet(IEnumerable<string> collection, IEqualityComparer<string>? comparer) : base(collection, comparer ?? FileSystemPathEqualityComparer.Instance)
    {
    }

    public ObservableFileSystemPathHashSet(int capacity, IEqualityComparer<string>? comparer) : base(capacity, comparer ?? FileSystemPathEqualityComparer.Instance)
    {
    }
    /// <summary>
    /// The equality comparer used to determine equality of items in the set. This comparer is used for all operations that involve comparing items, such as adding, removing, and checking for the presence of items in the set.
    /// </summary>
    /// <removedItem>The equality <see cref="IEqualityComparer"/>&lt;<see langword="string"/>&gt; used by the set. The default is <see cref="StringComparer.OrdinalIgnoreCase"/>.</removedItem>
    public new IEqualityComparer<string> Comparer => Items.Comparer;

    /// <summary>
    /// Adds the specified file system item to the collection.
    /// </summary>
    /// <param name="item">The file system item to add. Cannot be <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the item was successfully added; otherwise, <see langword="false"/>.</returns>
    /// <remarks>This method adds the full path as returned by the <see cref="FileSystemInfo.FullName"/> property of the specified <paramref name="item"/> to the set. If the item is already present, the set remains unchanged and the method returns <see langword="false"/>; otherwise, the item is added and the method returns <see langword="true"/>.
    public bool Add(FileSystemInfo item)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(item);

        return base.Add(item.FullName);
    }

    /// <summary>
    /// Removes the specified file system item from the collection.
    /// </summary>
    /// <param name="item">The file system item to remove. Cannot be <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the item was successfully removed; otherwise, <see langword="false"/>.</returns>
    /// <remarks>This method removes the full path as returned by the <see cref="FileSystemInfo.FullName"/> property of the specified <paramref name="item"/> from the set. If the item is not present, the set remains unchanged and the method returns <see langword="false"/>; otherwise, the item is removed and the method returns <see langword="true"/>.</remarks>
    public bool Remove(FileSystemInfo item)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(item);

        return base.Remove(item.FullName);
    }

    /// <summary>
    /// Determines whether the collection contains the specified file system item.
    /// </summary>
    /// <param name="item">The file system item to locate in the collection. Cannot be <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the item exists in the collection; otherwise, <see langword="false"/>.</returns>
    /// <remarks>This method checks for the presence of the full path as returned by the <see cref="FileSystemInfo.FullName"/> property of the specified <paramref name="item"/> in the set. If the item is found, the method returns <see langword="true"/>; otherwise, it returns <see langword="false"/>.</remarks>
    public bool Contains(FileSystemInfo item)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(item);

        return base.Contains(item.FullName);
    }

    /// <summary>
    /// Removes all items from the collection that match the specified predicate.
    /// </summary>
    /// <remarks>Each item is represented as a <see cref="FileInfo"/> object. The method removes only those items for which the predicate
    /// returns <see langword="true"/>.</remarks>
    /// <param name="match">A delegate that defines the conditions of the items to remove. Cannot be <see langword="null"/>.</param>
    /// <returns>The number of items removed from the collection.</returns>
    public int RemoveWhere(Predicate<FileInfo> match)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(match);

        string[] snapshot = Items.ToArray();
        int removedCount = 0;
        foreach (string item in snapshot)
        {
            var fileInfo = new FileInfo(item);
            if (match(fileInfo) && Remove(item))
            {
                removedCount++;
            }
        }

        return removedCount;
    }

    /// <summary>
    /// Removes all items from the collection that match the specified predicate.
    /// </summary>
    /// <remarks>Each item is represented as a <see cref="DirectoryInfo"/> object. The method removes only those items for which the predicate
    /// returns <see langword="true"/>.</remarks>
    /// <param name="match">A delegate that defines the conditions of the items to remove. Cannot be <see langword="null"/>.</param>
    /// <returns>The number of items removed from the collection.</returns>
    public int RemoveWhere(Predicate<DirectoryInfo> match)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(match);

        string[] snapshot = Items.ToArray();
        int removedCount = 0;
        foreach (string item in snapshot)
        {
            var directoryInfo = new DirectoryInfo(item);
            if (match(directoryInfo) && Remove(item))
            {
                removedCount++;
            }
        }

        return removedCount;
    }

    public bool TryGetValue(FileInfo item, [MaybeNullWhen(false)] out FileInfo value)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(item);

        value = default;
        if (Items.TryGetValue(item.FullName, out string? stringValue))
        {
            value = new FileInfo(stringValue);

            return true;
        }

        return false;
    }

    public bool TryGetValue(DirectoryInfo item, [MaybeNullWhen(false)] out DirectoryInfo value)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(item);

        value = default;
        if (Items.TryGetValue(item.FullName, out string? stringValue))
        {
            value = new DirectoryInfo(stringValue);

            return true;
        }

        return false;
    }

    /// <summary>
    /// Creates an equality comparer that can be used to compare two hash sets for set equality.
    /// </summary>
    /// <remarks>The returned comparer considers two hash sets equal if they have the same elements, even if
    /// the order differs. This is useful for scenarios where set semantics are required, such as using hash sets as
    /// keys in dictionaries.</remarks>
    /// <returns>An equality comparer that determines whether two hash sets contain the same elements, regardless of order.</returns>
    public static new IEqualityComparer<ObservableFileSystemPathHashSet> CreateSetComparer() => ObservableFileSystemPathHashSetEqualityComparer.Instance;

    /// <summary>
    /// Removes all elements in the specified collection from the current set.
    /// </summary>
    /// <remarks>If the specified collection contains elements that are not present in the set, those elements
    /// are ignored. The operation modifies the current set and does not return a removedItem.
    /// <para/>This method raises the <see cref="CollectionChanged"/> event with <see cref="NotifyCollectionChangedAction.Add"/> or <see cref="NotifyCollectionChangedAction.Remove"/> action including the set of removed and added items where the change index is always '-1'.
    /// <para/>This method raises the <see cref="PropertyChanged"/> event for the <see cref="Count"/> property.</remarks>
    /// <param name="other">The collection of elements to remove from the set. Cannot be <see langword="null"/>.</param>
    public void ExceptWith(IEnumerable<FileSystemInfo> other)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(other);

        IEnumerable<string> unwrappedOther = other.Select(item => item.FullName);
        ExceptWith(unwrappedOther);
    }
    /// <summary>
    /// Modifies the current set to contain only elements that are also in the specified collection.
    /// </summary>
    /// <remarks>This method removes any elements from the current set that are not present in the specified
    /// collection. The operation does not preserve the order of elements.
    /// <para/>This method raises the <see cref="CollectionChanged"/> event with <see cref="NotifyCollectionChangedAction.Add"/> or <see cref="NotifyCollectionChangedAction.Remove"/> action including the set of removed and added items where the change index is always '-1'.
    /// <para/>This method raises the <see cref="PropertyChanged"/> event for the <see cref="Count"/> property.</remarks>
    /// <param name="other">The collection to compare to the current set. Cannot be null.</param>
    public void IntersectWith(IEnumerable<FileSystemInfo> other)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(other);

        IEnumerable<string> unwrappedOther = other.Select(item => item.FullName);
        IntersectWith(unwrappedOther);
    }

    /// <summary>
    /// Determines whether the current set is a proper subset of the specified collection.
    /// </summary>
    /// <remarks>A set is a proper subset of another collection if all elements of the set are contained in
    /// the collection and the collection contains at least one element not in the set. If the specified collection is
    /// <see langword="null"/>, an <see cref="ArgumentNullException"/> is thrown.</remarks>
    /// <param name="other">The collection to compare to the current set. Cannot be <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the current set is a proper subset of the specified collection; otherwise, <see langword="false"/>.</returns>
    public bool IsProperSubsetOf(IEnumerable<FileSystemInfo> other)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(other);

        IEnumerable<string> unwrappedOther = other.Select(item => item.FullName);
        return Items.IsProperSubsetOf(unwrappedOther);
    }

    /// <summary>
    /// Determines whether the current set is a proper superset of the specified collection.
    /// </summary>
    /// <remarks>A set is a proper superset of another collection if all elements of the other collection are contained in
    /// the set and the set contains at least one element not in the other collection. If the specified collection is
    /// <see langword="null"/>, an <see cref="ArgumentNullException"/> is thrown.</remarks>
    /// <param name="other">The collection to compare to the current set. Cannot be <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the current set is a proper superset of the specified collection; otherwise, <see langword="false"/>.</returns>
    public bool IsProperSupersetOf(IEnumerable<FileSystemInfo> other)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(other);

        IEnumerable<string> unwrappedOther = other.Select(item => item.FullName);
        return Items.IsProperSupersetOf(unwrappedOther);
    }
    /// <summary>
    /// Determines whether the current set is a subset of the specified collection.
    /// </summary>
    /// <param name="other">The collection to compare to the current set. Cannot be <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the current set is a subset of the specified collection; otherwise, <see langword="false"/>.</returns>
    public bool IsSubsetOf(IEnumerable<FileSystemInfo> other)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(other);

        IEnumerable<string> unwrappedOther = other.Select(item => item.FullName);
        return Items.IsSubsetOf(unwrappedOther);
    }
    /// <summary>
    /// Determines whether the current set is a superset of the specified collection.
    /// </summary>
    /// <param name="other">The collection to compare to the current set. Cannot be <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the current set is a superset of the specified collection; otherwise, <see langword="false"/>.</returns>
    public bool IsSupersetOf(IEnumerable<FileSystemInfo> other)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(other);

        IEnumerable<string> unwrappedOther = other.Select(item => item.FullName);
        return Items.IsSupersetOf(unwrappedOther);
    }
    /// <summary>
    /// Determines whether the current set and the specified collection share any common elements.
    /// </summary>
    /// <param name="other">The collection to compare to the current set. Cannot be <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the current set and the specified collection share at least one common element; otherwise, <see langword="false"/>.</returns>
    public bool Overlaps(IEnumerable<FileSystemInfo> other)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(other);

        IEnumerable<string> unwrappedOther = other.Select(item => item.FullName);
        return Items.Overlaps(unwrappedOther);
    }
    /// <summary>
    /// Determines whether the current set contains exactly the same elements as the specified collection.
    /// </summary>
    /// <remarks>Set equality is determined by comparing the unique elements in both collections, regardless
    /// of order. The comparison ignores duplicate elements in the input collection.</remarks>
    /// <param name="other">The collection to compare to the current set. The elements are compared for equality, and duplicate elements are
    /// ignored.</param>
    /// <returns><see langword="true"/> if the current set and the specified collection contain the same elements; otherwise, <see langword="false"/>.</returns>
    public bool SetEquals(IEnumerable<FileSystemInfo> other)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(other);

        IEnumerable<string> unwrappedOther = other.Select(item => item.FullName);
        return Items.SetEquals(unwrappedOther);
    }
    /// <summary>
    /// Modifies the current set so that it contains only elements that are present in either the set or the specified
    /// collection, but not both.
    /// </summary>
    /// <remarks>The symmetric difference operation removes elements that appear in both the current set and
    /// the specified collection, and adds elements that appear in either set but not both. If the specified collection
    /// contains duplicate elements, only unique elements are considered. This method does not return a removedItem; it
    /// modifies the current set in place.
    /// <para/>This method raises the <see cref="CollectionChanged"/> event with <see cref="NotifyCollectionChangedAction.Add"/> or <see cref="NotifyCollectionChangedAction.Remove"/> action including the set of removed and added items where the change index is always '-1'.
    /// <para/>This method raises the <see cref="PropertyChanged"/> event for the <see cref="Count"/> property.</remarks>
    /// <param name="other">The collection whose symmetric difference with the current set is to be computed. Cannot be <see langword="null"/>.</param>
    public void SymmetricExceptWith(IEnumerable<FileSystemInfo> other)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(other);

        IEnumerable<string> unwrappedOther = other.Select(item => item.FullName);
        SymmetricExceptWith(unwrappedOther);
    }
    /// <summary>
    /// Adds all elements from the specified collection to the current set.
    /// </summary>
    /// <remarks>Duplicate elements in the specified collection are ignored. The set will contain each unique
    /// element from both the original set and the specified collection after the operation completes.
    /// <para/>This method raises the <see cref="CollectionChanged"/> event with <see cref="NotifyCollectionChangedAction.Add"/> or <see cref="NotifyCollectionChangedAction.Remove"/> action including the set of removed and added items where the change index is always '-1'.
    /// <para/>This method raises the <see cref="PropertyChanged"/> event for the <see cref="Count"/> property.</remarks>
    /// <param name="other">The collection whose elements are to be added to the set. Cannot be <see langword="null"/>.</param>
    public void UnionWith(IEnumerable<FileSystemInfo> other)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(other);

        IEnumerable<string> unwrappedOther = other.Select(item => item.FullName);
        UnionWith(unwrappedOther);
    }

    internal sealed class ObservableFileSystemPathHashSetEqualityComparer : IEqualityComparer<ObservableFileSystemPathHashSet?>, IEqualityComparer<HashSet<string>?>, IEqualityComparer<ObservableHashSet<string>?>, IEqualityComparer<HashSet<FileSystemInfo>?>, IEqualityComparer<ObservableHashSet<FileSystemInfo>?>
    {
        public static ObservableFileSystemPathHashSetEqualityComparer Instance { get; } = new ObservableFileSystemPathHashSetEqualityComparer();

        private ObservableFileSystemPathHashSetEqualityComparer() { }

        /// <summary>
        /// Determines whether two specified <see cref="ObservableFileSystemPathHashSet"> instances are equal by comparing their elements
        /// and comparers.
        /// </summary>
        /// <remarks>Equality is determined using the collection's comparer. Collection equality is defined as follows (ordered by hierarchy):
        /// <list type="number">
        /// <item>If both parameters are <see langword="null"/>, they are considered equal.</item>
        /// <item>If both parameters reference the same instance, they are considered equal.</item>
        /// <item>If only one is <see langword="null"/>, they are not equal.</item>
        /// <item>If both sets use the same comparer instance</item>
        /// <item>AND if both sets have the same number of elements</item>
        /// <item>AND if all elements are equal according to the set's comparer</item>
        /// </list>
        /// both collections are considered equal.
        /// </remarks>
        /// <param name="x">The first <see cref="ObservableFileSystemPathHashSet"> to compare, or null.</param>
        /// <param name="y">The second <see cref="ObservableFileSystemPathHashSet"> to compare, or null.</param>
        /// <returns><see langword="true"> if both sets satisfy the constraints for equality; otherwise, <see langword="false">.</returns>
        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "NULL is allowed and handled as primary condition for equality. Equality check ends (fast exit) if either of the arguments is NULL without dereferencing any instance members.")]
        public bool Equals(ObservableFileSystemPathHashSet? x, ObservableFileSystemPathHashSet? y) => SetEqualityComparerHelpers.IsSetEqual(x, y, () => x!.Comparer, () => y!.Comparer);

        /// <summary>
        /// Determines whether two <see cref="ObservableHashSet"/>&lt;<see langword="string"/>&gt; instances are equal by comparing their contents.
        /// </summary>
        /// <remarks>Equality is determined using the collection's comparer. Collection equality is defined as follows (ordered by hierarchy):
        /// <list type="number">
        /// <item>If both parameters are <see langword="null"/>, they are considered equal.</item>
        /// <item>If both parameters reference the same instance, they are considered equal.</item>
        /// <item>If only one is <see langword="null"/>, they are not equal.</item>
        /// <item>If both sets use the same comparer instance</item>
        /// <item>AND if both sets have the same number of elements</item>
        /// <item>AND if all elements are equal according to the set's comparer</item>
        /// </list>
        /// both collections are considered equal.
        /// </remarks>
        /// <param name="x">The first <see cref="ObservableHashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
        /// <param name="y">The second <see cref="ObservableHashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if both sets satisfy the constraints for equality; otherwise, <see langword="false"/>.</returns>
        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "NULL is allowed and handled as primary condition for equality. Equality check ends (fast exit) if either of the arguments is NULL without dereferencing any instance members.")]
        public bool Equals(ObservableHashSet<string>? x, ObservableHashSet<string>? y) => SetEqualityComparerHelpers.IsSetEqual(x, y, () => x!.Comparer, () => y!.Comparer);

        /// <summary>
        /// Determines whether two <see cref="HashSet"/>&lt;<see langword="string"/>&gt; instances are equal by comparing their contents.
        /// </summary>
        /// <remarks>Equality is determined using the collection's comparer. Collection equality is defined as follows (ordered by hierarchy):
        /// <list type="number">
        /// <item>If both parameters are <see langword="null"/>, they are considered equal.</item>
        /// <item>If both parameters reference the same instance, they are considered equal.</item>
        /// <item>If only one is <see langword="null"/>, they are not equal.</item>
        /// <item>If both sets use the same comparer instance</item>
        /// <item>AND if both sets have the same number of elements</item>
        /// <item>AND if all elements are equal according to the set's comparer</item>
        /// </list>
        /// both collections are considered equal.
        /// </remarks>
        /// <param name="x">The first <see cref="HashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
        /// <param name="y">The second <see cref="HashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if both sets satisfy the constraints for equality; otherwise, <see langword="false"/>.</returns>
        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "NULL is allowed and handled as primary condition for equality. Equality check ends (fast exit) if either of the arguments is NULL without dereferencing any instance members.")]
        public bool Equals(HashSet<string>? x, HashSet<string>? y) => SetEqualityComparerHelpers.IsSetEqual(x, y, () => x!.Comparer, () => y!.Comparer);

        /// <summary>
        /// Determines whether two <see cref="ObservableHashSet"/>&lt;<see langword="string"/>&gt; instances are equal by comparing their contents.
        /// </summary>
        /// <remarks>Equality is determined using the collection's comparer. Collection equality is defined as follows (ordered by hierarchy):
        /// <list type="number">
        /// <item>If both parameters are <see langword="null"/>, they are considered equal.</item>
        /// <item>If both parameters reference the same instance, they are considered equal.</item>
        /// <item>If only one is <see langword="null"/>, they are not equal.</item>
        /// <item>If both sets use the same comparer instance</item>
        /// <item>AND if both sets have the same number of elements</item>
        /// <item>AND if all elements are equal according to the set's comparer</item>
        /// </list>
        /// both collections are considered equal.
        /// </remarks>
        /// <param name="x">The first <see cref="ObservableHashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
        /// <param name="y">The second <see cref="ObservableHashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if both sets satisfy the constraints for equality; otherwise, <see langword="false"/>.</returns>
        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "NULL is allowed and handled as primary condition for equality. Equality check ends (fast exit) if either of the arguments is NULL without dereferencing any instance members.")]
        public bool Equals(ObservableHashSet<FileSystemInfo>? x, ObservableHashSet<FileSystemInfo>? y) => SetEqualityComparerHelpers.IsSetEqual(x, y, () => x!.Comparer, () => y!.Comparer);

        /// <summary>
        /// Determines whether two <see cref="HashSet"/>&lt;<see langword="string"/>&gt; instances are equal by comparing their contents.
        /// </summary>
        /// <remarks>Equality is determined using the collection's comparer. Collection equality is defined as follows (ordered by hierarchy):
        /// <list type="number">
        /// <item>If both parameters are <see langword="null"/>, they are considered equal.</item>
        /// <item>If both parameters reference the same instance, they are considered equal.</item>
        /// <item>If only one is <see langword="null"/>, they are not equal.</item>
        /// <item>If both sets use the same comparer instance</item>
        /// <item>AND if both sets have the same number of elements</item>
        /// <item>AND if all elements are equal according to the set's comparer</item>
        /// </list>
        /// both collections are considered equal.
        /// </remarks>
        /// <param name="x">The first <see cref="HashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
        /// <param name="y">The second <see cref="HashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if both sets satisfy the constraints for equality; otherwise, <see langword="false"/>.</returns>
        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "NULL is allowed and handled as primary condition for equality. Equality check ends (fast exit) if either of the arguments is NULL without dereferencing any instance members.")]
        public bool Equals(HashSet<FileSystemInfo>? x, HashSet<FileSystemInfo>? y) => SetEqualityComparerHelpers.IsSetEqual(x, y, () => x!.Comparer, () => y!.Comparer);

        /// <summary>
        /// Determines whether two <see cref="HashSet"/>&lt;<see langword="string"/>&gt; instances are equal by comparing their contents.
        /// </summary>
        /// <remarks>Equality is determined using the collection's comparer. Collection equality is defined as follows (ordered by hierarchy):
        /// <list type="number">
        /// <item>If both parameters are <see langword="null"/>, they are considered equal.</item>
        /// <item>If both parameters reference the same instance, they are considered equal.</item>
        /// <item>If only one is <see langword="null"/>, they are not equal.</item>
        /// <item>If both sets use the same comparer instance</item>
        /// <item>AND if both sets have the same number of elements</item>
        /// <item>AND if all elements are equal according to the set's comparer</item>
        /// </list>
        /// both collections are considered equal.
        /// </remarks>
        /// <param name="x">The first <see cref="HashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
        /// <param name="y">The second <see cref="HashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if both sets satisfy the constraints for equality; otherwise, <see langword="false"/>.</returns>
        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "NULL is allowed and handled as primary condition for equality. Equality check ends (fast exit) if either of the arguments is NULL without dereferencing any instance members.")]
        public static bool Equals(ISet<FileSystemInfo>? x, IEqualityComparer<FileSystemInfo> setXComparer, ISet<FileSystemInfo>? y, IEqualityComparer<FileSystemInfo> setYComparer) => SetEqualityComparerHelpers.IsSetEqual(x, y, () => setXComparer, () => setYComparer);

        /// <summary>
        /// Determines whether two <see cref="HashSet"/>&lt;<see langword="string"/>&gt; instances are equal by comparing their contents.
        /// </summary>
        /// <remarks>Equality is determined using the collection's comparer. Collection equality is defined as follows (ordered by hierarchy):
        /// <list type="number">
        /// <item>If both parameters are <see langword="null"/>, they are considered equal.</item>
        /// <item>If both parameters reference the same instance, they are considered equal.</item>
        /// <item>If only one is <see langword="null"/>, they are not equal.</item>
        /// <item>If both sets use the same comparer instance</item>
        /// <item>AND if both sets have the same number of elements</item>
        /// <item>AND if all elements are equal according to the set's comparer</item>
        /// </list>
        /// both collections are considered equal.
        /// </remarks>
        /// <param name="x">The first <see cref="HashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
        /// <param name="y">The second <see cref="HashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if both sets satisfy the constraints for equality; otherwise, <see langword="false"/>.</returns>
        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "NULL is allowed and handled as primary condition for equality. Equality check ends (fast exit) if either of the arguments is NULL without dereferencing any instance members.")]
        public static bool Equals(ISet<FileSystemInfo>? x, IEqualityComparer<FileSystemInfo> setXComparer, ISet<string>? y, IEqualityComparer<string> setYComparer) => SetEqualityComparerHelpers.IsSetEqual(y, x, () => setYComparer, () => setXComparer);

        /// <summary>
        /// Determines whether two <see cref="HashSet"/>&lt;<see langword="string"/>&gt; instances are equal by comparing their contents.
        /// </summary>
        /// <remarks>Equality is determined using the collection's comparer. Collection equality is defined as follows (ordered by hierarchy):
        /// <list type="number">
        /// <item>If both parameters are <see langword="null"/>, they are considered equal.</item>
        /// <item>If both parameters reference the same instance, they are considered equal.</item>
        /// <item>If only one is <see langword="null"/>, they are not equal.</item>
        /// <item>If both sets use the same comparer instance</item>
        /// <item>AND if both sets have the same number of elements</item>
        /// <item>AND if all elements are equal according to the set's comparer</item>
        /// </list>
        /// both collections are considered equal.
        /// </remarks>
        /// <param name="x">The first <see cref="HashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
        /// <param name="y">The second <see cref="HashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if both sets satisfy the constraints for equality; otherwise, <see langword="false"/>.</returns>
        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "NULL is allowed and handled as primary condition for equality. Equality check ends (fast exit) if either of the arguments is NULL without dereferencing any instance members.")]
        public static bool Equals(ISet<string>? x, IEqualityComparer<string> setXComparer, ISet<string>? y, IEqualityComparer<string> setYComparer) => SetEqualityComparerHelpers.IsSetEqual(x, y, () => setXComparer, () => setYComparer);

        /// <summary>
        /// Determines whether two <see cref="HashSet"/>&lt;<see langword="string"/>&gt; instances are equal by comparing their contents.
        /// </summary>
        /// <remarks>Equality is determined using the collection's comparer. Collection equality is defined as follows (ordered by hierarchy):
        /// <list type="number">
        /// <item>If both parameters are <see langword="null"/>, they are considered equal.</item>
        /// <item>If both parameters reference the same instance, they are considered equal.</item>
        /// <item>If only one is <see langword="null"/>, they are not equal.</item>
        /// <item>If both sets use the same comparer instance</item>
        /// <item>AND if both sets have the same number of elements</item>
        /// <item>AND if all elements are equal according to the set's comparer</item>
        /// </list>
        /// both collections are considered equal.
        /// </remarks>
        /// <param name="x">The first <see cref="HashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
        /// <param name="y">The second <see cref="HashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if both sets satisfy the constraints for equality; otherwise, <see langword="false"/>.</returns>
        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "NULL is allowed and handled as primary condition for equality. Equality check ends (fast exit) if either of the arguments is NULL without dereferencing any instance members.")]
        public static bool Equals(ISet<string>? x, IEqualityComparer<string> setXComparer, ISet<FileSystemInfo>? y, IEqualityComparer<FileSystemInfo> setYComparer) => SetEqualityComparerHelpers.IsSetEqual(x, y, () => setXComparer, () => setYComparer);

        /// <summary>
        /// Determines whether two <see cref="HashSet"/>&lt;<see langword="string"/>&gt; instances are equal by comparing their contents.
        /// </summary>
        /// <remarks>Equality is determined using the collection's comparer. Collection equality is defined as follows (ordered by hierarchy):
        /// <list type="number">
        /// <item>If both parameters are <see langword="null"/>, they are considered equal.</item>
        /// <item>If both parameters reference the same instance, they are considered equal.</item>
        /// <item>If only one is <see langword="null"/>, they are not equal.</item>
        /// <item>If both sets use the same comparer instance</item>
        /// <item>AND if both sets have the same number of elements</item>
        /// <item>AND if all elements are equal according to the set's comparer</item>
        /// </list>
        /// both collections are considered equal.
        /// </remarks>
        /// <param name="x">The first <see cref="HashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
        /// <param name="y">The second <see cref="HashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if both sets satisfy the constraints for equality; otherwise, <see langword="false"/>.</returns>
        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "NULL is allowed and handled as primary condition for equality. Equality check ends (fast exit) if either of the arguments is NULL without dereferencing any instance members.")]
        public static bool Equals(ObservableFileSystemPathHashSet? x, ISet<string>? y, IEqualityComparer<string> setYComparer) => SetEqualityComparerHelpers.IsSetEqual(x, y, () => x!.Comparer, () => setYComparer);

        /// <summary>
        /// Determines whether two <see cref="HashSet"/>&lt;<see langword="string"/>&gt; instances are equal by comparing their contents.
        /// </summary>
        /// <remarks>Equality is determined using the collection's comparer. Collection equality is defined as follows (ordered by hierarchy):
        /// <list type="number">
        /// <item>If both parameters are <see langword="null"/>, they are considered equal.</item>
        /// <item>If both parameters reference the same instance, they are considered equal.</item>
        /// <item>If only one is <see langword="null"/>, they are not equal.</item>
        /// <item>If both sets use the same comparer instance</item>
        /// <item>AND if both sets have the same number of elements</item>
        /// <item>AND if all elements are equal according to the set's comparer</item>
        /// </list>
        /// both collections are considered equal.
        /// </remarks>
        /// <param name="x">The first <see cref="HashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
        /// <param name="y">The second <see cref="HashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if both sets satisfy the constraints for equality; otherwise, <see langword="false"/>.</returns>
        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "NULL is allowed and handled as primary condition for equality. Equality check ends (fast exit) if either of the arguments is NULL without dereferencing any instance members.")]
        public static bool Equals(ObservableFileSystemPathHashSet? x, HashSet<string>? y) => SetEqualityComparerHelpers.IsSetEqual(x, y, () => x!.Comparer, () => y!.Comparer);

        /// <summary>
        /// Determines whether two <see cref="HashSet"/>&lt;<see langword="string"/>&gt; instances are equal by comparing their contents.
        /// </summary>
        /// <remarks>Equality is determined using the collection's comparer. Collection equality is defined as follows (ordered by hierarchy):
        /// <list type="number">
        /// <item>If both parameters are <see langword="null"/>, they are considered equal.</item>
        /// <item>If both parameters reference the same instance, they are considered equal.</item>
        /// <item>If only one is <see langword="null"/>, they are not equal.</item>
        /// <item>If both sets use the same comparer instance</item>
        /// <item>AND if both sets have the same number of elements</item>
        /// <item>AND if all elements are equal according to the set's comparer</item>
        /// </list>
        /// both collections are considered equal.
        /// </remarks>
        /// <param name="x">The first <see cref="HashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
        /// <param name="y">The second <see cref="HashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if both sets satisfy the constraints for equality; otherwise, <see langword="false"/>.</returns>
        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "NULL is allowed and handled as primary condition for equality. Equality check ends (fast exit) if either of the arguments is NULL without dereferencing any instance members.")]
        public static bool Equals(ObservableFileSystemPathHashSet? x, ObservableHashSet<string>? y) => SetEqualityComparerHelpers.IsSetEqual(x, y, () => x!.Comparer, () => y!.Comparer);

        /// <summary>
        /// Determines whether two <see cref="HashSet"/>&lt;<see langword="string"/>&gt; instances are equal by comparing their contents.
        /// </summary>
        /// <remarks>Equality is determined using the collection's comparer. Collection equality is defined as follows (ordered by hierarchy):
        /// <list type="number">
        /// <item>If both parameters are <see langword="null"/>, they are considered equal.</item>
        /// <item>If both parameters reference the same instance, they are considered equal.</item>
        /// <item>If only one is <see langword="null"/>, they are not equal.</item>
        /// <item>If both sets use the same comparer instance</item>
        /// <item>AND if both sets have the same number of elements</item>
        /// <item>AND if all elements are equal according to the set's comparer</item>
        /// </list>
        /// both collections are considered equal.
        /// </remarks>
        /// <param name="x">The first <see cref="HashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
        /// <param name="y">The second <see cref="HashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if both sets satisfy the constraints for equality; otherwise, <see langword="false"/>.</returns>
        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "NULL is allowed and handled as primary condition for equality. Equality check ends (fast exit) if either of the arguments is NULL without dereferencing any instance members.")]
        public static bool Equals(ISet<string>? y, IEqualityComparer<string> setYComparer, ObservableFileSystemPathHashSet? x) => SetEqualityComparerHelpers.IsSetEqual(x, y, () => x!.Comparer, () => setYComparer);

        /// <summary>
        /// Determines whether two <see cref="HashSet"/>&lt;<see langword="string"/>&gt; instances are equal by comparing their contents.
        /// </summary>
        /// <remarks>Equality is determined using the collection's comparer. Collection equality is defined as follows (ordered by hierarchy):
        /// <list type="number">
        /// <item>If both parameters are <see langword="null"/>, they are considered equal.</item>
        /// <item>If both parameters reference the same instance, they are considered equal.</item>
        /// <item>If only one is <see langword="null"/>, they are not equal.</item>
        /// <item>If both sets use the same comparer instance</item>
        /// <item>AND if both sets have the same number of elements</item>
        /// <item>AND if all elements are equal according to the set's comparer</item>
        /// </list>
        /// both collections are considered equal.
        /// </remarks>
        /// <param name="x">The first <see cref="HashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
        /// <param name="y">The second <see cref="HashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if both sets satisfy the constraints for equality; otherwise, <see langword="false"/>.</returns>
        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "NULL is allowed and handled as primary condition for equality. Equality check ends (fast exit) if either of the arguments is NULL without dereferencing any instance members.")]
        public static bool Equals(HashSet<string>? y, ObservableFileSystemPathHashSet? x) => SetEqualityComparerHelpers.IsSetEqual(x, y, () => x!.Comparer, () => y!.Comparer);

        /// <summary>
        /// Determines whether two <see cref="HashSet"/>&lt;<see langword="string"/>&gt; instances are equal by comparing their contents.
        /// </summary>
        /// <remarks>Equality is determined using the collection's comparer. Collection equality is defined as follows (ordered by hierarchy):
        /// <list type="number">
        /// <item>If both parameters are <see langword="null"/>, they are considered equal.</item>
        /// <item>If both parameters reference the same instance, they are considered equal.</item>
        /// <item>If only one is <see langword="null"/>, they are not equal.</item>
        /// <item>If both sets use the same comparer instance</item>
        /// <item>AND if both sets have the same number of elements</item>
        /// <item>AND if all elements are equal according to the set's comparer</item>
        /// </list>
        /// both collections are considered equal.
        /// </remarks>
        /// <param name="x">The first <see cref="HashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
        /// <param name="y">The second <see cref="HashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if both sets satisfy the constraints for equality; otherwise, <see langword="false"/>.</returns>
        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "NULL is allowed and handled as primary condition for equality. Equality check ends (fast exit) if either of the arguments is NULL without dereferencing any instance members.")]
        public static bool Equals(ObservableHashSet<string>? y, ObservableFileSystemPathHashSet? x) => SetEqualityComparerHelpers.IsSetEqual(x, y, () => x!.Comparer, () => y!.Comparer);

        /// <summary>
        /// Determines whether two <see cref="HashSet"/>&lt;<see langword="string"/>&gt; instances are equal by comparing their contents.
        /// </summary>
        /// <remarks>Equality is determined using the collection's comparer. Collection equality is defined as follows (ordered by hierarchy):
        /// <list type="number">
        /// <item>If both parameters are <see langword="null"/>, they are considered equal.</item>
        /// <item>If both parameters reference the same instance, they are considered equal.</item>
        /// <item>If only one is <see langword="null"/>, they are not equal.</item>
        /// <item>If both sets use the same comparer instance</item>
        /// <item>AND if both sets have the same number of elements</item>
        /// <item>AND if all elements are equal according to the set's comparer</item>
        /// </list>
        /// both collections are considered equal.
        /// </remarks>
        /// <param name="x">The first <see cref="HashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
        /// <param name="y">The second <see cref="HashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if both sets satisfy the constraints for equality; otherwise, <see langword="false"/>.</returns>
        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "NULL is allowed and handled as primary condition for equality. Equality check ends (fast exit) if either of the arguments is NULL without dereferencing any instance members.")]
        public static bool Equals(ObservableFileSystemPathHashSet? x, ISet<FileSystemInfo>? y, IEqualityComparer<FileSystemInfo> setYComparer) => SetEqualityComparerHelpers.IsSetEqual(x, y, () => x!.Comparer, () => setYComparer);

        /// <summary>
        /// Determines whether two <see cref="HashSet"/>&lt;<see langword="string"/>&gt; instances are equal by comparing their contents.
        /// </summary>
        /// <remarks>Equality is determined using the collection's comparer. Collection equality is defined as follows (ordered by hierarchy):
        /// <list type="number">
        /// <item>If both parameters are <see langword="null"/>, they are considered equal.</item>
        /// <item>If both parameters reference the same instance, they are considered equal.</item>
        /// <item>If only one is <see langword="null"/>, they are not equal.</item>
        /// <item>If both sets use the same comparer instance</item>
        /// <item>AND if both sets have the same number of elements</item>
        /// <item>AND if all elements are equal according to the set's comparer</item>
        /// </list>
        /// both collections are considered equal.
        /// </remarks>
        /// <param name="x">The first <see cref="HashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
        /// <param name="y">The second <see cref="HashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if both sets satisfy the constraints for equality; otherwise, <see langword="false"/>.</returns>
        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "NULL is allowed and handled as primary condition for equality. Equality check ends (fast exit) if either of the arguments is NULL without dereferencing any instance members.")]
        public static bool Equals(ObservableFileSystemPathHashSet? x, HashSet<FileSystemInfo>? y) => SetEqualityComparerHelpers.IsSetEqual(x, y, () => x!.Comparer, () => y!.Comparer);

        /// <summary>
        /// Determines whether two <see cref="HashSet"/>&lt;<see langword="string"/>&gt; instances are equal by comparing their contents.
        /// </summary>
        /// <remarks>Equality is determined using the collection's comparer. Collection equality is defined as follows (ordered by hierarchy):
        /// <list type="number">
        /// <item>If both parameters are <see langword="null"/>, they are considered equal.</item>
        /// <item>If both parameters reference the same instance, they are considered equal.</item>
        /// <item>If only one is <see langword="null"/>, they are not equal.</item>
        /// <item>If both sets use the same comparer instance</item>
        /// <item>AND if both sets have the same number of elements</item>
        /// <item>AND if all elements are equal according to the set's comparer</item>
        /// </list>
        /// both collections are considered equal.
        /// </remarks>
        /// <param name="x">The first <see cref="HashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
        /// <param name="y">The second <see cref="HashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if both sets satisfy the constraints for equality; otherwise, <see langword="false"/>.</returns>
        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "NULL is allowed and handled as primary condition for equality. Equality check ends (fast exit) if either of the arguments is NULL without dereferencing any instance members.")]
        public static bool Equals(ObservableFileSystemPathHashSet? x, ObservableHashSet<FileSystemInfo>? y) => SetEqualityComparerHelpers.IsSetEqual(x, y, () => x!.Comparer, () => y!.Comparer);

        /// <summary>
        /// Determines whether two <see cref="HashSet"/>&lt;<see langword="string"/>&gt; instances are equal by comparing their contents.
        /// </summary>
        /// <remarks>Equality is determined using the collection's comparer. Collection equality is defined as follows (ordered by hierarchy):
        /// <list type="number">
        /// <item>If both parameters are <see langword="null"/>, they are considered equal.</item>
        /// <item>If both parameters reference the same instance, they are considered equal.</item>
        /// <item>If only one is <see langword="null"/>, they are not equal.</item>
        /// <item>If both sets use the same comparer instance</item>
        /// <item>AND if both sets have the same number of elements</item>
        /// <item>AND if all elements are equal according to the set's comparer</item>
        /// </list>
        /// both collections are considered equal.
        /// </remarks>
        /// <param name="x">The first <see cref="HashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
        /// <param name="y">The second <see cref="HashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if both sets satisfy the constraints for equality; otherwise, <see langword="false"/>.</returns>
        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "NULL is allowed and handled as primary condition for equality. Equality check ends (fast exit) if either of the arguments is NULL without dereferencing any instance members.")]
        public static bool Equals(ISet<FileSystemInfo>? x, IEqualityComparer<FileSystemInfo> setXComparer, ObservableFileSystemPathHashSet? y) => SetEqualityComparerHelpers.IsSetEqual(y, x, () => y!.Comparer, () => setXComparer);

        /// <summary>
        /// Determines whether two <see cref="HashSet"/>&lt;<see langword="string"/>&gt; instances are equal by comparing their contents.
        /// </summary>
        /// <remarks>Equality is determined using the collection's comparer. Collection equality is defined as follows (ordered by hierarchy):
        /// <list type="number">
        /// <item>If both parameters are <see langword="null"/>, they are considered equal.</item>
        /// <item>If both parameters reference the same instance, they are considered equal.</item>
        /// <item>If only one is <see langword="null"/>, they are not equal.</item>
        /// <item>If both sets use the same comparer instance</item>
        /// <item>AND if both sets have the same number of elements</item>
        /// <item>AND if all elements are equal according to the set's comparer</item>
        /// </list>
        /// both collections are considered equal.
        /// </remarks>
        /// <param name="x">The first <see cref="HashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
        /// <param name="y">The second <see cref="HashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if both sets satisfy the constraints for equality; otherwise, <see langword="false"/>.</returns>
        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "NULL is allowed and handled as primary condition for equality. Equality check ends (fast exit) if either of the arguments is NULL without dereferencing any instance members.")]
        public static bool Equals(HashSet<FileSystemInfo>? x, ObservableFileSystemPathHashSet? y) => SetEqualityComparerHelpers.IsSetEqual(y, x, () => y!.Comparer, () => x!.Comparer);

        /// <summary>
        /// Determines whether two <see cref="HashSet"/>&lt;<see langword="string"/>&gt; instances are equal by comparing their contents.
        /// </summary>
        /// <remarks>Equality is determined using the collection's comparer. Collection equality is defined as follows (ordered by hierarchy):
        /// <list type="number">
        /// <item>If both parameters are <see langword="null"/>, they are considered equal.</item>
        /// <item>If both parameters reference the same instance, they are considered equal.</item>
        /// <item>If only one is <see langword="null"/>, they are not equal.</item>
        /// <item>If both sets use the same comparer instance</item>
        /// <item>AND if both sets have the same number of elements</item>
        /// <item>AND if all elements are equal according to the set's comparer</item>
        /// </list>
        /// both collections are considered equal.
        /// </remarks>
        /// <param name="x">The first <see cref="HashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
        /// <param name="y">The second <see cref="HashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if both sets satisfy the constraints for equality; otherwise, <see langword="false"/>.</returns>
        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "NULL is allowed and handled as primary condition for equality. Equality check ends (fast exit) if either of the arguments is NULL without dereferencing any instance members.")]
        public static bool Equals(ObservableHashSet<FileSystemInfo>? x, ObservableFileSystemPathHashSet? y) => SetEqualityComparerHelpers.IsSetEqual(y, x, () => y!.Comparer, () => x!.Comparer);

        public static int GetHashCode([DisallowNull] ISet<string>? obj, IEqualityComparer<string> comparer)
        {
            ArgumentNullExceptionAdvanced.ThrowIfNull(obj);
            ArgumentNullExceptionAdvanced.ThrowIfNull(comparer);

            return SetEqualityComparerHelpers.ComputeHashCode(obj, comparer);
        }

        public static int GetHashCode([DisallowNull] ISet<FileSystemInfo>? obj, IEqualityComparer<FileSystemInfo> comparer)
        {
            ArgumentNullExceptionAdvanced.ThrowIfNull(obj);
            ArgumentNullExceptionAdvanced.ThrowIfNull(comparer);

            return SetEqualityComparerHelpers.ComputeHashCode(obj, comparer);
        }

        public int GetHashCode([DisallowNull] HashSet<FileSystemInfo>? obj)
        {
            ArgumentNullExceptionAdvanced.ThrowIfNull(obj);

            return SetEqualityComparerHelpers.ComputeHashCode(obj, obj.Comparer);
        }

        public int GetHashCode([DisallowNull] ObservableHashSet<FileSystemInfo> obj)
        {
            ArgumentNullExceptionAdvanced.ThrowIfNull(obj);

            return SetEqualityComparerHelpers.ComputeHashCode(obj, obj.Comparer);
        }

        public int GetHashCode([DisallowNull] ObservableFileSystemPathHashSet obj)
        {
            ArgumentNullExceptionAdvanced.ThrowIfNull(obj);

            return SetEqualityComparerHelpers.ComputeHashCode(obj, obj.Comparer);
        }

        public int GetHashCode([DisallowNull] ObservableHashSet<string> obj)
        {
            ArgumentNullExceptionAdvanced.ThrowIfNull(obj);

            return SetEqualityComparerHelpers.ComputeHashCode(obj, obj.Comparer);
        }

        public int GetHashCode([DisallowNull] HashSet<string> obj)
        {
            ArgumentNullExceptionAdvanced.ThrowIfNull(obj);

            return SetEqualityComparerHelpers.ComputeHashCode(obj, obj.Comparer);
        }
    }
}

/// <summary>
/// A base class for equality comparers that compare file system paths represented as strings. This class provides a common implementation for normalizing file system paths and comparing them using a specified string comparer. The actual comparison logic is delegated to the underlying string comparer, which can be either case-sensitive or case-insensitive depending on the operating system's file system semantics.
/// </summary>
/// <remarks>On Linux, file system paths are case-sensitive, so the default comparer is case-sensitive. On Windows and other platforms with case-insensitive file systems, the default comparer is case-insensitive. This class also provides methods for comparing <see cref="FileSystemInfo"/> objects by their full paths.
/// <para/>To obtain an instance of an actual file system comparer, use the <see cref="Instance"/> property.</remarks>
public abstract class FileSystemPathEqualityComparer : StringComparer, IEqualityComparer<FileSystemInfo>, IEqualityComparer<string>
{
    protected StringComparer Comparer { get; }
    public static FileSystemPathEqualityComparer Instance { get; } = OperatingSystem.IsLinux()
        ? CaseSensitiveFileSystemPathEqualityComparer.Instance
        : CaseInsensitiveFileSystemPathEqualityComparer.Instance;

    protected FileSystemPathEqualityComparer(StringComparer comparer) => Comparer = comparer;

    public override bool Equals(string? x, string? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x is null || y is null)
        {
            return false;
        }

        string xNormalized = NormalizeFileSystemPath(x);
        string yNormalized = NormalizeFileSystemPath(y);

        return Comparer.Equals(xNormalized, yNormalized);
    }

    public virtual bool Equals(FileSystemInfo? x, FileSystemInfo? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x is null || y is null)
        {
            return false;
        }

        string xNormalized = NormalizeFileSystemPath(x.FullName);
        string yNormalized = NormalizeFileSystemPath(y.FullName);

        return Comparer.Equals(xNormalized, yNormalized);
    }

    public virtual bool Equals(IEqualityComparer<string>? other) => ReferenceEquals(this, other);

    internal static bool Equals(IEqualityComparer<string>? x, IEqualityComparer<string>? y) => ReferenceEquals(x, y);

    public override bool Equals(object? obj) => obj is IEqualityComparer<string> other && Equals(other);

    private static string NormalizeFileSystemPath(string fileSystemPath) => Path.TrimEndingDirectorySeparator(fileSystemPath);

    public override int GetHashCode(string? obj) => obj is not null
        ? Comparer.GetHashCode(NormalizeFileSystemPath(obj))
        : 0;

    public int GetHashCode([DisallowNull] FileSystemInfo obj) => obj is FileSystemInfo fileSystemInfo
        ? Comparer.GetHashCode(NormalizeFileSystemPath(fileSystemInfo.FullName))
        : 0;

    public override int GetHashCode() => Comparer.GetHashCode();

    public override int Compare(string? x, string? y)
    {
        if (x is null && y is null)
        {
            return 0;
        }

        if (x is null)
        {
            return -1;
        }

        if (y is null)
        {
            return 1;
        }

        return Comparer.Compare(NormalizeFileSystemPath(x), NormalizeFileSystemPath(y));
    }
}

/// <summary>
/// Provides a file system path equality comparer that performs case-sensitive comparisons using ordinal string
/// comparison rules.
/// </summary>
/// <remarks>Use this comparer when file system path comparisons must distinguish between uppercase and lowercase
/// characters, such as on case-sensitive file systems as implemented on Linux. 
/// <para/>This class is a singleton; use the <see cref="Instance"/> property to access
/// the shared instance.</remarks>
public sealed class CaseSensitiveFileSystemPathEqualityComparer : FileSystemPathEqualityComparer
{
    public static new CaseSensitiveFileSystemPathEqualityComparer Instance { get; } = new();

    private CaseSensitiveFileSystemPathEqualityComparer() : base(StringComparer.Ordinal) { }
}

/// <summary>
/// Provides a file system path equality comparer that performs case-insensitive comparisons using ordinal string
/// comparison rules.
/// </summary>
/// <remarks>Use this comparer when file system path comparisons must ignore case differences, such as on case-insensitive
/// file systems as implemented on Windows or macOS. 
/// <para/>This class is a singleton; use the <see cref="Instance"/> property to access
/// the shared instance.</remarks>
public sealed class CaseInsensitiveFileSystemPathEqualityComparer : FileSystemPathEqualityComparer
{
    public static new CaseInsensitiveFileSystemPathEqualityComparer Instance { get; } = new();

    private CaseInsensitiveFileSystemPathEqualityComparer() : base(StringComparer.OrdinalIgnoreCase) { }
}