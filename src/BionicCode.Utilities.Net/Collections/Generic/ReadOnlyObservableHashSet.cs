namespace BionicCode.Utilities.Net;

using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Provides a read-only wrapper around an <see cref="ObservableHashSet{TItem}"/>.
/// </summary>
/// <remarks>The wrapper exposes non-mutating set operations while forwarding <see cref="INotifyCollectionChanged.CollectionChanged"/> and <see cref="INotifyPropertyChanged.PropertyChanged"/> notifications from the wrapped set.</remarks>
/// <typeparam name="TItem">The type of elements contained in the set.</typeparam>
public class ReadOnlyObservableHashSet<TItem> : IReadOnlySet<TItem>, INotifyCollectionChanged, INotifyPropertyChanged
{
    private readonly ObservableHashSet<TItem> _set;

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc/>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadOnlyObservableHashSet{TItem}"/> class.
    /// </summary>
    /// <param name="set">The observable set to wrap.</param>
    public ReadOnlyObservableHashSet(ObservableHashSet<TItem> set)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(set);

        _set = set;
        _ = _set.EnableDataBindingSupport();
        _set.CollectionChanged += HandleCollectionChanged;
        _set.PropertyChanged += HandlePropertyChanged;
    }

    /// <summary>
    /// Gets the wrapped observable set.
    /// </summary>
    /// <remarks>Derived types can use this property to customize the read-only view behavior while preserving access to the wrapped collection.</remarks>
    protected ObservableHashSet<TItem> Items => _set;

    /// <summary>
    /// Gets the current capacity of the wrapped set.
    /// </summary>
    public int Capacity => _set.Capacity;

    /// <summary>
    /// Gets the equality comparer that is used to determine equality of items in the wrapped set.
    /// </summary>
    public IEqualityComparer<TItem> Comparer => _set.Comparer;

    /// <summary>
    /// Gets the number of items contained in the wrapped set.
    /// </summary>
    public int Count => _set.Count;

    /// <summary>
    /// Determines whether an element is in the wrapped set.
    /// </summary>
    /// <param name="item">The object to locate in the wrapped set.</param>
    /// <returns><see langword="true"/> if the wrapped set contains the specified element; otherwise, <see langword="false"/>.</returns>
    public bool Contains(TItem item) => _set.Contains(item);

    /// <summary>
    /// Copies the elements of the wrapped set to the specified array, starting at the specified index.
    /// </summary>
    /// <param name="array">The destination array that receives the copied elements.</param>
    /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
    public void CopyTo(TItem[] array, int arrayIndex) => _set.CopyTo(array, arrayIndex);

    /// <summary>
    /// Returns an enumerator that iterates through the wrapped set.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the wrapped set.</returns>
    public IEnumerator<TItem> GetEnumerator() => ((IEnumerable<TItem>)_set).GetEnumerator();

    /// <summary>
    /// Determines whether the current set is a proper subset of the specified collection.
    /// </summary>
    /// <param name="other">The collection to compare to the wrapped set.</param>
    /// <returns><see langword="true"/> if the wrapped set is a proper subset of <paramref name="other"/>; otherwise, <see langword="false"/>.</returns>
    public bool IsProperSubsetOf(IEnumerable<TItem> other) => _set.IsProperSubsetOf(other);

    /// <summary>
    /// Determines whether the current set is a proper superset of the specified collection.
    /// </summary>
    /// <param name="other">The collection to compare to the wrapped set.</param>
    /// <returns><see langword="true"/> if the wrapped set is a proper superset of <paramref name="other"/>; otherwise, <see langword="false"/>.</returns>
    public bool IsProperSupersetOf(IEnumerable<TItem> other) => _set.IsProperSupersetOf(other);

    /// <summary>
    /// Determines whether the current set is a subset of the specified collection.
    /// </summary>
    /// <param name="other">The collection to compare to the wrapped set.</param>
    /// <returns><see langword="true"/> if the wrapped set is a subset of <paramref name="other"/>; otherwise, <see langword="false"/>.</returns>
    public bool IsSubsetOf(IEnumerable<TItem> other) => _set.IsSubsetOf(other);

    /// <summary>
    /// Determines whether the current set is a superset of the specified collection.
    /// </summary>
    /// <param name="other">The collection to compare to the wrapped set.</param>
    /// <returns><see langword="true"/> if the wrapped set is a superset of <paramref name="other"/>; otherwise, <see langword="false"/>.</returns>
    public bool IsSupersetOf(IEnumerable<TItem> other) => _set.IsSupersetOf(other);

    /// <summary>
    /// Determines whether the wrapped set and the specified collection share any common elements.
    /// </summary>
    /// <param name="other">The collection to compare to the wrapped set.</param>
    /// <returns><see langword="true"/> if the wrapped set and <paramref name="other"/> share at least one common element; otherwise, <see langword="false"/>.</returns>
    public bool Overlaps(IEnumerable<TItem> other) => _set.Overlaps(other);

    /// <summary>
    /// Determines whether the wrapped set and the specified collection contain the same elements.
    /// </summary>
    /// <param name="other">The collection to compare to the wrapped set.</param>
    /// <returns><see langword="true"/> if the wrapped set and <paramref name="other"/> contain the same elements; otherwise, <see langword="false"/>.</returns>
    public bool SetEquals(IEnumerable<TItem> other) => _set.SetEquals(other);

    /// <summary>
    /// Returns an array containing the elements of the wrapped set in enumeration order.
    /// </summary>
    /// <returns>An array that contains all elements of the wrapped set.</returns>
    public TItem[] ToArray() => _set.ToArray();

    /// <summary>
    /// Attempts to retrieve the actual stored value that is equal to the specified value.
    /// </summary>
    /// <param name="equalValue">The value to search for.</param>
    /// <param name="actualValue">When this method returns <see langword="true"/>, contains the value from the wrapped set that is equal to <paramref name="equalValue"/>; otherwise, the default value for <typeparamref name="TItem"/>.</param>
    /// <returns><see langword="true"/> if an equal value was found; otherwise, <see langword="false"/>.</returns>
    public bool TryGetValue(TItem equalValue, [MaybeNullWhen(false)] out TItem actualValue) => _set.TryGetValue(equalValue, out actualValue);

    /// <summary>
    /// Raises the <see cref="CollectionChanged"/> event.
    /// </summary>
    /// <param name="e">The event data.</param>
    protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e) => CollectionChanged?.Invoke(this, e);

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event.
    /// </summary>
    /// <param name="e">The event data.</param>
    protected virtual void OnPropertyChanged(PropertyChangedEventArgs e) => PropertyChanged?.Invoke(this, e);

    /// <summary>
    /// Returns a non-generic enumerator that iterates through the wrapped set.
    /// </summary>
    /// <returns>A non-generic enumerator for the wrapped set.</returns>
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_set).GetEnumerator();

    private void HandleCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => OnCollectionChanged(e);

    private void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e) => OnPropertyChanged(e);
}