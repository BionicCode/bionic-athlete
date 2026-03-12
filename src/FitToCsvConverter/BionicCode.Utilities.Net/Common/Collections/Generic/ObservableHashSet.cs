namespace BionicCode.Utilities.Net.Common.Collections.Generic;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

public class ObservableHashSet<TItem> :
    ICollection<TItem>,
    IEnumerable<TItem>,
    IReadOnlyCollection<TItem>,
    ISet<TItem>,
    IEnumerable,
    IReadOnlySet<TItem>,
    IDeserializationCallback,
    ISerializable,
    INotifyCollectionChanged,
    INotifyPropertyChanged
{
    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;
    /// <inheritdoc/>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;
    protected HashSet<TItem> Items { get; }

    public ObservableHashSet() => Items = [];

    [SuppressMessage("Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>")]
    public ObservableHashSet(IEnumerable<TItem> collection)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(collection);
        Items = new(collection);
    }

    [SuppressMessage("Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>")]
    public ObservableHashSet(IEqualityComparer<TItem>? comparer) => Items = new(comparer);

    [SuppressMessage("Style", "IDE0028:Simplify collection initialization", Justification = "<Pending>")]
    public ObservableHashSet(int capacity)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(capacity);
        Items = new(capacity);
    }

    public ObservableHashSet(IEnumerable<TItem> collection, IEqualityComparer<TItem>? comparer)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(collection);
        Items = new HashSet<TItem>(collection, comparer);
    }

    public ObservableHashSet(int capacity, IEqualityComparer<TItem>? comparer)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(capacity);
        Items = new HashSet<TItem>(capacity, comparer);
    }

    public int Capacity => Items.Capacity;
    /// <summary>
    /// The equality comparer used to determine equality of items in the set. This comparer is used for all operations that involve comparing items, such as adding, removing, and checking for the presence of items in the set.
    /// </summary>
    /// <value>The equality <see cref="IEqualityComparer{T}"/> used by the set.</value>
    public IEqualityComparer<TItem> Comparer => Items.Comparer;

    public int Count => Items.Count;

    int ICollection<TItem>.Count => Count;
    bool ICollection<TItem>.IsReadOnly { get; } = false;

    /// <summary>Adds an item to the <see cref="ObservableHashSet{T}"/> if it is not already present.</summary>
    /// <paramref name="item"/> is the item to add to the set. The value can be <c>null</c> for reference types.
    /// <remarks>Use this method to add an item to the set. If the item is already present, the set remains unchanged and the method returns <see langword="false"/>; otherwise, the item is added and the method returns <see langword="true"/>.
    /// <para/>This method raises the <see cref="CollectionChanged"/> event with <see cref="NotifyCollectionChangedAction.Add"/> action where the change index is always '-1'.
    /// <para/>This method raises the <see cref="PropertyChanged"/> event for the <see cref="Count"/> property.</remarks>
    /// <returns><see langword="true"/> if the item was added to the set; <see langword="false"/> if the item was already present.</returns>
    public virtual bool Add(TItem item)
    {
        if (Items.Add(item))
        {
            OnCountChanged();
            OnCollectionChanged(NotifyCollectionChangedAction.Add, item);

            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to find a value in the set that is equal to the specified item.
    /// </summary>
    /// <param name="equalValue">The item to search for in the set. Equality is determined by the set's comparer.</param>
    /// <param name="actualValue">When this method returns <see langword="true"/>, contains the value from the set that is equal to <paramref
    /// name="equalValue"/>; otherwise, contains the default value for the type.</param>
    /// <returns><see langword="true"/> if a value equal to <paramref name="equalValue"/> was found in the set; otherwise, <see
    /// langword="false"/>.</returns>
    public virtual bool TryGetValue(TItem equalValue, [MaybeNullWhen(false)] out TItem actualValue) => Items.TryGetValue(equalValue, out actualValue);

    /// <summary>
    /// Attempts to remove the specified item from the set.
    /// </summary>
    /// <param name="item">The item to remove from the set. The value can be <c>null</c> for reference types.</param>
    /// <returns><see langword="true"/> if the item was successfully removed; otherwise, <see langword="false"/>.</returns>
    /// <remarks>Use this method to remove the item <paramref name="item"/> from the set and return a value indicating whether the removal was successful.
    /// <para/>This method raises the <see cref="CollectionChanged"/> event with <see cref="NotifyCollectionChangedAction.Remove"/> action where the change index is always '-1'.
    /// <para/>This method raises the <see cref="PropertyChanged"/> event for the <see cref="Count"/> property.</remarks>
    public virtual bool Remove(TItem item)
    {
        if (Items.Remove(item))
        {
            OnCountChanged();
            OnCollectionChanged(NotifyCollectionChangedAction.Remove, item);

            return true;
        }

        return false;
    }

    /// <summary>
    /// Removes all elements from the collection that match the conditions defined by the specified predicate.
    /// </summary>
    /// <remarks>If one or more elements are removed, the collection raises change notifications. Use this
    /// method to efficiently remove multiple items based on custom criteria.
    /// <para/>This method raises the <see cref="CollectionChanged"/> event with <see cref="NotifyCollectionChangedAction.Remove"/> action including the set of removed items where the change index is always '-1'.
    /// <para/>This method raises the <see cref="PropertyChanged"/> event for the <see cref="Count"/> property.</remarks>
    /// <param name="match">A delegate that defines the conditions of the elements to remove. Cannot be null.</param>
    /// <returns>The number of elements removed from the collection.</returns>
    public virtual int RemoveWhere(Predicate<TItem> match)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(match);

        var oldState = new HashSet<TItem>(Items, Comparer);
        int removedCount = Items.RemoveWhere(match);
        PublishDelta(oldState, DeltaType.Remove);

        return removedCount;
    }

    /// <summary>
    /// Removes all objects from the <see cref="ObservableHashSet{TItem}"/>.
    /// </summary>
    /// <remarks>Use this method to clear the set. This method raises the <see cref="CollectionChanged"/> event with <see cref="NotifyCollectionChangedAction.Reset"/> action.
    /// <para/>This method raises the <see cref="PropertyChanged"/> event for the <see cref="Count"/> property.</remarks>
    public void Clear()
    {
        if (Count > 0)
        {
            Items.Clear();
            OnCountChanged();
            OnCollectionChangedReset();
        }
    }

    /// <summary>
    /// Determines whether an element is in the <see cref="ObservableHashSet{T}"/>.
    /// </summary>
    /// <param name="item">The object to locate in the <see cref="ObservableHashSet{T}"/>. The value can be <see langword="null"/> for reference types.</param>
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
    public static IEqualityComparer<ObservableHashSet<TItem>> CreateSetComparer() => new ObservableHashSetEqualityComparer<TItem>();
    /// <summary>
    /// Returns an array containing the elements of the queue in the order they would be dequeued.
    /// </summary>
    /// <returns>An array of type TItem containing all elements in the queue. The array will be empty if the queue contains no
    /// elements.</returns>
    public TItem[] ToArray() => Items.ToArray();
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
    /// are ignored. The operation modifies the current set and does not return a value.
    /// <para/>This method raises the <see cref="CollectionChanged"/> event with <see cref="NotifyCollectionChangedAction.Add"/> or <see cref="NotifyCollectionChangedAction.Remove"/> action including the set of removed and added items where the change index is always '-1'.
    /// <para/>This method raises the <see cref="PropertyChanged"/> event for the <see cref="Count"/> property.</remarks>
    /// <param name="other">The collection of elements to remove from the set. Cannot be <see langword="null"/>.</param>
    public void ExceptWith(IEnumerable<TItem> other)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(other);

        var oldState = new HashSet<TItem>(Items, Comparer);
        Items.ExceptWith(other);

        PublishDelta(oldState);
    }

    /// <summary>
    /// Creates an alternate lookup structure for items in the set using the specified alternate type.
    /// </summary>
    /// <remarks>Use this method to efficiently perform lookups based on a different key or representation of
    /// the items. The alternate lookup is valid only within the scope of the <see langword="ref"/> <see langword="struct"/> and cannot be stored or used
    /// outside its lifetime.</remarks>
    /// <typeparam name="TAlternate">The alternate type used for lookup. Must be a <see langword="ref"/> <see langword="struct"/>.</typeparam>
    /// <returns>An alternate lookup object that enables searching for items using the specified alternate type.</returns>
    public HashSet<TItem>.AlternateLookup<TAlternate> GetAlternateLookup<TAlternate>() where TAlternate : allows ref struct => Items.GetAlternateLookup<TAlternate>();

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the items in the collection.</returns>
    public Enumerator GetEnumerator() => new(Items);
    /// <summary>
    /// Modifies the current set to contain only elements that are also in the specified collection.
    /// </summary>
    /// <remarks>This method removes any elements from the current set that are not present in the specified
    /// collection. The operation does not preserve the order of elements.
    /// <para/>This method raises the <see cref="CollectionChanged"/> event with <see cref="NotifyCollectionChangedAction.Add"/> or <see cref="NotifyCollectionChangedAction.Remove"/> action including the set of removed and added items where the change index is always '-1'.
    /// <para/>This method raises the <see cref="PropertyChanged"/> event for the <see cref="Count"/> property.</remarks>
    /// <param name="other">The collection to compare to the current set. Cannot be null.</param>
    public void IntersectWith(IEnumerable<TItem> other)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(other);

        var oldState = new HashSet<TItem>(Items, Comparer);
        Items.IntersectWith(other);

        PublishDelta(oldState);
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
    /// collection, but not both.
    /// </summary>
    /// <remarks>The symmetric difference operation removes elements that appear in both the current set and
    /// the specified collection, and adds elements that appear in either set but not both. If the specified collection
    /// contains duplicate elements, only unique elements are considered. This method does not return a value; it
    /// modifies the current set in place.
    /// <para/>This method raises the <see cref="CollectionChanged"/> event with <see cref="NotifyCollectionChangedAction.Add"/> or <see cref="NotifyCollectionChangedAction.Remove"/> action including the set of removed and added items where the change index is always '-1'.
    /// <para/>This method raises the <see cref="PropertyChanged"/> event for the <see cref="Count"/> property.</remarks>
    /// <param name="other">The collection whose symmetric difference with the current set is to be computed. Cannot be <see langword="null"/>.</param>
    public void SymmetricExceptWith(IEnumerable<TItem> other)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(other);

        var oldState = new HashSet<TItem>(Items, Comparer);
        Items.SymmetricExceptWith(other);

        PublishDelta(oldState);
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
    /// <para/>This method raises the <see cref="CollectionChanged"/> event with <see cref="NotifyCollectionChangedAction.Add"/> or <see cref="NotifyCollectionChangedAction.Remove"/> action including the set of removed and added items where the change index is always '-1'.
    /// <para/>This method raises the <see cref="PropertyChanged"/> event for the <see cref="Count"/> property.</remarks>
    /// <param name="other">The collection whose elements are to be added to the set. Cannot be <see langword="null"/>.</param>
    public void UnionWith(IEnumerable<TItem> other)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(other);

        var oldState = new HashSet<TItem>(Items, Comparer);
        Items.UnionWith(other);
        PublishDelta(oldState);
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

    private void PublishDelta(HashSet<TItem> oldState, DeltaType deltaType = DeltaType.AddAndRemove)
    {
        HashSetDelta hashSetDelta = GetDelta(oldState, deltaType);
        if (!hashSetDelta.HasChanges)
        {
            return;
        }

        if (Items.Count != oldState.Count)
        {
            OnCountChanged();
        }

        if ((deltaType & DeltaType.Remove) == DeltaType.Remove && hashSetDelta.RemovedItems.Count > 0)
        {
            OnCollectionChanged(NotifyCollectionChangedAction.Remove, hashSetDelta.RemovedItems);
        }

        if ((deltaType & DeltaType.Add) == DeltaType.Add && hashSetDelta.AddedItems.Count > 0)
        {
            OnCollectionChanged(NotifyCollectionChangedAction.Add, hashSetDelta.AddedItems);
        }
    }

    private HashSetDelta GetDelta(HashSet<TItem> oldState, DeltaType deltaType = DeltaType.AddAndRemove)
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

        bool hasChanges = removedItems.Any() || addedItems.Any();
        return new(removedItems, addedItems, hasChanges);
    }

    private void OnCollectionChanged(NotifyCollectionChangedAction action, TItem item)
        => CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, item));

    private void OnCollectionChanged(NotifyCollectionChangedAction action, IList changedItems)
        => CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, changedItems));

    private void OnCollectionChangedReset() => CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

    private void OnCountChanged() => OnPropertyChanged(nameof(Count));

    private void OnCapacityChanged() => OnPropertyChanged(nameof(Capacity));
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    /// <summary>
    /// Adds an item to the collection and raises change notifications if the collection is modified.
    /// </summary>
    /// <remarks>This method triggers collection change and count change notifications only if the item is
    /// successfully added. If the item already exists in the collection, no notifications are raised.
    /// <para/>This method raises the <see cref="CollectionChanged"/> event with <see cref="NotifyCollectionChangedAction.Add"/> action where the change index is always '-1'.
    /// <para/>This method raises the <see cref="PropertyChanged"/> event for the <see cref="Count"/> property.</remarks>
    /// <param name="item">The item to add to the collection. Cannot be null if the collection does not accept null values.</param>
    void ICollection<TItem>.Add(TItem item) => Add(item);

    public readonly struct Enumerator : IEnumerator<TItem>
    {
        private readonly HashSet<TItem>.Enumerator _enumerator;
        internal Enumerator(HashSet<TItem> hashSet)
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

    internal sealed class ObservableHashSetEqualityComparer<TItem> : IEqualityComparer<ObservableHashSet<TItem>?>, IEqualityComparer<HashSet<TItem>?>
    {
        public bool Equals(ObservableHashSet<TItem>? x, ObservableHashSet<TItem>? y)
        {
            // If they're the exact same instance, they're equal.
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            // They're not both null, so if either is null, they're not equal.
            if (x == null || y == null)
            {
                return false;
            }

            return HashSet<TItem>.CreateSetComparer().Equals(x.Items, y.Items);
        }

        public bool Equals(HashSet<TItem>? x, HashSet<TItem>? y)
        {
            // If they're the exact same instance, they're equal.
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            // They're not both null, so if either is null, they're not equal.
            if (x == null || y == null)
            {
                return false;
            }

            return HashSet<TItem>.CreateSetComparer().Equals(x, y);
        }

        public bool Equals(ObservableHashSet<TItem>? x, HashSet<TItem>? y)
        {
            // If they're the exact same instance, they're equal.
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            // They're not both null, so if either is null, they're not equal.
            if (x == null || y == null)
            {
                return false;
            }

            return HashSet<TItem>.CreateSetComparer().Equals(x.Items, y);
        }

        public bool Equals(HashSet<TItem>? x, ObservableHashSet<TItem>? y)
        {
            // If they're the exact same instance, they're equal.
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            // They're not both null, so if either is null, they're not equal.
            if (x == null || y == null)
            {
                return false;
            }

            return HashSet<TItem>.CreateSetComparer().Equals(x, y.Items);
        }

        public int GetHashCode(ObservableHashSet<TItem>? obj)
        {
            int hashCode = 0; // default to 0 for null/empty set

            if (obj != null)
            {
                foreach (TItem t in obj)
                {
                    if (t != null)
                    {
                        hashCode ^= t.GetHashCode(); // same hashcode as default comparer
                    }
                }
            }

            return hashCode;
        }

        public int GetHashCode(HashSet<TItem>? obj)
        {
            int hashCode = 0; // default to 0 for null/empty set

            if (obj != null)
            {
                foreach (TItem t in obj)
                {
                    if (t != null)
                    {
                        hashCode ^= t.GetHashCode(); // same hashcode as default comparer
                    }
                }
            }

            return hashCode;
        }

        // Equals method for the comparer itself.
        public override bool Equals([NotNullWhen(true)] object? obj) => obj is ObservableHashSetEqualityComparer<TItem>;

        public override int GetHashCode() => EqualityComparer<TItem>.Default.GetHashCode();
    }

    internal readonly struct HashSetDelta
    {
        public HashSetDelta(IList removedItems, IList addedItems, bool hasChanges)
        {
            RemovedItems = removedItems;
            AddedItems = addedItems;
            HasChanges = hasChanges;
        }

        public IList RemovedItems { get; }
        public IList AddedItems { get; }
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
    public ObservableFileSystemPathHashSet() : base(FileSystemPathEqualityComparer.Default) { }
    public ObservableFileSystemPathHashSet(IEnumerable<string> collection) : base(collection, FileSystemPathEqualityComparer.Default) { }

    public ObservableFileSystemPathHashSet(int capacity) : base(capacity, FileSystemPathEqualityComparer.Default)
    {
    }

    public ObservableFileSystemPathHashSet(IEqualityComparer<string>? comparer) : base(comparer)
    {
    }

    public ObservableFileSystemPathHashSet(IEnumerable<string> collection, IEqualityComparer<string>? comparer) : base(collection, comparer)
    {
    }

    public ObservableFileSystemPathHashSet(int capacity, IEqualityComparer<string>? comparer) : base(capacity, comparer)
    {
    }
    /// <summary>
    /// The equality comparer used to determine equality of items in the set. This comparer is used for all operations that involve comparing items, such as adding, removing, and checking for the presence of items in the set.
    /// </summary>
    /// <value>The equality <see cref="IEqualityComparer"/>&lt;<see langword="string"/>&gt; used by the set. The default is <see cref="StringComparer.OrdinalIgnoreCase"/>.</value>
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
    /// <remarks>Each item is represented as a <see cref="FileSystemInfo"/> object, which may be a <see cref="FileInfo"/> or <see cref="DirectoryInfo"/>
    /// depending on whether the item has a file extension. The method removes only those items for which the predicate
    /// returns <see langword="true"/>.</remarks>
    /// <param name="match">A delegate that defines the conditions of the items to remove. Cannot be <see langword="null"/>.</param>
    /// <returns>The number of items removed from the collection.</returns>
    public int RemoveWhere(Predicate<FileSystemInfo> match)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(match);

        int removedCount = 0;
        foreach (string item in Items)
        {
            FileSystemInfo fileSystemInfo = ConvertToFileSystemInfo(item);
            if (match(fileSystemInfo) && Remove(item))
            {
                removedCount++;
            }
        }

        return removedCount;
    }

    public bool TryGetValue(FileSystemInfo item, [MaybeNullWhen(false)] out FileSystemInfo value)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(item);

        value = default;
        if (Items.TryGetValue(item.FullName, out string stringValue))
        {
            value = ConvertToFileSystemInfo(stringValue);

            return true;
        }

        return false;
    }

    public FileSystemInfo[] ToFileSystemInfoArray() => Items.Select(item =>
    {
        FileSystemInfo fileSystemInfo = ConvertToFileSystemInfo(item);
        return fileSystemInfo;
    }).ToArray();

    /// <summary>
    /// Creates an equality comparer that can be used to compare two hash sets for set equality.
    /// </summary>
    /// <remarks>The returned comparer considers two hash sets equal if they have the same elements, even if
    /// the order differs. This is useful for scenarios where set semantics are required, such as using hash sets as
    /// keys in dictionaries.</remarks>
    /// <returns>An equality comparer that determines whether two hash sets contain the same elements, regardless of order.</returns>
    public static new IEqualityComparer<ObservableFileSystemPathHashSet> CreateSetComparer() => new ObservableFileSystemPathHashSetEqualityComparer();

    /// <summary>
    /// Removes all elements in the specified collection from the current set.
    /// </summary>
    /// <remarks>If the specified collection contains elements that are not present in the set, those elements
    /// are ignored. The operation modifies the current set and does not return a value.
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
    /// contains duplicate elements, only unique elements are considered. This method does not return a value; it
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

    private static FileSystemInfo ConvertToFileSystemInfo(string item) => Path.HasExtension(item)
        ? new FileInfo(item)
        : new DirectoryInfo(item);
}

