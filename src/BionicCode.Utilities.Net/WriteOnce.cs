using System.Diagnostics.CodeAnalysis;

/// <summary>
/// A wrapper that decorates and underlying value and acts like the wrapped value by hiding it's own type in e.g. expressions and statements.
/// <br/><see cref="WriteOnce{TValue}"/> acts like a write-once field or property, allowing the underlying value to be set only once and then acts as immutable.
/// </summary>
/// <remarks>
/// <see cref="WriteOnce{TValue}"/> implements a write-once semantics for the underlying value, allowing it to be set only once and then acts as immutable.
/// <para/><see cref="WriteOnce{TValue}"/> additionally acts l ike a normal field or property 
/// in that it can be implicitly cast to and from the underlying value type, allowing it to be used in expressions 
/// and assignments without needing to explicitly access the underlying value.
/// <para/><see cref="WriteOnce{TValue}"/> is thread-safe, ensuring that the underlying value can only be set once even in concurrent scenarios.
/// <para/><see cref="WriteOnce{TValue}"/> implements <see cref="IEquatable{T}"/> and <see cref="IFormattable"/> to allow for equality comparisons and formatted string representations based on the underlying value.
/// <br/>Note: <see cref="object.GetHashCode()"/> and <see cref="object.Equals(object?)"/> are still based on the reference of the current <see cref="WriteOnce{TValue}"/> instance.
/// <para>Typical usage is to wrap e.g. a OnCollectionChanged call with a using() scope or using expression:</para>
/// <code>
/// class Foo
/// {
///     private readonly WriteOnce&lt;bool&gt; _isInitialized;
///     private readonly WriteOnce&lt;string&gt; _name;
///     private readonly WriteOnce&lt;double&gt; _offset;
///     
///     private void Initialize(double offset)
///     {
///         _offset.SetValue(offset);
///         _isInitialized.SetValue(true);
///         
///         // Initializing a WriteOnce&lt;string&gt; with an implicit cast from string literal, 
///         // making it act like a plain string variable.
///         WriteOnce&lt;string&gt; name = "Malcolm X";
///     }
///     
///     private void DoSomething()
///     {
///         // Implicit cast allows treating the WriteOnce&lt;bool&gt; like plain bool field.
///         // WriteOnce&lt;bool&gt; also behaves like a field as that it would return 'default(bool)' 
///         // if not explicitly initialized with a value.
///         if (_isInitialized)
///         {
///             // Implicit cast makes the WriteOnce&lt;string&gt; act like a plain string field.
///             double length = 24.476 + _offset;
///             
///             // Initializing a WriteOnce&lt;string&gt; with an implicit cast from string literal, 
///             // making it act like a plain string variable.
///             WriteOnce&lt;string&gt; text = "Jerry";
///             
///             // If 'TValue' is IFormattable, the WriteOnce&lt;TValue&gt; can be formatted directly 
///             // without needing to access the underlying value.
///             Console.WriteLine($"Name: {_name}, Length: {length:F2}");
///         }
///     }
/// }
/// </code>
/// </remarks>
[SuppressMessage("Design", "CA1067:Override Object.Equals(object) when implementing IEquatable<T>", Justification = "'WriteOnce<TValue> is a wrapper type, more like a decorator and deliberately separates value equality from reference equality since it is technically not immutable.'")]
public sealed class WriteOnce<TValue> : IFormattable
{
    private TValue _value = default!;
    private int _isSet;
    private readonly object _syncLock = new();

    public bool TrySetValue(TValue value)
    {
        if (Volatile.Read(ref _isSet) != 0)
        {
            return false;
        }

        lock (_syncLock)
        {
            if (_isSet != 0)
            {
                return false;
            }

            _value = value;
            Volatile.Write(ref _isSet, 1);
            return true;
        }
    }

    public void SetValue(TValue value)
    {
        if (!TrySetValue(value))
        {
            throw new InvalidOperationException("Value has already been initialized and cannot be modified.");
        }
    }

    [SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "Required.")]
    public TValue GetValueOrDefault() => Volatile.Read(ref _isSet) != 0 ? _value : default!;

    public bool IsSet => Volatile.Read(ref _isSet) != 0;
    public bool IsSealed => IsSet;

    public static implicit operator TValue(WriteOnce<TValue>? source) => source is null
        ? default!
        : source.GetValueOrDefault();

    /// <summary>
    /// Returns a string representation of the current underlying value, or an empty string if no value is present.
    /// </summary>
    /// <returns>A string that represents the current underlying value if present; otherwise, an empty string.</returns>
    public override string ToString() => GetValueOrDefault() is TValue value
        ? value.ToString() ?? string.Empty
        : string.Empty;

    /// <summary>
    /// Performs an equality comparison between this instance and another <see cref="WriteOnce{TValue}"/> instance based on the underlying values AND NOT on their references.
    /// </summary>
    /// <remarks>Equality is based on the underlying values of the <see cref="WriteOnce{TValue}"/> instances.
    /// <br/>For reference equality, use the <see cref="ReferenceEquals(object, object)"/> method or call <see cref="object.Equals(object)"/>.
    /// <para/><see cref="object.GetHashCode"/> and <see cref="object.Equals"/> are based on the wrapping <see cref="WriteOnce{TValue}"/> reference equality.</remarks>
    /// <param name="other">The other <see cref="WriteOnce{TValue}"/> instance to compare with.</param>
    /// <returns><see langword="true"/> if the underlying values are equal; otherwise, <see langword="false"/>.</returns>
    public bool ValueEquals(WriteOnce<TValue>? other) => ValueEquals(this, other);

    /// <summary>
    /// Performs an equality comparison between this instance and another <see cref="WriteOnce{TValue}"/> instance based on the underlying values AND NOT on their references.
    /// </summary>
    /// <remarks>Equality is based on the underlying values of the <see cref="WriteOnce{TValue}"/> instances.
    /// <br/>For reference equality, use the <see cref="ReferenceEquals(object, object)"/> method or call <see cref="object.Equals(object)"/>.
    /// <para/><see cref="object.GetHashCode"/> and <see cref="object.Equals"/> are based on the wrapping <see cref="WriteOnce{TValue}"/> reference equality.</remarks>
    /// <param name="other">The other <see cref="WriteOnce{TValue}"/> instance to compare with.</param>
    /// <returns><see langword="true"/> if the underlying values are equal; otherwise, <see langword="false"/>.</returns>
    public bool ValueEquals(WriteOnce<TValue>? x, WriteOnce<TValue>? y) => ReferenceEquals(x, y)
        || (x is not null && y is not null && EqualityComparer<TValue>.Default.Equals(x.GetValueOrDefault(), y.GetValueOrDefault()));

    /// <summary>
    /// Returns the string representation of the current underlying value, using the specified format and culture-specific format
    /// information.
    /// </summary>
    /// <remarks>If the underlying value implements <see cref="IFormattable"/> like e.g. <see cref="int"/>, its <see cref="IFormattable.ToString"/> implementation is used.
    /// Otherwise, the default <see cref="object.ToString"/> method on the underlying value is called.</remarks>
    /// <param name="format">A standard or custom format string that defines the format to use. If null or empty, the default format is used.</param>
    /// <param name="formatProvider">An object that supplies culture-specific formatting information. If null, the current culture is used.</param>
    /// <returns>A string representation of the current object, formatted as specified by the format and formatProvider
    /// parameters.</returns>
    public string ToString(string? format, IFormatProvider? formatProvider) => GetValueOrDefault() is IFormattable formattable
        ? formattable.ToString(format, formatProvider)
        : ToString();
}