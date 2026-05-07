namespace BionicCode.Utilities.Net;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

/// <summary>
/// Represents a set collection that provides observable change notifications and supports both hash set and list-based
/// operations, enabling efficient data binding scenarios and set semantics.
/// </summary>
/// <remarks>The <see cref="ObservableHashSet{TItem}"/> combines the uniqueness guarantees and performance characteristics of a
/// hash set with the ability to raise collection and property change notifications compatible with data-binding
/// frameworks such as WPF and WinUI. 
/// <para/>By default, it operates as a natural hash set, raising index-agnostic notifications. When
/// list-based APIs (such as <see cref="IList"/> or <see cref="IList{T}"/>) are accessed, the collection transitions into hybrid mode, maintaining
/// an internal list projection to provide index-based <see cref="CollectionChanged"/> notifications required by many UI frameworks. This hybrid mode
/// enables advanced UI features like virtualization but may incur additional performance costs for certain operations,
/// particularly removals. The collection always raises both <see cref="SetChanged"> (only bulk, index-agnostic) and <see cref="CollectionChanged"> (granular per item,
/// index-aware) events as appropriate. Thread safety is not guaranteed; callers must synchronize access if used from
/// multiple threads.</remarks>
/// <typeparam name="TItem">The type of elements contained in the set.</typeparam>
public partial class ObservableHashSet<TItem> :
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

    /// <list type="table">
    ///   <listheader>
    ///     <term>Operation</term>
    ///     <term>Default hash set mode</term>
    ///     <term>Hybrid mode</term>
    ///   </listheader>
    ///   <item>
    ///     <term><see cref="Add(TItem)"/> / <see cref="ICollection{T}.Add(TItem)"/></term>
    ///     <term>O(1)</term>
    ///     <term>O(1)</term>
    ///   </item>
    ///   <item>
    ///     <term><see cref="AddRange(ICollection{TItem}, out IList{TItem})"/> / <see cref="UnionWith(IEnumerable{TItem})"/></term>
    ///     <term>O(n)</term>
    ///     <term>O(n)</term>
    ///   </item>
    ///   <item>
    ///     <term><see cref="Remove(TItem)"/> / <see cref="IList.Remove(object)"/></term>
    ///     <term>O(1)</term>
    ///     <term>O(m) worst case; O(1) if the removed item is already the last projected item</term>
    ///   </item>
    ///   <item>
    ///     <term><see cref="RemoveRange(ICollection{TItem}, out List{TItem})"/> / <see cref="ExceptWith(IEnumerable{TItem})"/></term>
    ///     <term>O(n)</term>
    ///     <term>O(n · m) worst case</term>
    ///   </item>
    ///   <item>
    ///     <term><see cref="RemoveWhere(Predicate{TItem})"/></term>
    ///     <term>O(m)</term>
    ///     <term>O(m²) worst case</term>
    ///   </item>
    ///   <item>
    ///     <term><see cref="IntersectWith(IEnumerable{TItem})"/></term>
    ///     <term>O(m) if the other collection is already a compatible hash set; otherwise O(n + m)</term>
    ///     <term>O(m²) worst case</term>
    ///   </item>
    ///   <item>
    ///     <term><see cref="SymmetricExceptWith(IEnumerable{TItem})"/></term>
    ///     <term>O(n)</term>
    ///     <term>O(n + m²) worst case</term>
    ///   </item>
    ///   <item>
    ///     <term><see cref="Clear()"/> / <see cref="IList.Clear()"/></term>
    ///     <term>O(m)</term>
    ///     <term>O(m)</term>
    ///   </item>
    ///   <item>
    ///     <term><see cref="IList.Add(object)"/></term>
    ///     <term>O(m) on first use because the list surface must be initialized; otherwise O(1)</term>
    ///     <term>O(1)</term>
    ///   </item>
    ///   <item>
    ///     <term><see cref="IList{T}.Insert(int, TItem)"/> / <see cref="IList.Insert(int, object)"/></term>
    ///     <term>O(m) on first use; otherwise O(m)</term>
    ///     <term>O(m)</term>
    ///   </item>
    ///   <item>
    ///     <term><see cref="IList{T}.RemoveAt(int)"/> / <see cref="IList.RemoveAt(int)"/></term>
    ///     <term>O(m) on first use; otherwise O(m)</term>
    ///     <term>O(m)</term>
    ///   </item>
    ///   <item>
    ///     <term><see cref="IList{T}.this[int]"/> / <see cref="IList.this[int]"/> set accessor</term>
    ///     <term>O(m) on first use because the list surface must be initialized; otherwise O(1)</term>
    ///     <term>O(1)</term>
    ///   </item>
    /// </list>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <summary>
    /// Occurs when the contents of the set change.
    /// </summary>
    /// <remarks>Subscribe to this event to be notified when items are added to or removed from the set. The
    /// event provides details about the change through the <see cref="SetChangedEventArgs{TItem}"/>
    /// parameter.
    /// <para/>Opposed to the <see cref="CollectionChanged"/> event, which provides index-based notifications once the <see cref="ObservableHashSet{TItem}"/> has transitioned into hybrid mode, 
    /// the <see cref="SetChanged"/> event provides bulk-based notifications suitable for scenarios where multiple items are added or removed simultaneously. 
    /// <see cref="SetChanged"/> never delivers index based event or granular single-item change events. 
    /// It's the default index agnostic hash set event.
    /// <para/>Note: The <see cref="ObservableHashSet{TItem}"/> operates as a real observable natural hash set by default. 
    /// But accessing any of the explicitly implemented <see cref="IList"/> or <see cref="IList{T}"/> members 
    /// will transition the set into hybrid mode.
    /// <br/><b>This means, <see cref="ObservableHashSet{TItem}"/> will always operate in hybrid mode when used as a binding source in e.g. WPF or WinUI etc.</b></remarks>
    public event EventHandler<SetChangedEventArgs<TItem>>? SetChanged;

    protected HashSet<TItem> Items { get; }
    private readonly Dictionary<TItem, int> _indexTable = [];
    private readonly Dictionary<int, TItem> _reverseIndexTable = [];
    private readonly List<TItem> _listProjection = [];
    private int _blockReentrancyCount;
    private const int MaxNumberOfSingleItemChangesBeforeBatchChange = 100;

    // Collection transitions into hybrid mode when IList or IList<T> API surface is used.
    // In this mode, the collection maintains the internal index tables and list projection
    // to support the list API, which incurs additional overhead and degraded remove complexity.
    // The collection can transition back to pure hash set mode when the list API is no longer used, at which point the index tables and list projection are cleared to optimize performance for hash set operations.
    private readonly WriteOnce<bool> _isInHybridMode = new();

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

    /// <summary>
    /// Explicitly enables the index based hybrid mode to support data binding in UI frameworks like WPF or WinUI.
    /// </summary>
    /// <remarks>The <see cref="ObservableHashSet{TItem}"/> combines the uniqueness guarantees and performance characteristics of a
    /// hash set with the ability to raise collection and property change notifications compatible with data-binding
    /// frameworks such as WPF and WinUI. 
    /// <para/>By default, it operates as a natural hash set, raising index-agnostic notifications. When
    /// list-based APIs (such as the explicitly implemented <see cref="IList"/> or <see cref="IList{T}"/> interfaces) are accessed, the collection implicitly transitions into hybrid mode, maintaining
    /// an internal list projection to provide index-based <see cref="CollectionChanged"/> notifications required by many UI frameworks. This hybrid mode
    /// enables advanced UI features like virtualization but may incur additional performance costs for certain operations,
    /// particularly removals. The collection always raises both <see cref="SetChanged"> (only bulk, index-agnostic) and <see cref="CollectionChanged"> (granular per item,
    /// index-aware) events as appropriate. Thread safety is not guaranteed; callers must synchronize access if used from
    /// multiple threads.
    /// <br/>Calling <see cref="EnableDataBindingSupport"/> forces hybrid mode without prior access of the explicitly implemented <see cref="IList"/> or <see cref="IList{T}"/> API members.</remarks>
    /// <returns><see langword="true"/> if the collection now operates in hybrid mode and therefore supports data binding; otherwise <see langword="false"/>.</returns>
    public bool EnableDataBindingSupport()
    {
        InitializeListSurface();

        return _isInHybridMode;
    }

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

        return AddInternal(item, isCollectionChangedRequired: true, out _);
    }

    private bool AddInternal(TItem item, bool isCollectionChangedRequired, out int newIndex)
    {
        newIndex = -1;

        if (AddItem(item))
        {
            _ = RegisterItem(item, out newIndex);
            if (isCollectionChangedRequired)
            {
                BroadcastSingleSetChangeEvents(NotifyCollectionChangedAction.Add, item, newIndex);
            }

            return true;
        }

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
    /// <returns><see langword="true"/> if at least one item was added to the collection; otherwise, <see langword="false"/>.</returns>
    public bool AddRange(ICollection<TItem> items, out IList<TItem> addedItems)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(items);

        CheckReentrancy();

        addedItems = [];

        if (AddRangeInternal(items, isCollectionChangedPerItemRequired: true, isManualResetRequired: false, out List<TItem> addedItemsList, out _))
        {
            addedItems = addedItemsList;
        }

        return addedItems.Count > 0;
    }

    private bool AddRangeInternal(ICollection<TItem> items,
        bool isCollectionChangedPerItemRequired,
        bool isManualResetRequired,
        [NotNullWhen(true)] out List<TItem> addedItems,
        out int rangeStartIndex)
    {
        addedItems = [];
        rangeStartIndex = -1;

        if (items.Count == 0)
        {
            return false;
        }

        // If TRUE it disables per single-item event dispatching and triggers a single bulk change event.
        bool isResetRequired = items.Count > MaxNumberOfSingleItemChangesBeforeBatchChange
            || !_isInHybridMode;
        bool isCollectionChangedRequired = isCollectionChangedPerItemRequired
            && !isManualResetRequired
            && !isResetRequired;
        rangeStartIndex = _listProjection.Count;
        foreach (TItem item in items)
        {
            if (AddInternal(item, isCollectionChangedRequired: isCollectionChangedRequired, out _))
            {
                addedItems.Add(item);
            }
        }

        bool hasChanges = addedItems.Count > 0;
        if (hasChanges
            && isResetRequired
            && !isManualResetRequired)
        {
            BroadcastBulkSetChangeEvents(NotifyCollectionChangedAction.Reset, addedItems, [], rangeStartIndex);
        }

        return hasChanges;
    }

    /// <summary>
    /// Attempts to find an item in the collection that is equal to the specified item.
    /// </summary>
    /// <param name="equalValue">The item to search for in the collection. Equality is determined by the collection's comparer.</param>
    /// <param name="actualValue">When this method returns <see langword="true"/>, contains the actual stored item from the <see cref="ObservableHashSet{TItem}"/> that is equal to <paramref name="equalValue"/>, if found; otherwise, contains the default value of <typeparamref name="TItem"/> when the search yielded no match.</param>
    /// <returns><see langword="true"/> if an item equal to <paramref name="equalValue"/> is found; otherwise, <see langword="false"/>.</returns>
    public bool TryGetValue(TItem equalValue, [MaybeNullWhen(false)] out TItem actualValue) => TryGetItem(equalValue, out actualValue);

    /// <summary>
    /// Attempts to find an item in the collection that is equal to the specified item.
    /// </summary>
    /// <remarks>Override this method to extend the behavior of the <see cref="ObservableHashSet{TItem}.TryGetValue(TItem, out TItem)"/> member without affecting the notification behavior.</remarks>
    /// <param name="equalValue">The item to search for in the collection. Equality is determined by the collection's comparer.</param>
    /// <param name="actualValue">When this method returns <see langword="true"/>, contains the actual stored item from the <see cref="ObservableHashSet{TItem}"/> that is equal to <paramref name="equalValue"/>, if found; otherwise, contains the default value of <typeparamref name="TItem"/> when the search yielded no match.</param>
    /// <returns><see langword="true"/> if an item equal to <paramref name="equalValue"/> is found; otherwise, <see langword="false"/>.</returns>
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

        return RemoveInternal(item, isRebuildIndexRequired: _isInHybridMode, isCollectionChangedRequired: true, out _);
    }

    private bool RemoveInternal(TItem item, bool isRebuildIndexRequired, bool isCollectionChangedRequired, out int itemIndex)
    {
        itemIndex = -1;
        if (RemoveItem(item))
        {
            // If we raise events per single-item then we must rebuild the index after each change to ensure collection state consistency for the listener.
            _ = UnregisterItem(item, isRebuildIndexRequired || isCollectionChangedRequired, out itemIndex);

            if (isCollectionChangedRequired)
            {
                BroadcastSingleSetChangeEvents(NotifyCollectionChangedAction.Remove, item, itemIndex);
            }

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
    /// <param name="items">The collection of items to remove from the collection. Cannot be <see langword="null"/>.</param>
    /// <param name="removedItems">When this method returns, contains the list of items that were successfully removed from the collection. This list is empty if no items were removed.</param>
    /// <returns><see langword="true"/> if at least one item was removed from the collection; otherwise, <see langword="false"/>.</returns>
    public bool RemoveRange(ICollection<TItem> items, out List<TItem> removedItems)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(items);

        CheckReentrancy();

        return _isInHybridMode
            ? HybridModeRemoveRangeInternal(items, out removedItems) > 0
            : DefaultModeRemoveRangeInternal(items, out removedItems, out _);
    }

    private int HybridModeRemoveRangeInternal(ICollection<TItem> items, out List<TItem> removedItems)
    {
        Debug.Assert(_isInHybridMode == true, "This method is only for hybrid mode.");

        if (items.Count == 0)
        {
            removedItems = [];
            return 0;
        }

        List<KeyValuePair<int, TItem>> removeItemCandidateEntries = [];
        foreach (TItem item in items)
        {
            if (_indexTable.TryGetValue(item, out int itemIndex))
            {
                Debug.Assert(ReferenceEquals(item, _listProjection[itemIndex]), "Index table is out of sync with the list projection.");
                Debug.Assert(_reverseIndexTable.TryGetValue(itemIndex, out TItem? reverseIndexedItem) && ReferenceEquals(item, reverseIndexedItem), "Reverse index table is out of sync with the list projection.");
                removeItemCandidateEntries.Add(new KeyValuePair<int, TItem>(itemIndex, item));
            }
        }

        // Sort in descending order to ensure that the index of the next change is not affected when raising per single-item change events.
        removeItemCandidateEntries.Sort((x, y) => y.Key.CompareTo(x.Key));

        bool isResetRequired = removeItemCandidateEntries.Count > MaxNumberOfSingleItemChangesBeforeBatchChange;

        removedItems = [];
        for (int index = 0; index < removeItemCandidateEntries.Count; index++)
        {
            KeyValuePair<int, TItem> entry = removeItemCandidateEntries[index];
            TItem item = entry.Value;

            // When there are too many changes, it's more efficient to raise a Reset event instead of many single-item events.
            // This is because if single-item events are too many, then the dispatcher thread could be flooded causing the UI to freeze.
            if (RemoveInternal(item,
                isRebuildIndexRequired: !isResetRequired,
                isCollectionChangedRequired: !isResetRequired,
                out _))
            {
                removedItems.Add(item);
            }
        }

        bool hasChanges = removedItems.Count > 0;
        if (isResetRequired
            && hasChanges)
        {
            int lowestChangeIndex = removeItemCandidateEntries.Last().Key;
            BuildIndex(lowestChangeIndex);
            BroadcastBulkSetChangeEvents(NotifyCollectionChangedAction.Reset, [], removedItems, lowestChangeIndex);
        }

        return removedItems.Count;
    }

    protected bool DefaultModeRemoveRangeInternal(ICollection<TItem>? items,
        [NotNullWhen(true)] out List<TItem> removedItems,
        [NotNullWhen(true)] out List<int> removedIndices)
    {
        Debug.Assert(_isInHybridMode == false, "This method is only for default mode.");

        return RemoveRangeInternal(
            items,
            isRebuildIndexRequired: false,
            isCollectionChangedPerItemRequired: false,
            isManualResetRequired: false,
            out removedItems,
            out removedIndices);
    }

    protected bool RemoveRangeInternal(ICollection<TItem>? items,
        bool isRebuildIndexRequired,
        bool isCollectionChangedPerItemRequired,
        bool isManualResetRequired,
        [NotNullWhen(true)] out List<TItem> removedItems,
        [NotNullWhen(true)] out List<int> removedIndices)
    {
        removedIndices = [];
        removedItems = [];
        items = items.OrEmpty();

        if (items.Count == 0)
        {
            return false;
        }

        // If TRUE it disables per single-item event dispatching and triggers a single bulk change event.
        bool isResetRequired = items.Count > MaxNumberOfSingleItemChangesBeforeBatchChange
            || !_isInHybridMode;
        bool isCollectionChangedRequired = isCollectionChangedPerItemRequired
            && !isManualResetRequired
            && !isResetRequired;

        int smallestChangeIndex = int.MaxValue;
        foreach (TItem item in items)
        {
            if (RemoveInternal(
                item,
                isRebuildIndexRequired: false, // May reindex if 'isCollectionChangedPerItemRequired' argument is true
                isCollectionChangedRequired: isCollectionChangedRequired,
                out int itemIndex))
            {
                removedItems.Add(item);
                removedIndices.Add(itemIndex);
                smallestChangeIndex = Math.Min(smallestChangeIndex, itemIndex);
            }
        }

        bool hasChanges = removedItems.Count > 0;
        if (hasChanges
            && isRebuildIndexRequired
            && !isCollectionChangedRequired)
        {
            BuildIndex(smallestChangeIndex);
        }

        if (hasChanges
            && isResetRequired
            && !isManualResetRequired)
        {
            BroadcastBulkSetChangeEvents(NotifyCollectionChangedAction.Reset, [], removedItems, smallestChangeIndex);
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

        return _isInHybridMode
            ? HybridModeRemoveWhereInternal(match)
            : DefaultModeRemoveWhereInternal(match);
    }

    private int HybridModeRemoveWhereInternal(Predicate<TItem> match)
    {
        Debug.Assert(_isInHybridMode == true, "This method is only for hybrid mode.");

        if (Items.Count == 0)
        {
            return 0;
        }

        List<KeyValuePair<int, TItem>> removeItemCandidateEntries = [];
        for (int index = _listProjection.Count - 1; index >= 0; index--)
        {
            TItem item = _listProjection[index];
            Debug.Assert(_reverseIndexTable.TryGetValue(index, out TItem? reverseIndexedItem) && ReferenceEquals(item, reverseIndexedItem), "Reverse index table is out of sync with the list projection.");
            if (match.Invoke(item)
                && _indexTable.TryGetValue(item, out int itemIndex))
            {
                Debug.Assert(itemIndex == index, "Index table is out of sync with the list projection.");
                removeItemCandidateEntries.Add(new KeyValuePair<int, TItem>(itemIndex, item));
            }
        }

        bool isResetRequired = removeItemCandidateEntries.Count > MaxNumberOfSingleItemChangesBeforeBatchChange;
        List<TItem> removedItems = [];
        for (int index = 0; index < removeItemCandidateEntries.Count; index++)
        {
            KeyValuePair<int, TItem> entry = removeItemCandidateEntries[index];
            TItem item = entry.Value;

            // When there are too many changes, it's more efficient to raise a Reset event instead of many single-item events.
            // This is because if single-item events are too many, then the dispatcher thread could be flooded causing the UI to freeze.
            if (RemoveInternal(item,
                isRebuildIndexRequired: !isResetRequired,
                isCollectionChangedRequired: !isResetRequired,
                out _))
            {
                removedItems.Add(item);
            }
        }

        bool hasChanges = removedItems.Count > 0;
        if (isResetRequired
            && hasChanges)
        {
            int lowestChangeIndex = removeItemCandidateEntries.Last().Key;
            BuildIndex(lowestChangeIndex);
            BroadcastBulkSetChangeEvents(NotifyCollectionChangedAction.Reset, [], removedItems, -1);
        }

        return removedItems.Count;
    }

    private int DefaultModeRemoveWhereInternal(Predicate<TItem> match)
    {
        Debug.Assert(_isInHybridMode == false, "This method is only for default mode.");

        var removedItems = new List<TItem>();

        TItem[] items = TakeSnapshot();

        for (int index = items.Length - 1; index >= 0; index--)
        {
            TItem item = items[index];
            if (match.Invoke(item)
                && RemoveInternal(item, isRebuildIndexRequired: false, isCollectionChangedRequired: false, out _))
            {
                removedItems.Add(item);
            }
        }

        bool hasChanges = removedItems.Count > 0;
        if (hasChanges)
        {
            BroadcastBulkSetChangeEvents(NotifyCollectionChangedAction.Remove, [], removedItems, -1);
        }

        return removedItems.Count;
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
            TItem[] oldItems = TakeSnapshot();
            ClearItems();
            _indexTable.Clear();
            _reverseIndexTable.Clear();
            _listProjection.Clear();

            BroadcastBulkSetChangeEvents(NotifyCollectionChangedAction.Reset, [], oldItems, 0);
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

        // This is already the empty set; return.
        if (Count == 0)
        {
            return;
        }

        // Special case if other is this; a set minus itself is the empty set.
        if (ReferenceEquals(other, this))
        {
            Clear();
            return;
        }

        if (other is ICollection<TItem> otherAsCollection
            && otherAsCollection.Count == 0)
        {
            return;
        }

        if (_isInHybridMode)
        {
            HybridModeExceptWithInternal(other);
        }
        else
        {
            DefaultModeExceptWithInternal(other);
        }
    }

    private void HybridModeExceptWithInternal(IEnumerable<TItem> other)
    {
        // Remove every element in other from this.
        Debug.Assert(_isInHybridMode == true, "This method is only for hybrid mode.");

        List<KeyValuePair<int, TItem>> removeItemCandidateEntries = [];
        foreach (TItem otherItem in other)
        {
            if (Contains(otherItem)
                && _indexTable.TryGetValue(otherItem, out int itemIndex)
                && _reverseIndexTable.TryGetValue(itemIndex, out TItem? originalIndexedItem))
            {
                Debug.Assert(ReferenceEquals(_listProjection[itemIndex], originalIndexedItem), "Reverse index table is out of sync with the list projection.");
                removeItemCandidateEntries.Add(new KeyValuePair<int, TItem>(itemIndex, originalIndexedItem));
            }
        }

        if (removeItemCandidateEntries.Count == 0)
        {
            return;
        }

        bool isResetRequired = removeItemCandidateEntries.Count > MaxNumberOfSingleItemChangesBeforeBatchChange;
        List<TItem> removedItems = [];

        // Sort in descending order to ensure that the index of the next change is not affected when raising per single-item change events.
        removeItemCandidateEntries.Sort((x, y) => y.Key.CompareTo(x.Key));

        for (int index = 0; index < removeItemCandidateEntries.Count; index++)
        {
            KeyValuePair<int, TItem> entry = removeItemCandidateEntries[index];
            TItem item = entry.Value;

            // When there are too many changes, it's more efficient to raise a Reset event instead of many single-item events.
            // This is because if single-item events are too many, then the dispatcher thread could be flooded causing the UI to freeze.
            if (RemoveInternal(item,
                isRebuildIndexRequired: !isResetRequired,
                isCollectionChangedRequired: !isResetRequired,
                out _))
            {
                removedItems.Add(item);
            }
        }

        bool hasChanges = removedItems.Count > 0;
        if (isResetRequired
            && hasChanges)
        {
            int lowestChangeIndex = removeItemCandidateEntries.Last().Key;
            BuildIndex(lowestChangeIndex);
            BroadcastBulkSetChangeEvents(NotifyCollectionChangedAction.Reset, [], removedItems, -1);
        }

        return;
    }

    private void DefaultModeExceptWithInternal(IEnumerable<TItem> other)
    {
        // Remove every element in other from this.
        Debug.Assert(_isInHybridMode == false, "This method is only for default mode.");

        List<TItem> removedItems = [];

        // Prevent enumeration of self if other is this, which could cause issues since we are modifying the collection while enumerating.
        foreach (TItem otherItem in other)
        {
            if (TryGetItem(otherItem, out TItem originalIndexedItem)
                && RemoveInternal(originalIndexedItem, isRebuildIndexRequired: false, isCollectionChangedRequired: false, out _))
            {
                removedItems.Add(originalIndexedItem);
            }
        }

        bool hasChanges = removedItems.Count > 0;
        if (hasChanges)
        {
            BroadcastBulkSetChangeEvents(NotifyCollectionChangedAction.Reset, [], removedItems, -1);
        }

        return;
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
    public Enumerator GetEnumerator() => _isInHybridMode
        ? new(_listProjection)
        : new Enumerator(Items);
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

        // If this set is empty, then the intersection with other set is always empty.
        if (Count == 0)
        {
            return;
        }

        // Special-case this; the intersection of a set with itself is an unchanged set.
        if (ReferenceEquals(other, this))
        {
            return;
        }

        if (other is ICollection<TItem> otherAsCollection)
        {
            if (otherAsCollection.Count == 0)
            {
                Clear();
                return;
            }
        }

        // If other is a hash set that uses same equality comparer, intersect is much faster
        // because we can use other's Contains
        HashSet<TItem> otherAsHashSet = NormalizeEnumerableArgument(other);
        if (otherAsHashSet.Count == 0)
        {
            Clear();
            return;
        }

        if (_isInHybridMode)
        {
            HybridModeIntersectWithInternal(otherAsHashSet);
        }
        else
        {
            DefaultModeIntersectWithInternal(otherAsHashSet);
        }
    }

    private void HybridModeIntersectWithInternal(HashSet<TItem> other)
    {
        Debug.Assert(_isInHybridMode == true, "This method is only for hybrid mode.");

        List<KeyValuePair<int, TItem>> removeItemCandidateEntries = [];
        for (int index = _listProjection.Count - 1; index >= 0; index--)
        {
            TItem item = _listProjection[index];
            Debug.Assert(_reverseIndexTable.TryGetValue(index, out TItem? itemFromLookupTable) && ReferenceEquals(item, itemFromLookupTable), "Index table is out of sync with the list projection.");
            if (!other.Contains(item)
                && _indexTable.TryGetValue(item, out int itemIndex))
            {
                Debug.Assert(itemIndex == index, "Index table is out of sync with the list projection.");
                removeItemCandidateEntries.Add(new KeyValuePair<int, TItem>(itemIndex, item));
            }
        }

        bool isResetRequired = removeItemCandidateEntries.Count > MaxNumberOfSingleItemChangesBeforeBatchChange;
        List<TItem> removedItems = [];
        for (int index = 0; index < removeItemCandidateEntries.Count; index++)
        {
            KeyValuePair<int, TItem> entry = removeItemCandidateEntries[index];
            TItem item = entry.Value;

            // When there are too many changes, it's more efficient to raise a Reset event instead of many single-item events.
            // This is because if single-item events are too many, then the dispatcher thread could be flooded causing the UI to freeze.
            if (RemoveInternal(item,
                isRebuildIndexRequired: !isResetRequired,
                isCollectionChangedRequired: !isResetRequired,
                out _))
            {
                removedItems.Add(item);
            }
        }

        bool hasChanges = removedItems.Count > 0;
        if (isResetRequired
            && hasChanges)
        {
            int lowestChangeIndex = removeItemCandidateEntries.Last().Key;
            BuildIndex(lowestChangeIndex);
            BroadcastBulkSetChangeEvents(NotifyCollectionChangedAction.Reset, [], removedItems, -1);
        }

        return;
    }

    private void DefaultModeIntersectWithInternal(HashSet<TItem> other)
    {
        Debug.Assert(_isInHybridMode == false, "This method is only for default mode.");

        var removedItems = new List<TItem>();

        TItem[] items = TakeSnapshot();

        for (int index = items.Length - 1; index >= 0; index--)
        {
            TItem item = items[index];
            if (!other.Contains(item))
            {
                if (RemoveInternal(item, isRebuildIndexRequired: false, isCollectionChangedRequired: false, out _))
                {
                    removedItems.Add(item);
                }
            }
        }

        bool hasChanges = removedItems.Count > 0;
        if (hasChanges)
        {
            BroadcastBulkSetChangeEvents(NotifyCollectionChangedAction.Remove, [], removedItems, -1);
        }

        return;
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
    /// <para/>This method raises the <see cref="CollectionChanged"/> event with <see cref="NotifyCollectionChangedAction.Reset"/> action 
    /// if the number of total changes exceeds the threshold <see cref="MaxNumberOfSingleItemChangesBeforeBatchChange"/>.
    /// <br/>Otherwise, <see cref="CollectionChanged"/> is delivered per item as <see cref="NotifyCollectionChangedAction.Add"/> and <see cref="NotifyCollectionChangedAction.Remove"/> with te associated change index
    /// <para/>This method raises the <see cref="PropertyChanged"/> event for the <see cref="Count"/> and indexer properties.</remarks>
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

        // If other is a hash set that uses same equality comparer, intersect is much faster
        // because we can use other's Contains
        HashSet<TItem> otherAsHashSet = NormalizeEnumerableArgument(other);

        if (_isInHybridMode)
        {
            HybridModeSymmetricExceptWithInternal(otherAsHashSet);
        }
        else
        {
            DefaultModeSymmetricExceptWithInternal(otherAsHashSet);
        }
    }

    private void HybridModeSymmetricExceptWithInternal(HashSet<TItem> other)
    {

        Debug.Assert(_isInHybridMode == true, "This method is only for hybrid mode.");

        // Important: don't allow duplicates in List<KeyValuePair<int, TItem>>.
        // It's not possible to create duplicates in this context.
        // This is just a reminder for future refactoring.
        //
        // WPF’s collection/view pipeline is explicitly designed for incremental replay of single-item changes.
        // CollectionView.ProcessCollectionChanged handles one change at a time, and ListCollectionView.ProcessCollectionChangedWithAdjustedIndex
        // updates internal state incrementally. That is the normal fast path.
        // WPF explicitly rejects range add / remove / replace / move events,
        // so the real choices are basically many single-item events or one brutal Reset.

        List<KeyValuePair<int, TItem>> removeItemCandidateEntries = [];

        // We must process all removes before we can process adds, otherwise we might end up with stale indices since a remove operation can shift indices.
        List<TItem> addedPendingPool = [];
        foreach (TItem item in other)
        {
            if (TryGetItem(item, out TItem? originalItem))
            {
                if (_indexTable.TryGetValue(originalItem, out int existingItemIndex))
                {
#if DEBUG
                    Debug.Assert(ReferenceEquals(originalItem, _listProjection[existingItemIndex]), "Index table is out of sync with the list projection.");
                    Debug.Assert(_reverseIndexTable.TryGetValue(existingItemIndex, out TItem? itemFromReverseLookup) && ReferenceEquals(originalItem, itemFromReverseLookup), "Reverse index table is out of sync with the list projection.");
#endif
                    removeItemCandidateEntries.Add(new KeyValuePair<int, TItem>(existingItemIndex, originalItem));
                }
            }
            else
            {
                addedPendingPool.Add(originalItem);
            }
        }

        // We only need to sort removed items by their original indices in descending order to ensure optimal order for UI data binding.
        // However, the real index will potentially change/shift as we process removes,
        // but since we are removing from the end towards the start, the ordering will remain correct.
        removeItemCandidateEntries.Sort((item1, item2) => item2.Key.CompareTo(item1.Key));

        bool isResetRequired = removeItemCandidateEntries.Count + addedPendingPool.Count > MaxNumberOfSingleItemChangesBeforeBatchChange;
        List<TItem> addedItems = [];
        List<TItem> removedItems = [];
        bool hasChanges = false;
        int lowestChangeIndex = int.MaxValue;

        // When there are too many changes, it's more efficient to raise a Reset event instead of many single-item events.
        // This is because if single-item events are too many, then the dispatcher thread could be flooded causing the UI to freeze.
        // We set argument 'isManualResetRequired' to true so that we can dispatch a single bulk event for remove and add operations together.
        foreach (KeyValuePair<int, TItem> itemEntry in removeItemCandidateEntries)
        {
            TItem item = itemEntry.Value;
            if (RemoveInternal(item,
                isRebuildIndexRequired: !isResetRequired,
                isCollectionChangedRequired: !isResetRequired,
                out _))
            {
                removedItems.Add(item);
                lowestChangeIndex = Math.Min(lowestChangeIndex, itemEntry.Key);
            }

            hasChanges = removedItems.Count > 0;
        }

        hasChanges |= AddRangeInternal(addedPendingPool,
            isCollectionChangedPerItemRequired: !isResetRequired,
            isManualResetRequired: isResetRequired,
            out addedItems,
            out _);

        if (hasChanges
            && isResetRequired)
        {
            if (removedItems.Count > 0)
            {
                BuildIndex(lowestChangeIndex);
            }

            BroadcastBulkSetChangeEvents(NotifyCollectionChangedAction.Reset, addedItems, removedItems, -1);
        }
    }

    private void DefaultModeSymmetricExceptWithInternal(HashSet<TItem> other)
    {
        var removedItems = new List<TItem>();
        var addedItems = new List<TItem>();

        Debug.Assert(_isInHybridMode == false, "This method is only for default mode.");

        foreach (TItem item in other)
        {
            if (RemoveInternal(item, isRebuildIndexRequired: false, isCollectionChangedRequired: false, out _))
            {
                removedItems.Add(item);
            }
            else
            {
                _ = AddInternal(item, isCollectionChangedRequired: false, out _);
                addedItems.Add(item);
            }
        }

        bool hasChanges = addedItems.Count > 0 || removedItems.Count > 0;
        if (hasChanges)
        {
            BroadcastBulkSetChangeEvents(NotifyCollectionChangedAction.Reset, addedItems, removedItems, -1);
        }

        return;
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
    /// <para/>This method raises the <see cref="PropertyChanged"/> event for the <see cref="Count"/> and the indexer properties.</remarks>
    /// <param name="other">The collection whose elements are to be added to the set. Cannot be <see langword="null"/>.</param>
    public void UnionWith(IEnumerable<TItem> other)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(other);

        CheckReentrancy();

        foreach (TItem item in other)
        {
            _ = AddInternal(item, isCollectionChangedRequired: true, out _);
#if DEBUG
            if (_isInHybridMode)
            {
                bool hasMatchingOriginalItem = TryGetItem(item, out TItem? originalItem);
                Debug.Assert(hasMatchingOriginalItem, "Newly added item is not found in the set after UnionWith.");
                Debug.Assert(_indexTable.TryGetValue(originalItem, out int itemIndex) && ReferenceEquals(_listProjection[itemIndex], originalItem), "Index table is out of sync with the list projection after UnionWith.");
                Debug.Assert(_reverseIndexTable.TryGetValue(itemIndex, out TItem? itemFromReverseLookup) && ReferenceEquals(itemFromReverseLookup, originalItem), "Reverse index table is out of sync with the list projection after UnionWith.");
            }
#endif
        }
    }

    // Normalize to HashSet<T>.
    // If other is a hash set that uses same equality comparer, intersect is much faster
    // because we can use other's Contains
    private HashSet<TItem> NormalizeEnumerableArgument(IEnumerable<TItem> other) => other is ObservableHashSet<TItem> observableHashSet && Comparer.Equals(observableHashSet.Comparer)
        ? observableHashSet.Items
        : other is HashSet<TItem> hashSet && Comparer.Equals(hashSet.Comparer)
            ? hashSet
            : new HashSet<TItem>(other, Comparer);

    protected void BroadcastSingleSetChangeEvents(NotifyCollectionChangedAction changedAction, TItem item, int changeIndex, TItem oldItem = default)
    {
        if (_isInHybridMode)
        {
            OnCollectionChanged(changedAction, item, changeIndex);
        }

        if (changedAction is NotifyCollectionChangedAction.Add)
        {
            BroadcastDefaultSetChangedEvents([item], []);
        }
        else if (changedAction is NotifyCollectionChangedAction.Remove)
        {
            BroadcastDefaultSetChangedEvents([], [item]);
        }
        else if (changedAction is NotifyCollectionChangedAction.Replace)
        {
            BroadcastDefaultSetChangedEvents([item], [oldItem]);
        }
        else
        {
            throw new NotSupportedException($"The collection changed action '{changedAction}' is not supported for single set change events. Only Add, Remove and Replace actions are supported.");
        }
    }

    protected void BroadcastBulkSetChangeEvents(NotifyCollectionChangedAction changedAction, IList<TItem>? addedItems, IList<TItem>? removedItems, int changeStartIndex)
    {
        addedItems ??= [];
        removedItems ??= [];

        if (_isInHybridMode)
        {
            switch (changedAction)
            {
                case NotifyCollectionChangedAction.Reset:
                    OnCollectionChangedReset();
                    BroadcastDefaultSetChangedEvents(addedItems, removedItems);
                    break;
                default:
                    throw new NotSupportedException($"The collection changed action '{changedAction}' is not supported for bulk set change events. Only Add, Remove and Reset actions are supported.");
            }
        }
        else
        {
            BroadcastDefaultSetChangedEvents(addedItems, removedItems);
        }
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

            // When in hybrid-mode we already have broadcasted granular index-based collection changed events,
            // so we can skip raising an additional Reset event for the CollectionChanged event.
            // For non-hybrid mode we need to raise a Reset event as we don't have granular index-based collection changed events.
            if (!_isInHybridMode)
            {
                OnCollectionChangedReset();
            }
        }
        else if (addedItems.Count > 0)
        {
            if (addedItems.Count > 1)
            {
                OnSetChanged(NotifyCollectionChangedAction.Add, addedItems, []);

                // When in hybrid-mode we already have broadcasted granular index-based collection changed events,
                // so we can skip raising an additional event for the CollectionChanged event.
                // For non-hybrid mode we need to raise an anonymous item change event as we don't have granular index-based representation.
                if (!_isInHybridMode)
                {
                    OnCollectionChangedReset();
                }
            }
            else
            {
                OnSetChanged(NotifyCollectionChangedAction.Add, addedItems[0]);

                // When in hybrid-mode we already have broadcasted granular index-based collection changed events,
                // so we can skip raising an additional event for the CollectionChanged event.
                // For non-hybrid mode we need to raise an anonymous item change event as we don't have granular index-based representation.
                if (!_isInHybridMode)
                {
                    OnCollectionChanged(NotifyCollectionChangedAction.Add, addedItems[0], -1);
                }
            }
        }
        else if (removedItems.Count > 0)
        {
            if (removedItems.Count > 1)
            {
                OnSetChanged(NotifyCollectionChangedAction.Remove, [], removedItems);

                // When in hybrid-mode we already have broadcasted granular index-based collection changed events,
                // so we can skip raising an additional event for the CollectionChanged event.
                // For non-hybrid mode we need to raise an anonymous item change event as we don't have granular index-based representation.
                if (!_isInHybridMode)
                {
                    OnCollectionChangedReset();
                }
            }
            else
            {
                OnSetChanged(NotifyCollectionChangedAction.Remove, removedItems[0]);

                // When in hybrid-mode we already have broadcasted granular index-based collection changed events,
                // so we can skip raising an additional event for the CollectionChanged event.
                // For non-hybrid mode we need to raise an anonymous item change event as we don't have granular index-based representation.
                if (!_isInHybridMode)
                {
                    OnCollectionChanged(NotifyCollectionChangedAction.Remove, removedItems[0], -1);
                }
            }
        }
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

            // Ensure that the reverse table does not contain stale indices after we update the table with the new index for the indexed item.
            // Otherwise, multiple indices could point to the same item or remain in the set despite the item being already removed.
            // The main reason for this is that middle remove operations cause a shift of indices to the left.
            if (_indexTable.TryGetValue(indexedItem, out int oldIndex))
            {
                _ = _reverseIndexTable.Remove(oldIndex);
            }

            _indexTable[indexedItem] = index;
            _reverseIndexTable[index] = indexedItem;
        }

        Debug.Assert(_indexTable.Count == _reverseIndexTable.Count && _reverseIndexTable.Count == _listProjection.Count, "Index tables and list projection are expected to be always in sync.");
    }

    private void InitializeListSurface()
    {
        if (_isInHybridMode)
        {
            return;
        }

        _listProjection.Clear();
        _indexTable.Clear();
        _reverseIndexTable.Clear();
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
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator for the collection of items.</returns>
    IEnumerator<TItem> IEnumerable<TItem>.GetEnumerator() => GetEnumerator();

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

    protected TItem[] TakeSnapshot() => Items.ToArray();

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
    private void OnSetChangedReset() => SetChanged?.Invoke(this, new SetChangedEventArgs<TItem>(NotifyCollectionChangedAction.Reset, default!));
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
        InitializeListSurface();

        // index == Count  translates to appending to the end of the list for native 'List<T>.Insert(...)' behavior.
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfIndexOutOfRange(index, 0, _listProjection.Count);

        if (AddItem(item))
        {
            RegisterInsertedItem(item, index, isRebuildIndexRequired: true);
            BroadcastSingleSetChangeEvents(NotifyCollectionChangedAction.Add, item, index);
        }
    }

    [SuppressMessage("Design", "CA1033:Interface methods should be callable by child types", Justification = "Not overridable behavior. IList<T> implementation only exist to add performance boost for WPF data binding support.")]
    void IList<TItem>.RemoveAt(int index)
    {
        InitializeListSurface();

        ArgumentOutOfRangeExceptionAdvanced.ThrowIfIndexOutOfRange(index, _listProjection);

        if (_reverseIndexTable.TryGetValue(index, out TItem? item))
        {
            _ = RemoveInternal(item, isRebuildIndexRequired: true, isCollectionChangedRequired: true, out _);
        }
    }

    TItem IList<TItem>.this[int index]
    {
        [SuppressMessage("Design", "CA1033:Interface methods should be callable by child types", Justification = "Not overridable behavior. IList<T> implementation only exist to add performance boost for WPF data binding aupport.")]
        get
        {
            InitializeListSurface();

            ArgumentOutOfRangeExceptionAdvanced.ThrowIfIndexOutOfRange(index, _listProjection);

            return _listProjection[index];
        }

        [SuppressMessage("Design", "CA1033:Interface methods should be callable by child types", Justification = "Not overridable behavior. IList<T> implementation only exist to add performance boost for WPF data binding aupport.")]
        set
        {
            InitializeListSurface();

            ArgumentOutOfRangeExceptionAdvanced.ThrowIfIndexOutOfRange(index, _listProjection);

            if (_reverseIndexTable.TryGetValue(index, out TItem existingItem)
                && RemoveItem(existingItem))
            {
                if (AddItem(value))
                {
                    UpdateItemAt(value, existingItem, index);
                    BroadcastSingleSetChangeEvents(NotifyCollectionChangedAction.Replace, value, index, existingItem);
                }
                else
                {
                    // Roll back
                    if (!AddItem(existingItem))
                    {
                        // This should never happen as we just removed the existing item, but we should at least try to keep the internal state consistent in this case.
                        throw new InvalidOperationException($"Failed to roll back after failed attempt to replace item at index '{index}' with value '{value}'. The collection is now in an inconsistent state.");
                    }
                }
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

        InitializeListSurface();
        return Add(item) && _indexTable.TryGetValue(item, out int index)
            ? index
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

        return ((IList<TItem>)this).IndexOf(item);
    }

    [SuppressMessage("Design", "CA1033:Interface methods should be callable by child types", Justification = "Not overridable behavior. IList<T> implementation only exist to add performance boost for WPF data binding support.")]
    void IList.Insert(int index, object? value)
    {
        if (value is not TItem item)
        {
            throw new InvalidCastException($"Unable to convert '{value?.GetType().FullName ?? "NULL"}' to '{typeof(TItem).FullName}'.");
        }

        ((IList<TItem>)this).Insert(index, item);
    }

    void IList.Remove(object? value)
    {
        if (value is not TItem item)
        {
            throw new InvalidCastException($"Unable to convert '{value?.GetType().FullName ?? "NULL"}' to '{typeof(TItem).FullName}'.");
        }

        _ = Remove(item);
    }

    [SuppressMessage("Design", "CA1033:Interface methods should be callable by child types", Justification = "Not overridable behavior. IList<T> implementation only exist to add performance boost for WPF data binding support.")]
    void IList.RemoveAt(int index) => ((IList<TItem>)this).RemoveAt(index);

    object? IList.this[int index]
    {
        [SuppressMessage("Design", "CA1033:Interface methods should be callable by child types", Justification = "Not overridable behavior. IList<T> implementation only exist to add performance boost for WPF data binding support.")]
        get => ((IList<TItem>)this)[index];

        [SuppressMessage("Design", "CA1033:Interface methods should be callable by child types", Justification = "Not overridable behavior. IList<T> implementation only exist to add performance boost for WPF data binding support.")]
        set
        {
            if (value is not TItem item)
            {
                throw new InvalidCastException($"Unable to convert '{value?.GetType().FullName ?? "NULL"}' to '{typeof(TItem).FullName}'.");
            }

            ((IList<TItem>)this)[index] = item;
        }
    }
    #endregion IList

    #region Enumerator
    public struct Enumerator : IEnumerator<TItem>
    {
        private List<TItem>.Enumerator _listEnumerator;
        private HashSet<TItem>.Enumerator _hashSetEnumerator;
        private readonly bool _isHashSetEnumeratorAvailable;

        internal Enumerator(List<TItem> list)
        {
            ArgumentNullExceptionAdvanced.ThrowIfNull(list);
            _listEnumerator = list.GetEnumerator();
            _isHashSetEnumeratorAvailable = false;
        }

        internal Enumerator(HashSet<TItem> hashSet)
        {
            ArgumentNullExceptionAdvanced.ThrowIfNull(hashSet);
            _hashSetEnumerator = hashSet.GetEnumerator();
            _isHashSetEnumeratorAvailable = true;
        }

        public TItem Current => _isHashSetEnumeratorAvailable
            ? _hashSetEnumerator!.Current
            : _listEnumerator.Current;

        object IEnumerator.Current => Current!;
        public void Dispose()
        {
            _listEnumerator.Dispose();
            _hashSetEnumerator.Dispose();
        }

        public bool MoveNext() => _isHashSetEnumeratorAvailable
            ? _hashSetEnumerator.MoveNext()
            : _listEnumerator.MoveNext();

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

        return base.RemoveWhere(fileSystemPath => match.Invoke(new FileInfo(fileSystemPath)));
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

        return base.RemoveWhere(fileSystemPath => match.Invoke(new DirectoryInfo(fileSystemPath)));
    }

    public bool TryGetValue(FileInfo item, [MaybeNullWhen(false)] out FileInfo value)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(item);

        value = default;

        if (base.TryGetValue(item.FullName, out _))
        {
            value = item;
            return true;
        }

        return false;
    }

    public bool TryGetValue(DirectoryInfo item, [MaybeNullWhen(false)] out DirectoryInfo value)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(item);

        value = default;

        if (base.TryGetValue(item.FullName, out _))
        {
            value = item;
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