public sealed class FileSystemPathEqualityComparer : EqualityComparer<string>, IEqualityComparer<FileSystemInfo>
{
    public static new FileSystemPathEqualityComparer Default { get; } = new();
    private readonly IEqualityComparer<string> _stringComparer;

    private FileSystemPathEqualityComparer() => _stringComparer = OperatingSystem.IsLinux()
        ? StringComparer.Ordinal
        : StringComparer.OrdinalIgnoreCase;

    public FileSystemPathEqualityComparer(IEqualityComparer<string> comparer)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(comparer);
        _stringComparer = comparer;
    }

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

        return _stringComparer.Equals(xNormalized, yNormalized);
    }

    public bool Equals(FileSystemInfo? x, FileSystemInfo? y)
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

        return _stringComparer.Equals(xNormalized, yNormalized);
    }

    private string NormalizeFileSystemPath(string fileSystemPath)
    {
        // Normalize the path by removing leading/trailing whitespace
        // and any trailing directory separators. This helps ensure that paths that differ only in these aspects are considered equal.
        string normalizedPath = Path.TrimEndingDirectorySeparator(fileSystemPath.Trim());
        return normalizedPath;
    }

    public override int GetHashCode(string obj) => obj is string fileSystemPath
        ? _stringComparer.GetHashCode(NormalizeFileSystemPath(fileSystemPath))
        : 0;

    public int GetHashCode([DisallowNull] FileSystemInfo obj) => obj is FileSystemInfo fileSystemInfo
        ? _stringComparer.GetHashCode(NormalizeFileSystemPath(fileSystemInfo.FullName))
        : 0;
}

