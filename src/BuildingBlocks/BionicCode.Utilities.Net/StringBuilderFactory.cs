#nullable enable
namespace BionicCode.Utilities.Net;

using System;
using System.Collections.Concurrent;
using System.Text;

/// <summary>
/// Provides centralized acquisition and recycling of reusable <see cref="StringBuilder"/> instances
/// for both wrapped and unmanaged usage scenarios.
/// </summary>
/// <remarks>
/// <para>
/// This factory implements a lightweight, lock-free, best-effort pooling strategy for
/// <see cref="StringBuilder"/> instances. Its primary purpose is to reduce short-lived
/// <see cref="StringBuilder"/> allocations in high-frequency formatting, serialization, and
/// interpolated-string scenarios while still placing practical limits on retained memory.
/// </para>
///
/// <para>
/// The pool is intentionally designed around <b>reuse optimization</b> rather than strict resource
/// accounting. In other words, the pool attempts to retain builders when doing so is likely to be
/// beneficial, but it does not guarantee mathematically exact enforcement of all configured limits
/// under heavy concurrency. This is a deliberate design trade-off: the implementation favors a very
/// short and low-contention hot path over globally synchronized admission control.
/// </para>
///
/// <para>
/// Two acquisition models are supported:
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="GetOrCreate()"/>, <see cref="GetOrCreate(ReadOnlySpan{char})"/>, and
/// <see cref="GetOrCreate(int, ReadOnlySpan{char})"/> return a <see cref="PooledStringBuilder"/>,
/// which acts as the lifetime-management facade for a pooled <see cref="StringBuilder"/>.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="GetOrCreateUnmanaged()"/>, <see cref="GetOrCreateUnmanaged(ReadOnlySpan{char})"/>,
/// and <see cref="GetOrCreateUnmanaged(int, ReadOnlySpan{char})"/> return the raw
/// <see cref="StringBuilder"/> directly for internal high-performance scenarios that can manage
/// recycling explicitly.
/// </description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// When a retained builder is taken from the pool, the pool's approximate accounting values are
/// reduced using atomic operations. If the caller requested a larger initial capacity than the
/// reused builder currently provides, the builder capacity is increased before it is returned.
/// Seed content, if supplied, is appended after acquisition.
/// </para>
///
/// <para>
/// When a builder is recycled, the factory evaluates whether the instance is eligible to be cached.
/// Admission is governed by three independent best-effort limits:
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="MaxPoolSize"/> limits the approximate number of retained builders.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="MaxRetainedCapacityPerBuilder"/> prevents unusually large builders from being kept.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="MaxRetainedTotalCapacity"/> limits the approximate sum of retained builder capacities
/// across the entire pool.
/// </description>
/// </item>
/// </list>
/// Builders that do not satisfy these constraints are simply not cached and are left for the GC.
/// </para>
///
/// <para>
/// The values exposed via <see cref="Count"/> and <see cref="CurrentRetainedCapacity"/> are
/// intentionally approximate in concurrent scenarios. They are suitable for diagnostics,
/// telemetry, and best-effort admission decisions, but they should not be interpreted as strict
/// invariants. Because eligibility checks and bag publication are not synchronized as one atomic
/// transaction, temporary overshoot is possible under contention. This behavior is acceptable by
/// design and is considered preferable to introducing a global lock on the recycle path.
/// </para>
///
/// <para>
/// Before a builder is published back into the pool, its contents are cleared so that every builder
/// made visible to future consumers is already in a reusable state. This preserves an important pool
/// invariant: any successfully rented builder can be used immediately without requiring a second
/// cleanup step at acquisition time.
/// </para>
///
/// <para>
/// Conceptually, this type is not a general-purpose cache. It is a specialized pooling mechanism for
/// transient mutable text buffers where reuse is beneficial, allocation pressure is expected, and a
/// best-effort bounded retention policy is sufficient.
/// </para>
/// </remarks>
internal static class StringBuilderFactory
{
    private static readonly ConcurrentBag<StringBuilder> s_stringBuilderPool = [];
    public const int MaxPoolSize = 32;
    public const int MaxRetainedCapacityPerBuilder = 512;
    public const int MaxRetainedTotalCapacity = 16 * MaxRetainedCapacityPerBuilder;

    private static int s_currentRetainedCapacity;
    public static int CurrentRetainedCapacity { get => s_currentRetainedCapacity; private set => s_currentRetainedCapacity = value; }

    private static int s_count;
    public static int Count { get => s_count; private set => s_count = value; }

    public static PooledStringBuilder GetOrCreate()
        => GetOrCreateInternal(0, ReadOnlySpan<char>.Empty);

    public static PooledStringBuilder GetOrCreate(ReadOnlySpan<char> content)
        => GetOrCreateInternal(0, content);

    public static PooledStringBuilder GetOrCreate(int capacity, ReadOnlySpan<char> content)
        => GetOrCreateInternal(capacity, content);

    public static StringBuilder GetOrCreateUnmanaged()
        => GetOrCreateUnmanagedInternal(0, ReadOnlySpan<char>.Empty);

    public static StringBuilder GetOrCreateUnmanaged(ReadOnlySpan<char> content)
        => GetOrCreateUnmanagedInternal(0, content);

    public static StringBuilder GetOrCreateUnmanaged(int capacity, ReadOnlySpan<char> content)
        => GetOrCreateUnmanagedInternal(capacity, content);

    private static StringBuilder GetOrCreateUnmanagedInternal(int capacity, ReadOnlySpan<char> content)
    {
        if (s_stringBuilderPool.TryTake(out StringBuilder? stringBuilder))
        {
            _ = Interlocked.Decrement(ref s_count);
            _ = Interlocked.Add(ref s_currentRetainedCapacity, -stringBuilder.Capacity);

            if (capacity > stringBuilder.Capacity)
            {
                stringBuilder.Capacity = capacity;
            }
        }
        else
        {
            stringBuilder = new StringBuilder(capacity);
        }

        if (!content.IsEmpty)
        {
            _ = stringBuilder.Append(content);
        }

        return stringBuilder;
    }

    private static PooledStringBuilder GetOrCreateInternal(int capacity, ReadOnlySpan<char> content)
    {
        StringBuilder unmanagedStringBuilder = GetOrCreateUnmanagedInternal(capacity, content);
        return PooledStringBuilder.CreateInternal(unmanagedStringBuilder);
    }

    public static void Recycle(StringBuilder? stringBuilder)
    {
        if (stringBuilder is null)
        {
            return;
        }

        AddToPool(stringBuilder);
    }

    private static void AddToPool(StringBuilder stringBuilder)
    {
        if (IsEligibleForCaching(stringBuilder.Capacity))
        {
            _ = Interlocked.Increment(ref s_count);
            _ = Interlocked.Add(ref s_currentRetainedCapacity, stringBuilder.Capacity);
            _ = stringBuilder.Clear();
            s_stringBuilderPool.Add(stringBuilder);
        }
    }

    private static bool IsEligibleForCaching(int capacityToFit) => s_count < MaxPoolSize
        && capacityToFit <= MaxRetainedCapacityPerBuilder
        && s_currentRetainedCapacity + capacityToFit <= MaxRetainedTotalCapacity;
}