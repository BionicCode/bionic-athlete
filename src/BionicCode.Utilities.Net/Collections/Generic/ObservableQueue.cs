namespace BionicCode.Utilities.Net;

using System.Collections;

#region Info

// 2020/11/17  14:50
// Net.Wpf

#endregion

#region Usings

using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
#endregion

/// <summary>
/// A <see cref="Queue{T}"/> that implements <see cref="INotifyCollectionChanged"/> and <see cref="INotifyPropertyChanged"/>.
/// <br/> Since the behavior itself is not changed, see <see cref="Queue{T}"/> for a detailed API documentation.
/// </summary>
/// <typeparam name="TItem"></typeparam>
public sealed class ObservableQueue<TItem> : IEnumerable<TItem>, IReadOnlyCollection<TItem>, IEnumerable, ICollection, INotifyCollectionChanged, INotifyPropertyChanged
{
    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;
    /// <inheritdoc/>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;
    private readonly Queue<TItem> _queue;

    public ObservableQueue() => _queue = new Queue<TItem>();

    public ObservableQueue(IEnumerable<TItem> collection)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(collection);
        _queue = new Queue<TItem>(collection);
    }

    public ObservableQueue(int capacity)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(capacity);
        _queue = new Queue<TItem>(capacity);
    }

    public int Capacity => _queue.Capacity;

    public int Count => ((IReadOnlyCollection<TItem>)_queue).Count;

    int ICollection.Count => Count;
    bool ICollection.IsSynchronized => ((ICollection)_queue).IsSynchronized;
    object ICollection.SyncRoot => ((ICollection)_queue).SyncRoot;

    /// <summary>
    /// Adds an object to the end of the <see cref="Queue{T}"/>.
    /// </summary>
    /// <param name="item">The object to add to the <see cref="Queue{T}"/>. The value can be <c>null</c> for reference types.</param>
    /// <remarks>Use this method to add an item to the end of the queue. The item will be the last one to be dequeued.
    /// <para/>This method raises the <see cref="CollectionChanged"/> event with <see cref="NotifyCollectionChangedAction.Add"/> action where the change index is always '-1'.
    /// <para/>This method raises the <see cref="PropertyChanged"/> event for the <see cref="Count"/> property.</remarks>
    public void Enqueue(TItem item)
    {
        _queue.Enqueue(item);
        OnCountChanged();
        OnCollectionChanged(NotifyCollectionChangedAction.Add, item);
    }

    /// <summary>
    /// Removes and returns the object at the beginning of the <see cref="Queue{T}"/>.
    /// </summary>
    /// <returns>The object removed from the beginning of the <see cref="Queue{T}"/>.</returns>
    /// <remarks>Use this method to remove and return the item at the front of the queue. The item will be the first one to be dequeued.
    /// <para/>This method raises the <see cref="CollectionChanged"/> event with <see cref="NotifyCollectionChangedAction.Remove"/> action where the change index is always '-1'.
    /// <para/>This method raises the <see cref="PropertyChanged"/> event for the <see cref="Count"/> property.</remarks>
    public TItem Dequeue()
    {
        TItem removedItem = _queue.Dequeue();
        OnCountChanged();
        OnCollectionChanged(NotifyCollectionChangedAction.Remove, removedItem);
        return removedItem;
    }

    /// <summary>
    /// Removes the object at the beginning of the <see cref="Queue{T}"/>, and copies it to the result parameter.
    /// </summary>
    /// <param name="result">The removed object.</param>
    /// <returns><see langword="true"/> if the object is successfully removed; <see langword="false"/> if the <see cref="Queue{T}"/> is empty.</returns>
    /// <remarks>Use this method to attempt to remove and return the item at the front of the queue without throwing an exception if the queue is empty.
    /// <para/>This method raises the <see cref="CollectionChanged"/> event with <see cref="NotifyCollectionChangedAction.Remove"/> action where the change index is always '-1'.
    /// <para/>This method raises the <see cref="PropertyChanged"/> event for the <see cref="Count"/> property.</remarks>
    public bool TryDequeue([MaybeNullWhen(false)] out TItem result)
    {
        if (_queue.TryDequeue(out result))
        {
            OnCountChanged();
            OnCollectionChanged(NotifyCollectionChangedAction.Remove, result);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Removes all objects from the <see cref="Queue{T}"/>.
    /// </summary>
    /// <remarks>Use this method to clear the queue. This method raises the <see cref="CollectionChanged"/> event with <see cref="NotifyCollectionChangedAction.Reset"/> action.
    /// <para/>This method raises the <see cref="PropertyChanged"/> event for the <see cref="Count"/> property.</remarks>
    public void Clear()
    {
        _queue.Clear();
        OnCountChanged();
        OnCollectionChangedReset();
    }

    /// <summary>
    /// Determines whether an element is in the <see cref="ObservableQueue{T}"/>.
    /// </summary>
    /// <param name="item">The object to locate in the <see cref="ObservableQueue{T}"/>. The value can be <c>null</c> for reference types.</param>
    /// <returns><see langword="true"/> if the <see cref="ObservableQueue{T}"/> contains the specified element; otherwise, <see langword="false"/>.</returns>
    public bool Contains(TItem item) => _queue.Contains(item);
    /// <summary>
    /// Copies the elements of the queue to the specified array, starting at the given array index.
    /// </summary>
    /// <param name="array">The destination array that will receive the elements copied from the queue. Must be large enough to contain all
    /// elements from the specified starting index.</param>
    /// <param name="arrayIndex">The zero-based index in the destination array at which copying begins. Must be within the bounds of the array.</param>
    public void CopyTo(TItem[] array, int arrayIndex) => _queue.CopyTo(array, arrayIndex);
    /// <summary>
    /// Returns the item at the front of the queue without removing it.
    /// </summary>
    /// <remarks>Use this method to inspect the next item to be dequeued without modifying the queue. If the
    /// queue is empty, the returned value may not be meaningful; consider checking the queue's count before calling
    /// this method.</remarks>
    /// <returns>The item at the front of the queue. The value is undefined if the queue is empty.</returns>
    public TItem Peek() => _queue.Peek();
    /// <summary>
    /// Attempts to retrieve the item at the front of the queue without removing it.
    /// </summary>
    /// <param name="result">When this method returns, contains the item at the front of the queue if one is available; otherwise, the value
    /// is <see langword="null"/>.</param>
    /// <returns>true if an item was successfully retrieved; otherwise, false.</returns>
    public bool TryPeek(out TItem? result) => _queue.TryPeek(out result);
    /// <summary>
    /// Returns an array containing the elements of the queue in the order they would be dequeued.
    /// </summary>
    /// <returns>An array of type TItem containing all elements in the queue. The array will be empty if the queue contains no
    /// elements.</returns>
    public TItem[] ToArray() => _queue.ToArray();
    /// <summary>
    /// Reduces the capacity of the queue to match the actual number of elements, minimizing memory overhead.
    /// </summary>
    /// <remarks>Use this method to optimize memory usage after removing a significant number of elements from
    /// the queue. Calling this method may incur a performance cost due to internal array resizing.</remarks>
    public void TrimExcess()
    {
        _queue.TrimExcess();
        OnCapacityChanged();
    }
    /// <summary>
    /// Reduces the memory overhead by adjusting the internal storage to the specified capacity, if possible.
    /// </summary>
    /// <remarks>Use this method to minimize memory usage when the queue is expected to remain at or below the
    /// specified capacity. If the current number of elements exceeds the specified capacity, no trimming
    /// occurs.</remarks>
    /// <param name="capacity">The target capacity for the internal storage after trimming. Must be non-negative.</param>
    public void TrimExcess(int capacity)
    {
        _queue.TrimExcess(capacity);
        OnCapacityChanged();
    }

    /// <summary>
    /// Ensures that the queue can accommodate at least the specified number of elements without resizing.
    /// </summary>
    /// <param name="capacity">The minimum number of elements that the queue should be able to hold. Must be non-negative.</param>
    /// <returns>The new capacity of the queue after ensuring the specified minimum capacity.</returns>
    public int EnsureCapacity(int capacity)
    {
        int newCapacity = _queue.EnsureCapacity(capacity);
        OnCapacityChanged();

        return newCapacity;
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the items in the collection.</returns>
    public Enumerator GetEnumerator() => new(_queue);
    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection.</returns>
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_queue).GetEnumerator();
    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator for the collection of items.</returns>
    IEnumerator<TItem> IEnumerable<TItem>.GetEnumerator() => ((IEnumerable<TItem>)_queue).GetEnumerator();

    private void OnCollectionChanged(NotifyCollectionChangedAction action, TItem item)
        => CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action, item));

    private void OnCollectionChangedReset()
        => CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

    private void OnCountChanged() => OnPropertyChanged(nameof(Count));

    private void OnCapacityChanged() => OnPropertyChanged(nameof(Capacity));
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    void ICollection.CopyTo(Array array, int index) => ((ICollection)_queue).CopyTo(array, index);

    public readonly struct Enumerator : IEnumerator<TItem>
    {
        private readonly Queue<TItem>.Enumerator _enumerator;
        internal Enumerator(Queue<TItem> queue)
        {
            ArgumentNullExceptionAdvanced.ThrowIfNull(queue);
            _enumerator = queue.GetEnumerator();
        }

        public TItem Current => _enumerator.Current;
        object IEnumerator.Current => Current!;
        public void Dispose() => _enumerator.Dispose();
        public bool MoveNext() => _enumerator.MoveNext();
        public void Reset() => throw new NotSupportedException();
    }
}