internal sealed class ObservableFileSystemPathHashSetEqualityComparer : IEqualityComparer<ObservableFileSystemPathHashSet?>, IEqualityComparer<HashSet<string>?>, IEqualityComparer<ObservableHashSet<string>?>, IEqualityComparer<HashSet<FileSystemInfo>?>, IEqualityComparer<ObservableHashSet<FileSystemInfo>?>
{
    public bool Equals(ObservableFileSystemPathHashSet? x, ObservableFileSystemPathHashSet? y)
    {
        IEqualityComparer<string> comparer = x?.Comparer ?? y?.Comparer ?? FileSystemPathEqualityComparer.Default;
        return IsSetEqual(x, y, comparer);
    }

    /// <summary>
    /// Determines whether two <see cref="ObservableHashSet"/>&lt;<see langword="string"/>&gt; instances are equal by comparing their contents using file
    /// system path equality semantics.
    /// </summary>
    /// <remarks>Equality is determined using file system path comparison for string elements. If both
    /// parameters are <see langword="null"/>, they are considered equal. If only one is <see langword="null"/>, they are not equal. Specialized comparison
    /// is performed if either set is an <see cref="ObservableFileSystemPathHashSet"/> using their provided comparer where <paramref name="x"/> takes precedence over <paramref name="y"/>. Otherwise, the default equality is determined by using the <see cref="FileSystemPathEqualityComparer.Default"/> equality comparer.</remarks>
    /// <param name="x">The first <see cref="ObservableHashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
    /// <param name="y">The second <see cref="ObservableHashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if both sets contain the same elements according to file system path equality; otherwise, <see langword="false"/>.</returns>
    public bool Equals(ObservableHashSet<string>? x, ObservableHashSet<string>? y)
    {
        if (x is ObservableFileSystemPathHashSet xPathSet)
        {
            if (y is ObservableFileSystemPathHashSet yPathSet)
            {
                return Equals(xPathSet, yPathSet);
            }
            else
            {
                return Equals(xPathSet, y);
            }
        }
        else if (y is ObservableFileSystemPathHashSet yPathSet)
        {
            return Equals(x, yPathSet);
        }

        IEqualityComparer<string> comparer = FileSystemPathEqualityComparer.Default;
        return IsSetEqual(x, y, comparer);
    }

    /// <summary>
    /// Determines whether two <see cref="HashSet"/>&lt;<see langword="string"/>&gt; instances are equal by comparing their contents using file
    /// system path equality semantics.
    /// </summary>
    /// <remarks>Equality is determined using file system path comparison for string elements. If both
    /// parameters are <see langword="null"/>, they are considered equal. If only one is <see langword="null"/>, they are not equal. The equality is determined by using the <see cref="FileSystemPathEqualityComparer.Default"/> equality comparer.</remarks>
    /// <param name="x">The first <see cref="HashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
    /// <param name="y">The second <see cref="HashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if both sets contain the same elements according to file system path equality; otherwise, <see langword="false"/>.</returns>
    public bool Equals(HashSet<string>? x, HashSet<string>? y)
    {
        IEqualityComparer<string> comparer = FileSystemPathEqualityComparer.Default;
        return IsSetEqual(x, y, comparer);
    }

    /// <summary>
    /// Determines whether a <see cref="ObservableHashSet"/>&lt;<see langword="string"/>&gt; equals a <see cref="HashSet"/>&lt;<see langword="string"/>&gt; instances are equal by comparing their contents using file
    /// system path equality semantics.
    /// </summary>
    /// <remarks>Equality is determined using file system path comparison for string elements. If both
    /// parameters are <see langword="null"/>, they are considered equal. If only one is <see langword="null"/>, they are not equal. Specialized comparison
    /// is performed if the <paramref name="x"/> set is an <see cref="ObservableFileSystemPathHashSet"/> using its provided comparer. Otherwise, the default equality is determined by using the <see cref="FileSystemPathEqualityComparer.Default"/> equality comparer.</remarks>
    /// <param name="x">The first <see cref="ObservableHashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
    /// <param name="y">The second <see cref="HashSet"/>&lt;<see langword="string"/>&gt; to compare. Can be <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if both sets contain the same elements according to file system path equality; otherwise, <see langword="false"/>.</returns>
    public bool Equals(ObservableHashSet<string>? x, HashSet<string>? y)
    {
        if (x is ObservableFileSystemPathHashSet xPathSet)
        {
            return Equals(xPathSet, y);
        }

        IEqualityComparer<string> comparer = FileSystemPathEqualityComparer.Default;
        return IsSetEqual(x, y, comparer);
    }

    public bool Equals(ObservableHashSet<string>? x, ObservableFileSystemPathHashSet? y)
    {
        if (x is ObservableFileSystemPathHashSet xPathSet)
        {
            return Equals(xPathSet, y);
        }

        IEqualityComparer<string> comparer = y?.Comparer ?? FileSystemPathEqualityComparer.Default;
        return IsSetEqual(x, y, comparer);
    }

    public bool Equals(ObservableFileSystemPathHashSet? x, HashSet<string>? y)
    {
        IEqualityComparer<string> comparer = x?.Comparer ?? FileSystemPathEqualityComparer.Default;
        return IsSetEqual(x, y, comparer);
    }

    public bool Equals(ObservableFileSystemPathHashSet? x, ObservableHashSet<string>? y)
    {
        IEqualityComparer<string> comparer = x?.Comparer ?? FileSystemPathEqualityComparer.Default;
        return IsSetEqual(x, y, comparer);
    }

    public bool Equals(HashSet<string>? x, ObservableFileSystemPathHashSet? y)
    {
        IEqualityComparer<string> comparer = y?.Comparer ?? FileSystemPathEqualityComparer.Default;
        return IsSetEqual(x, y, comparer);
    }

    public bool Equals(HashSet<string>? x, ObservableHashSet<string>? y)
    {
        if (y is ObservableFileSystemPathHashSet yPathSet)
        {
            return Equals(x, yPathSet);
        }

        IEqualityComparer<string> comparer = FileSystemPathEqualityComparer.Default;
        return IsSetEqual(x, y, comparer);
    }

    public bool Equals(HashSet<FileSystemInfo>? x, HashSet<FileSystemInfo>? y)
    {
        IEqualityComparer<FileSystemInfo> comparer = FileSystemPathEqualityComparer.Default;
        return IsSetEqual(x, y, comparer);
    }

    public bool Equals(ObservableHashSet<FileSystemInfo>? x, ObservableHashSet<FileSystemInfo>? y)
    {
        IEqualityComparer<FileSystemInfo> comparer = FileSystemPathEqualityComparer.Default;
        return IsSetEqual(x, y, comparer);
    }

    public bool Equals(ObservableFileSystemPathHashSet? x, HashSet<FileSystemInfo>? y)
    {
        IEqualityComparer<string> comparer = x?.Comparer ?? FileSystemPathEqualityComparer.Default;
        return IsSetEqual(x, y, comparer);
    }

    public bool Equals(ObservableFileSystemPathHashSet? x, ObservableHashSet<FileSystemInfo>? y)
    {
        IEqualityComparer<string> comparer = x?.Comparer ?? FileSystemPathEqualityComparer.Default;
        return IsSetEqual(x, y, comparer);
    }

    public bool Equals(ObservableHashSet<FileSystemInfo>? x, ObservableFileSystemPathHashSet? y)
    {
        IEqualityComparer<string> comparer = y?.Comparer ?? FileSystemPathEqualityComparer.Default;
        return IsSetEqual(y, x, comparer);
    }

    public bool Equals(HashSet<FileSystemInfo>? x, ObservableFileSystemPathHashSet? y)
    {
        IEqualityComparer<string> comparer = y?.Comparer ?? FileSystemPathEqualityComparer.Default;
        return IsSetEqual(y, x, comparer);
    }

    public int GetHashCode([DisallowNull] HashSet<FileSystemInfo>? obj)
    {
        int hashCode = 0; // default to 0 for null/empty set

        if (obj != null)
        {
            foreach (FileSystemInfo t in obj)
            {
                if (t != null)
                {
                    hashCode ^= t.GetHashCode(); // same hashcode as default comparer
                }
            }
        }

        return hashCode;
    }

    public int GetHashCode([DisallowNull] ObservableHashSet<FileSystemInfo>? obj)
    {
        int hashCode = 0; // default to 0 for null/empty set

        if (obj != null)
        {
            foreach (FileSystemInfo t in obj)
            {
                if (t != null)
                {
                    hashCode ^= t.GetHashCode(); // same hashcode as default comparer
                }
            }
        }

        return hashCode;
    }

    public int GetHashCode(ObservableFileSystemPathHashSet? obj)
    {
        int hashCode = 0; // default to 0 for null/empty set

        if (obj != null)
        {
            foreach (string t in obj)
            {
                if (t != null)
                {
                    hashCode ^= t.GetHashCode(); // same hashcode as default comparer
                }
            }
        }

        return hashCode;
    }

    public int GetHashCode(ObservableHashSet<string>? obj)
    {
        int hashCode = 0; // default to 0 for null/empty set

        if (obj != null)
        {
            foreach (string t in obj)
            {
                if (t != null)
                {
                    hashCode ^= t.GetHashCode(); // same hashcode as default comparer
                }
            }
        }

        return hashCode;
    }

    public int GetHashCode(HashSet<string>? obj)
    {
        int hashCode = 0; // default to 0 for null/empty set

        if (obj != null)
        {
            foreach (string t in obj)
            {
                if (t != null)
                {
                    hashCode ^= t.GetHashCode(); // same hashcode as default comparer
                }
            }
        }

        return hashCode;
    }

    // Equals method for the comparer itself.
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is ObservableFileSystemPathHashSetEqualityComparer;

    private static bool IsSetEqual(ISet<string>? x, ISet<string>? y, IEqualityComparer<string> comparer)
    {
        // If they're the exact same instance, they're equal.
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        // They're not both null, so if either is null, they're not equal.
        if (x == null || y == null)
        {
            return false;
        }

        if (x.Count != y.Count)
        {
            return false;
        }

        foreach (string item in x)
        {
            if (!y.Contains(item, comparer))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsSetEqual(ISet<FileSystemInfo>? x, ISet<FileSystemInfo>? y, IEqualityComparer<FileSystemInfo> comparer)
    {
        // If they're the exact same instance, they're equal.
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        // They're not both null, so if either is null, they're not equal.
        if (x == null || y == null)
        {
            return false;
        }

        if (x.Count != y.Count)
        {
            return false;
        }

        foreach (FileSystemInfo item in x)
        {
            if (!y.Contains(item, comparer))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsSetEqual(ISet<string>? x, ISet<FileSystemInfo>? y, IEqualityComparer<string> comparer)
    {
        // They're not both null, so if either is null, they're not equal.
        if (x == null || y == null)
        {
            return false;
        }

        if (x.Count != y.Count)
        {
            return false;
        }

        foreach (FileSystemInfo item in y)
        {
            if (!x.Contains(item.FullName, comparer))
            {
                return false;
            }
        }

        return true;
    }

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }
}