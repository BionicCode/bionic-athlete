#nullable enable
namespace BionicCode.Utilities.Net;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

public class PooledStringBuilder : IDisposable
{
    private const string StringBuilderRecycledExceptionMessage = "Underlying StringBuilder has been recycled. Create a new PooledStringBuilder instance.";

    /// <summary>
    /// Represents the default character used for indentation.
    /// </summary>
    /// <remarks>This constant is typically used when formatting text or code that requires indentation. The
    /// default value is a single space character.</remarks>
    /// <value>The default indentation character <c>' '</c>.</value>
    public const char DefaultIndentationChar = ' ';

    /// <summary>
    /// Represents the default number of spaces used for indentation.
    /// </summary>
    /// <remarks>This constant is commonly used in code formatting and text alignment scenarios to define the standard indentation level. The default value is typically 4 spaces, which is a common convention in many coding styles.</remarks>
    /// <value>The default indentation level <c>4</c>.</value>
    public const int DefaultIndentationLevel = 4;

    private StringBuilder? _stringBuilder;
    private char _indentationChar = DefaultIndentationChar;
    private bool _hasCustomIndentationChar;
    private bool _hasCustomIndentationLevel;
    private int _indentationLevel;
    private (int IndentationLevel, char IndentationChar) CurrentIndentationInfo
    {
        get
        {
            int indentationLevel = _hasCustomIndentationLevel ? IndentationLevel : DefaultIndentationLevel;
            char indentationChar = _hasCustomIndentationChar ? IndentationChar : DefaultIndentationChar;
            return (indentationLevel, indentationChar);
        }
    }

    private StringBuilder StringBuilder
        => _stringBuilder ?? throw new InvalidOperationException(PooledStringBuilder.StringBuilderRecycledExceptionMessage);

    /// <summary>
    /// Gets or sets the character used for indentation in formatted output.
    /// </summary>
    /// <remarks>Changing this property allows customization of the indentation style, such as using a space
    /// or tab character. The default value is <see cref="DefaultIndentationChar"/>.</remarks>
    public char IndentationChar
    {
        get => _indentationChar;
        set
        {
            _indentationChar = value;
            _hasCustomIndentationChar = true;
        }
    }

    /// <summary>
    /// Gets or sets the current indentation level.
    /// </summary>
    /// <remarks>The indentation level determines how many indentation units are applied when formatting
    /// output. Setting this property to a negative value will throw an exception.</remarks>
    /// <value>The current indentation level. Must be greater than or equal to 0. The default is <see cref="DefaultIndentationLevel"/>.</value>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when attempting to set a negative indentation level.</exception>
    public int IndentationLevel
    {
        get => _indentationLevel;
        set
        {
            ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(value);
            _indentationLevel = value;
            _hasCustomIndentationLevel = true;
        }
    }

    public bool IsDisposed => IsRecycled;

    public bool IsRecycled => _stringBuilder is null;

    public int Capacity
    {
        get => StringBuilder.Capacity;
        set => StringBuilder.Capacity = value;
    }

    public int MaxCapacity => StringBuilder.MaxCapacity;

    public int Length
    {
        get => StringBuilder.Length;
        set => StringBuilder.Length = value;
    }

    public char this[int index]
    {
        get => StringBuilder[index];
        set => StringBuilder[index] = value;
    }

    // ============================================================================
    // Construction
    // ============================================================================

    private PooledStringBuilder(StringBuilder stringBuilder)
        => _stringBuilder = stringBuilder;

    public static PooledStringBuilder GetOrCreate()
        => StringBuilderFactory.GetOrCreate();

    public static PooledStringBuilder GetOrCreate(ReadOnlySpan<char> seed)
        => StringBuilderFactory.GetOrCreate(seed);

    public static PooledStringBuilder Create(StringBuilder stringBuilder)
        => new(stringBuilder);

    internal static PooledStringBuilder CreateInternal(StringBuilder stringBuilder)
        => new(stringBuilder);

    // ============================================================================
    // CAPACITY & CONVERSION METHODS
    // ============================================================================

    public int EnsureCapacity(int capacity)
    {
        StringBuilder builder = StringBuilder;
        return builder.EnsureCapacity(capacity);
    }

    public override string ToString()
    {
        StringBuilder builder = StringBuilder;
        return builder.ToString();
    }

    public string ToString(int startIndex, int length)
    {
        StringBuilder builder = StringBuilder;
        return builder.ToString(startIndex, length);
    }

    public PooledStringBuilder Clear()
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Clear();
        return this;
    }

    // ============================================================================
    // ENUMERATION
    // ============================================================================

    public StringBuilder.ChunkEnumerator GetChunks()
    {
        StringBuilder builder = StringBuilder;
        return builder.GetChunks();
    }

    // ============================================================================
    // APPEND METHODS
    // ============================================================================

    public PooledStringBuilder Append(char value, int repeatCount)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(repeatCount);

        StringBuilder builder = StringBuilder;
        _ = builder.Append(value, repeatCount);
        return this;
    }

    public PooledStringBuilder Append(char value)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Append(value);
        return this;
    }

    public PooledStringBuilder AppendIndented(char value, int indentationLevel, char indentationChar)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(indentationLevel);

        StringBuilder builder = StringBuilder;
        _ = builder.Append(indentationChar, indentationLevel)
            .Append(value);
        return this;
    }

    public PooledStringBuilder AppendIndented(char value)
    {
        _ = Indent()
            .Append(value);
        return this;
    }

    public PooledStringBuilder Append(bool value)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Append(value);
        return this;
    }

    public PooledStringBuilder AppendIndented(bool value, int indentationLevel, char indentationChar)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(indentationLevel);

        StringBuilder builder = StringBuilder;
        _ = builder.Append(indentationChar, indentationLevel)
            .Append(value);
        return this;
    }

    public PooledStringBuilder AppendIndented(bool value)
    {
        _ = Indent()
            .Append(value);
        return this;
    }

    public PooledStringBuilder Append(sbyte value)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Append(value);
        return this;
    }

    public PooledStringBuilder AppendIndented(sbyte value, int indentationLevel, char indentationChar)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(indentationLevel);

        StringBuilder builder = StringBuilder;
        _ = builder.Append(indentationChar, indentationLevel)
            .Append(value);
        return this;
    }

    public PooledStringBuilder AppendIndented(sbyte value)
    {
        _ = Indent()
            .Append(value);
        return this;
    }

    public PooledStringBuilder Append(byte value)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Append(value);
        return this;
    }

    public PooledStringBuilder AppendIndented(byte value, int indentationLevel, char indentationChar)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(indentationLevel);

        StringBuilder builder = StringBuilder;
        _ = builder.Append(indentationChar, indentationLevel)
            .Append(value);
        return this;
    }

    public PooledStringBuilder AppendIndented(byte value)
    {
        _ = Indent()
            .Append(value);
        return this;
    }

    public PooledStringBuilder Append(short value)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Append(value);
        return this;
    }

    public PooledStringBuilder AppendIndented(short value, int indentationLevel, char indentationChar)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(indentationLevel);

        StringBuilder builder = StringBuilder;
        _ = builder.Append(indentationChar, indentationLevel)
            .Append(value);
        return this;
    }

    public PooledStringBuilder AppendIndented(short value)
    {
        _ = Indent()
            .Append(value);
        return this;
    }

    public PooledStringBuilder Append(int value)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Append(value);
        return this;
    }

    public PooledStringBuilder AppendIndented(int value, int indentationLevel, char indentationChar)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(indentationLevel);

        StringBuilder builder = StringBuilder;
        _ = builder.Append(indentationChar, indentationLevel)
            .Append(value);
        return this;
    }

    public PooledStringBuilder AppendIndented(int value)
    {
        _ = Indent()
            .Append(value);
        return this;
    }

    public PooledStringBuilder Append(long value)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Append(value);
        return this;
    }

    public PooledStringBuilder AppendIndented(long value, int indentationLevel, char indentationChar)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(indentationLevel);

        StringBuilder builder = StringBuilder;
        _ = builder.Append(indentationChar, indentationLevel)
            .Append(value);
        return this;
    }

    public PooledStringBuilder AppendIndented(long value)
    {
        _ = Indent()
            .Append(value);
        return this;
    }

    public PooledStringBuilder Append(float value)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Append(value);
        return this;
    }

    public PooledStringBuilder AppendIndented(float value, int indentationLevel, char indentationChar)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(indentationLevel);

        StringBuilder builder = StringBuilder;
        _ = builder.Append(indentationChar, indentationLevel)
            .Append(value);
        return this;
    }

    public PooledStringBuilder AppendIndented(float value)
    {
        _ = Indent()
            .Append(value);
        return this;
    }

    public PooledStringBuilder Append(double value)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Append(value);
        return this;
    }

    public PooledStringBuilder AppendIndented(double value, int indentationLevel, char indentationChar)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(indentationLevel);

        StringBuilder builder = StringBuilder;
        _ = builder.Append(indentationChar, indentationLevel)
            .Append(value);
        return this;
    }

    public PooledStringBuilder AppendIndented(double value)
    {
        _ = Indent()
            .Append(value);
        return this;
    }

    public PooledStringBuilder Append(decimal value)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Append(value);
        return this;
    }

    public PooledStringBuilder AppendIndented(decimal value, int indentationLevel, char indentationChar)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(indentationLevel);

        StringBuilder builder = StringBuilder;
        _ = builder.Append(indentationChar, indentationLevel)
            .Append(value);
        return this;
    }

    public PooledStringBuilder AppendIndented(decimal value)
    {
        _ = Indent()
            .Append(value);
        return this;
    }

    public PooledStringBuilder Append(ushort value)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Append(value);
        return this;
    }

    public PooledStringBuilder AppendIndented(ushort value, int indentationLevel, char indentationChar)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(indentationLevel);

        StringBuilder builder = StringBuilder;
        _ = builder.Append(indentationChar, indentationLevel)
            .Append(value);
        return this;
    }

    public PooledStringBuilder AppendIndented(ushort value)
    {
        _ = Indent()
            .Append(value);
        return this;
    }

    public PooledStringBuilder Append(uint value)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Append(value);
        return this;
    }

    public PooledStringBuilder AppendIndented(uint value, int indentationLevel, char indentationChar)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(indentationLevel);

        StringBuilder builder = StringBuilder;
        _ = builder.Append(indentationChar, indentationLevel)
            .Append(value);
        return this;
    }

    public PooledStringBuilder AppendIndented(uint value)
    {
        _ = Indent()
            .Append(value);
        return this;
    }

    public PooledStringBuilder Append(ulong value)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Append(value);
        return this;
    }

    public PooledStringBuilder AppendIndented(ulong value, int indentationLevel, char indentationChar)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(indentationLevel);

        StringBuilder builder = StringBuilder;
        _ = builder.Append(indentationChar, indentationLevel)
            .Append(value);
        return this;
    }

    public PooledStringBuilder AppendIndented(ulong value)
    {
        _ = Indent()
            .Append(value);
        return this;
    }

    public PooledStringBuilder Append(object? value)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Append(value);
        return this;
    }

    public PooledStringBuilder AppendIndented(object? value, int indentationLevel, char indentationChar)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(indentationLevel);

        StringBuilder builder = StringBuilder;
        _ = builder.Append(indentationChar, indentationLevel)
            .Append(value);
        return this;
    }

    public PooledStringBuilder AppendIndented(object? value)
    {
        _ = Indent()
            .Append(value);
        return this;
    }

    public PooledStringBuilder Append(string? value)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Append(value);
        return this;
    }

    public PooledStringBuilder Append(string? value, int repeatCount)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(repeatCount);

        StringBuilder builder = StringBuilder;
        for (int i = 0; i < repeatCount; i++)
        {
            _ = builder.Append(value);
        }

        return this;
    }

    public PooledStringBuilder AppendIndented(string value, int indentationLevel, char indentationChar)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(indentationLevel);

        StringBuilder builder = StringBuilder;
        _ = builder.Append(indentationChar, indentationLevel)
            .Append(value);
        return this;
    }

    public PooledStringBuilder AppendIndented(string value)
    {
        _ = Indent()
            .Append(value);
        return this;
    }

    public PooledStringBuilder Append(string? value, int startIndex, int count)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Append(value, startIndex, count);
        return this;
    }

    public PooledStringBuilder AppendIndented(string? value, int startIndex, int count, int indentationLevel, char indentationChar)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(indentationLevel);

        StringBuilder builder = StringBuilder;
        _ = builder.Append(indentationChar, indentationLevel)
            .Append(value, startIndex, count);
        return this;
    }

    public PooledStringBuilder AppendIndented(string? value, int startIndex, int count)
    {
        _ = Indent()
            .Append(value, startIndex, count);
        return this;
    }

    public PooledStringBuilder Append(char[]? value)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Append(value);
        return this;
    }

    public PooledStringBuilder AppendIndented(char[]? value, int indentationLevel, char indentationChar)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(indentationLevel);

        StringBuilder builder = StringBuilder;
        _ = builder.Append(indentationChar, indentationLevel)
            .Append(value);
        return this;
    }

    public PooledStringBuilder AppendIndented(char[]? value)
    {
        _ = Indent()
            .Append(value);
        return this;
    }

    public PooledStringBuilder Append(char[]? value, int startIndex, int charCount)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Append(value, startIndex, charCount);
        return this;
    }

    public PooledStringBuilder AppendIndented(char[]? value, int startIndex, int charCount, int indentationLevel, char indentationChar)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(indentationLevel);

        StringBuilder builder = StringBuilder;
        _ = builder.Append(indentationChar, indentationLevel)
            .Append(value, startIndex, charCount);
        return this;
    }

    public PooledStringBuilder AppendIndented(char[]? value, int startIndex, int charCount)
    {
        _ = Indent()
            .Append(value, startIndex, charCount);
        return this;
    }

    public PooledStringBuilder Append(ReadOnlySpan<char> value)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Append(value);
        return this;
    }

    public PooledStringBuilder Append(ReadOnlySpan<char> value, int repeatCount)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(repeatCount);

        StringBuilder builder = StringBuilder;
        for (int i = 0; i < repeatCount; i++)
        {
            _ = builder.Append(value);
        }

        return this;
    }

    public PooledStringBuilder AppendIndented(ReadOnlySpan<char> value, int indentationLevel, char indentationChar)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(indentationLevel);

        StringBuilder builder = StringBuilder;
        _ = builder.Append(indentationChar, indentationLevel)
            .Append(value);
        return this;
    }

    public PooledStringBuilder AppendIndented(ReadOnlySpan<char> value)
    {
        _ = Indent()
            .Append(value);
        return this;
    }

    public PooledStringBuilder Append(ReadOnlyMemory<char> value)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Append(value);
        return this;
    }

    public PooledStringBuilder Append(ReadOnlyMemory<char> value, int repeatCount)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(repeatCount);

        StringBuilder builder = StringBuilder;
        for (int i = 0; i < repeatCount; i++)
        {
            _ = builder.Append(value);
        }

        return this;
    }

    public PooledStringBuilder AppendIndented(ReadOnlyMemory<char> value, int indentationLevel, char indentationChar)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(indentationLevel);

        StringBuilder builder = StringBuilder;
        _ = builder.Append(indentationChar, indentationLevel)
            .Append(value);
        return this;
    }

    public PooledStringBuilder AppendIndented(ReadOnlyMemory<char> value)
    {
        _ = Indent()
            .Append(value);
        return this;
    }

    public PooledStringBuilder Append(ReadOnlySpan<char> value, int startIndex, int count)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Append(value.Slice(startIndex, count));
        return this;
    }

    public PooledStringBuilder AppendIndented(ReadOnlySpan<char> value, int startIndex, int count, int indentationLevel, char indentationChar)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(indentationLevel);

        StringBuilder builder = StringBuilder;
        _ = builder.Append(indentationChar, indentationLevel)
            .Append(value.Slice(startIndex, count));
        return this;
    }

    public PooledStringBuilder AppendIndented(ReadOnlySpan<char> value, int startIndex, int count)
    {
        _ = Indent()
            .Append(value.Slice(startIndex, count));
        return this;
    }

    public PooledStringBuilder Append(StringBuilder? value, int repeatCount)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(repeatCount);

        StringBuilder builder = StringBuilder;
        for (int i = 0; i < repeatCount; i++)
        {
            _ = builder.Append(value);
        }

        return this;
    }

    public PooledStringBuilder AppendIndented(StringBuilder? value, int indentationLevel, char indentationChar)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(indentationLevel);

        StringBuilder builder = StringBuilder;
        _ = builder.Append(indentationChar, indentationLevel)
            .Append(value);
        return this;
    }

    public PooledStringBuilder AppendIndented(StringBuilder? value)
    {
        _ = Indent()
            .Append(value);
        return this;
    }

    public PooledStringBuilder Append(StringBuilder? value, int startIndex, int count)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Append(value, startIndex, count);
        return this;
    }

    public PooledStringBuilder AppendIndented(StringBuilder? value, int startIndex, int count, int indentationLevel, char indentationChar)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(indentationLevel);

        StringBuilder builder = StringBuilder;
        _ = builder.Append(indentationChar, indentationLevel)
            .Append(value, startIndex, count);
        return this;
    }

    public PooledStringBuilder AppendIndented(StringBuilder? value, int startIndex, int count)
    {
        _ = Indent()
            .Append(value, startIndex, count);
        return this;
    }

    public unsafe PooledStringBuilder Append(char* value, int valueCount)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Append(value, valueCount);
        return this;
    }

    public unsafe PooledStringBuilder AppendIndented(char* value, int valueCount, int indentationLevel, char indentationChar)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(indentationLevel);

        StringBuilder builder = StringBuilder;
        _ = builder.Append(indentationChar, indentationLevel)
            .Append(value, valueCount);
        return this;
    }

    public unsafe PooledStringBuilder AppendIndented(char* value, int valueCount)
    {
        _ = Indent()
            .Append(value, valueCount);
        return this;
    }

    public PooledStringBuilder Append([InterpolatedStringHandlerArgument("")] ref PooledStringBuilder.AppendInterpolatedStringHandler handler)
        => this;

    public PooledStringBuilder Append(int repeatCount, [InterpolatedStringHandlerArgument("", nameof(repeatCount))] ref PooledStringBuilder.AppendInterpolatedStringHandler handler)
        => handler.WriteTo(this);

    public PooledStringBuilder AppendIndented(int indentationLevel, char indentationChar, [InterpolatedStringHandlerArgument("", nameof(indentationLevel), nameof(indentationChar))] ref PooledStringBuilder.AppendInterpolatedStringHandler handler)
        => this;

    public PooledStringBuilder AppendIndented([InterpolatedStringHandlerArgument("")] ref PooledStringBuilder.AppendInterpolatedStringHandler handler)
        => AppendInterpolatedIndented(ref handler);

    public PooledStringBuilder AppendIndented(int indentationLevel, char indentationChar, int repeatCount, [InterpolatedStringHandlerArgument("", nameof(indentationLevel), nameof(indentationChar), nameof(repeatCount))] ref PooledStringBuilder.AppendInterpolatedStringHandler handler)
        => this;

    public PooledStringBuilder AppendIndented(int repeatCount, [InterpolatedStringHandlerArgument("", nameof(repeatCount))] ref PooledStringBuilder.AppendInterpolatedStringHandler handler)
        => AppendInterpolatedIndented(ref handler);

    public PooledStringBuilder Append(IFormatProvider? provider, [InterpolatedStringHandlerArgument("", nameof(provider))] ref PooledStringBuilder.AppendInterpolatedStringHandler handler)
        => this;

    public PooledStringBuilder Append(IFormatProvider? provider, int repeatCount, [InterpolatedStringHandlerArgument("", nameof(provider), nameof(repeatCount))] ref PooledStringBuilder.AppendInterpolatedStringHandler handler)
        => handler.WriteTo(this);

    public PooledStringBuilder AppendIndented(IFormatProvider? provider, int indentationLevel, char indentationChar, [InterpolatedStringHandlerArgument("", nameof(provider), nameof(indentationLevel), nameof(indentationChar))] ref PooledStringBuilder.AppendInterpolatedStringHandler handler)
        => this;

    public PooledStringBuilder AppendIndented(IFormatProvider? provider, [InterpolatedStringHandlerArgument("", nameof(provider))] ref PooledStringBuilder.AppendInterpolatedStringHandler handler)
        => AppendInterpolatedIndented(ref handler);

    public PooledStringBuilder AppendIndented(IFormatProvider? provider, int indentationLevel, char indentationChar, int repeatCount, [InterpolatedStringHandlerArgument("", nameof(provider), nameof(indentationLevel), nameof(indentationChar), nameof(repeatCount))] ref PooledStringBuilder.AppendInterpolatedStringHandler handler)
        => this;

    public PooledStringBuilder AppendIndented(IFormatProvider? provider, int repeatCount, [InterpolatedStringHandlerArgument("", nameof(provider), nameof(repeatCount))] ref PooledStringBuilder.AppendInterpolatedStringHandler handler)
        => AppendInterpolatedIndented(ref handler);

    private PooledStringBuilder AppendInterpolatedIndented(ref AppendInterpolatedStringHandler handler)
    {
        (int indentationLevel, char indentationChar) = CurrentIndentationInfo;
        return handler.WriteTo(this, indentationLevel, indentationChar);
    }

    // ============================================================================
    // APPENDLINE METHODS
    // ============================================================================

    public PooledStringBuilder AppendLine()
    {
        StringBuilder builder = StringBuilder;
        _ = builder.AppendLine();
        return this;
    }

    /// <summary>
    /// Appends the default line terminator to the end of the current <see cref="PooledStringBuilder"/> object 
    /// <br/>and indents the next line using the specified indentation character and level, followed by the default line terminator.
    /// </summary>
    /// <param name="indentationLevel">The level of indentation to apply.</param>
    /// <param name="indentationChar">The character to use for indentation.</param>
    /// <returns>The current <see cref="PooledStringBuilder"/> instance.</returns>
    public PooledStringBuilder AppendIndentedLine(int indentationLevel, char indentationChar)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(indentationLevel);

        StringBuilder builder = StringBuilder;
        _ = builder.AppendLine()
            .Append(indentationChar, indentationLevel);
        return this;
    }

    /// <summary>
    /// Appends the default line terminator to the end of the current <see cref="PooledStringBuilder"/> object 
    /// <br/>and indents the next line using the <see cref="indentationChar"/> character (or the default indentation character <see cref="DefaultIndentationChar"/>) 
    /// and <see cref="indentationLevel"/> level (or the default indentation level <see cref="DefaultIndentationLevel"/>), 
    /// followed by the default line terminator.
    /// </summary>
    /// <returns>The current <see cref="PooledStringBuilder"/> instance.</returns>
    public PooledStringBuilder AppendIndentedLine()
    {
        (int indentationLevel, char indentationChar) = CurrentIndentationInfo;
        StringBuilder builder = StringBuilder;
        _ = builder.AppendLine()
            .Append(indentationChar, indentationLevel);
        return this;
    }

    /// <summary>
    /// Appends the specified string followed by the default line terminator to the current instance of the
    /// PooledStringBuilder.
    /// </summary>
    /// <remarks>This method is useful for efficiently building multi-line strings. The line terminator
    /// appended is determined by the underlying StringBuilder implementation.</remarks>
    /// <param name="value">The string to append. If <see langword="null"/>, only the line terminator is appended.</param>
    /// <returns>The current instance of the <see cref="PooledStringBuilder"/>, enabling method chaining.</returns>
    public PooledStringBuilder AppendLine(string? value)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.AppendLine(value);
        return this;
    }

    /// <summary>
    /// Appends the specified string followed by the default line terminator to the current instance of the
    /// PooledStringBuilder.
    /// </summary>
    /// <remarks>This method is useful for efficiently building multi-line strings. The line terminator
    /// appended is determined by the underlying StringBuilder implementation.</remarks>
    /// <param name="value">The string to append. If <see langword="null"/>, only the line terminator is appended.</param>
    /// <returns>The current instance of the <see cref="PooledStringBuilder"/>, enabling method chaining.</returns>
    public PooledStringBuilder AppendLine(StringBuilder value)
    {
        StringBuilder builder = StringBuilder;
        _ = builder
            .Append(value)
            .AppendLine();
        return this;
    }

    /// <summary>
    /// Appends a value using the specified indentation character and level, followed by the default line terminator. The indentation is applied before the value.
    /// </summary>
    /// <param name="value">The indented value to append.</param>
    /// <param name="indentationLevel">The level of indentation to apply.</param>
    /// <param name="indentationChar">The character to use for indentation.</param>
    /// <returns>The current <see cref="PooledStringBuilder"/> instance.</returns>
    public PooledStringBuilder AppendIndentedLine(string value, int indentationLevel, char indentationChar)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(indentationLevel);

        StringBuilder builder = StringBuilder;
        _ = builder.Append(indentationChar, indentationLevel)
            .AppendLine(value);
        return this;
    }

    /// <summary>
    /// Appends a value using the <see cref="IndentationChar"/> indentation character (or, if not specified, the default indentation character <see cref="DefaultIndentationChar"/>) 
    /// and <see cref="IndentationLevel"/> level (or the default indentation level <see cref="DefaultIndentationLevel"/>), 
    /// followed by the default line terminator. The indentation is applied before the value.
    /// </summary>
    /// <param name="value">The indented value to append.</param>
    /// <returns>The current <see cref="PooledStringBuilder"/> instance.</returns>
    public PooledStringBuilder AppendIndentedLine(string value)
    {
        (int indentationLevel, char indentationChar) = CurrentIndentationInfo;
        StringBuilder builder = StringBuilder;
        _ = builder
            .Append(indentationChar, indentationLevel)
            .AppendLine(value);
        return this;
    }

    /// <summary>
    /// Appends a value using the specified indentation character and level, followed by the default line terminator. The indentation is applied before the value.
    /// </summary>
    /// <param name="value">The value to append.</param>
    /// <param name="indentationLevel">The level of indentation to apply.</param>
    /// <param name="indentationChar">The character to use for indentation.</param>
    /// <returns>The current <see cref="PooledStringBuilder"/> instance.</returns>
    public PooledStringBuilder AppendIndentedLine(StringBuilder value, int indentationLevel, char indentationChar)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(indentationLevel);

        StringBuilder builder = StringBuilder;
        _ = builder.Append(indentationChar, indentationLevel)
            .Append(value)
            .AppendLine();

        return this;
    }

    /// <summary>
    /// Appends a value using the <see cref="IndentationChar"/> indentation character (or, if not specified, the default indentation character <see cref="DefaultIndentationChar"/>) 
    /// and <see cref="IndentationLevel"/> level (or the default indentation level <see cref="DefaultIndentationLevel"/>), 
    /// followed by the default line terminator. The indentation is applied before the value.
    /// </summary>
    /// <param name="value">The indented value to append.</param>
    /// <returns>The current <see cref="PooledStringBuilder"/> instance.</returns>
    public PooledStringBuilder AppendIndentedLine(StringBuilder value)
    {
        (int indentationLevel, char indentationChar) = CurrentIndentationInfo;
        StringBuilder builder = StringBuilder;
        _ = builder
            .Append(indentationChar, indentationLevel)
            .Append(value)
            .AppendLine();

        return this;
    }

    public PooledStringBuilder AppendLine([InterpolatedStringHandlerArgument("")] ref PooledStringBuilder.AppendInterpolatedStringHandler handler)
        => AppendLine();

    public PooledStringBuilder AppendLine(int repeatCount, [InterpolatedStringHandlerArgument("", nameof(repeatCount))] ref PooledStringBuilder.AppendInterpolatedStringHandler handler)
        => handler.WriteLineTo(this); // Repeated append requires buffering the interpolated string. Hence we must manually invoke the handler to flush the buffer into the provided StringBuilder or PooledStringBuilder

    public PooledStringBuilder AppendIndentedLine(int indentationLevel, char indentationChar, [InterpolatedStringHandlerArgument("", nameof(indentationLevel), nameof(indentationChar))] ref PooledStringBuilder.AppendInterpolatedStringHandler handler)
        => AppendLine();

    public PooledStringBuilder AppendIndentedLine(int repeatCount, [InterpolatedStringHandlerArgument("", nameof(repeatCount))] ref PooledStringBuilder.AppendInterpolatedStringHandler handler)
        => AppendInterpolatedIndentedLine(ref handler); // Explicitly indent using instance indentation values

    public PooledStringBuilder AppendIndentedLine([InterpolatedStringHandlerArgument("")] ref PooledStringBuilder.AppendInterpolatedStringHandler handler)
        => AppendInterpolatedIndentedLine(ref handler);

    public PooledStringBuilder AppendLine(IFormatProvider? provider, [InterpolatedStringHandlerArgument("", nameof(provider))] ref PooledStringBuilder.AppendInterpolatedStringHandler handler)
        => AppendLine();

    public PooledStringBuilder AppendIndentedLine(IFormatProvider? provider, int indentationLevel, char indentationChar, [InterpolatedStringHandlerArgument("", nameof(provider), nameof(indentationLevel), nameof(indentationChar))] ref PooledStringBuilder.AppendInterpolatedStringHandler handler)
        => AppendLine();

    public PooledStringBuilder AppendIndentedLine(IFormatProvider? provider, [InterpolatedStringHandlerArgument("", nameof(provider))] ref PooledStringBuilder.AppendInterpolatedStringHandler handler)
        => AppendInterpolatedIndentedLine(ref handler);

    public PooledStringBuilder AppendIndentedLine(IFormatProvider? provider, int repeatCount, [InterpolatedStringHandlerArgument("", nameof(provider), nameof(repeatCount))] ref PooledStringBuilder.AppendInterpolatedStringHandler handler)
        => AppendInterpolatedIndentedLine(ref handler);

    private PooledStringBuilder AppendInterpolatedIndentedLine(ref AppendInterpolatedStringHandler handler)
    {
        (int indentationLevel, char indentationChar) = CurrentIndentationInfo;
        return handler.WriteLineTo(this, indentationLevel, indentationChar);
    }

    // ============================================================================
    // APPENDJOIN METHODS
    // ============================================================================

    public PooledStringBuilder AppendJoin(string? separator, params object?[] values)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.AppendJoin(separator, values);
        return this;
    }

    /// <summary>
    /// Appends the specified values to the current instance, joined by a separator and preceded by a repeated
    /// indentation character.
    /// </summary>
    /// <remarks>This method is useful for formatting structured text output with indentation, such as
    /// generating code or hierarchical data representations.</remarks>
    /// <param name="separator">The string to use as a separator between the values. If null, no separator is inserted.</param>
    /// <param name="indentationLevel">The number of times to repeat the indentation character. Must be zero or greater.</param>
    /// <param name="indentationChar">The character to use for indentation. Defaults to a space character if not specified..</param>
    /// <param name="values">An array of objects to join and append. Each <see cref="object"/> is converted to its <see cref="string"/> representation.</param>
    /// <returns>The current instance of the <see cref="PooledStringBuilder"/>, enabling method chaining.</returns>
    public PooledStringBuilder AppendIndentedJoin(string? separator, int indentationLevel, char indentationChar, params object?[] values)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(indentationLevel);

        StringBuilder builder = StringBuilder;
        _ = builder.Append(indentationChar, indentationLevel)
            .AppendJoin(separator, values);
        return this;
    }

    /// <summary>
    /// Appends the specified values to the current instance, joined by a separator and preceded by a repeated
    /// indentation character specified by <see cref="IndentationChar"/> (or the default indentation character <see cref="DefaultIndentationChar"/> if not specified)
    /// and the current indentation level <see cref="IndentationLevel"/> (or the default indentation level <see cref="DefaultIndentationLevel"/> if not specified).
    /// </summary>
    /// <remarks>This method is useful for formatting structured text output with indentation, such as
    /// generating code or hierarchical data representations.</remarks>
    /// <param name="separator">The string to use as a separator between the values. If null, no separator is inserted.</param>
    /// <param name="values">An array of objects to join and append. Each <see cref="object"/> is converted to its <see cref="string"/> representation.</param>
    /// <returns>The current instance of the <see cref="PooledStringBuilder"/>, enabling method chaining.</returns>
    public PooledStringBuilder AppendIndentedJoin(string? separator, params object?[] values)
    {
        _ = Indent()
            .AppendJoin(separator, values);
        return this;
    }

    public PooledStringBuilder AppendJoin<T>(string? separator, IEnumerable<T> values)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.AppendJoin(separator, values);
        return this;
    }

    /// <summary>
    /// Appends the specified values to the current instance, joined by a separator and preceded by a repeated
    /// indentation character.
    /// </summary>
    /// <remarks>This method is useful for formatting structured text output with indentation, such as
    /// generating code or hierarchical data representations.</remarks>
    /// <typeparam name="T">The type of the elements in the collection to join. Each element is converted to its string representation.</typeparam>
    /// <param name="separator">The string to use as a separator between the values. If null, no separator is inserted.</param>
    /// <param name="indentationLevel">The number of times to repeat the indentation character. Must be zero or greater.</param>
    /// <param name="indentationChar">The character to use for indentation. Defaults to a space character if not specified.</param>
    /// <param name="values">A collection of objects to join and append. Each item is converted to its <see cref="string"/> representation.</param>
    /// <returns>The current instance of the <see cref="PooledStringBuilder"/>, enabling method chaining.</returns>
    public PooledStringBuilder AppendIndentedJoin<T>(string? separator, IEnumerable<T> values, int indentationLevel, char indentationChar)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(indentationLevel);

        StringBuilder builder = StringBuilder;
        _ = builder.Append(indentationChar, indentationLevel)
            .AppendJoin(separator, values);
        return this;
    }

    /// <summary>
    /// Appends the specified values to the current instance, joined by a separator and preceded by a repeated
    /// indentation character specified by <see cref="IndentationChar"/> (or the default indentation character <see cref="DefaultIndentationChar"/> if not specified)
    /// and the <see cref="IndentationLevel"/> (or the default indentation level <see cref="DefaultIndentationLevel"/> if not specified).
    /// </summary>
    /// <remarks>This method is useful for formatting structured text output with indentation, such as
    /// generating code or hierarchical data representations.</remarks>
    /// <typeparam name="T">The type of the elements in the collection to join. Each element is converted to its string representation.</typeparam>
    /// <param name="separator">The string to use as a separator between the values. If null, no separator is inserted.</param>
    /// <param name="values">A collection of objects to join and append. Each item is converted to its <see cref="string"/> representation.</param>
    /// <returns>The current instance of the <see cref="PooledStringBuilder"/>, enabling method chaining.</returns>
    public PooledStringBuilder AppendIndentedJoin<T>(string? separator, IEnumerable<T> values)
    {
        _ = Indent()
            .AppendJoin(separator, values);
        return this;
    }

    public PooledStringBuilder AppendJoin(string? separator, params string?[] values)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.AppendJoin(separator, values);
        return this;
    }

    /// <summary>
    /// Appends the specified values to the current instance, joined by a separator and preceded by a repeated
    /// indentation character.
    /// </summary>
    /// <remarks>This method is useful for formatting structured text output with indentation, such as
    /// generating code or hierarchical data representations.</remarks>
    /// <param name="separator">The string to use as a separator between the values. If null, no separator is inserted.</param>
    /// <param name="indentationLevel">The number of times to repeat the indentation character. Must be zero or greater.</param>
    /// <param name="indentationChar">The character to use for indentation. Defaults to a space character if not specified.</param>
    /// <param name="values">An array of <see cref="string"/> to join and append.</param>
    /// <returns>The current instance of the <see cref="PooledStringBuilder"/>, enabling method chaining.</returns>
    public PooledStringBuilder AppendIndentedJoin(string? separator, int indentationLevel, char indentationChar, params string?[] values)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(indentationLevel);

        _ = Indent()
            .AppendJoin(separator, values);
        return this;
    }

    /// <summary>
    /// Appends the specified values to the current instance, joined by a separator and preceded by a repeated
    /// indentation character. The indentation level and character are determined by the current instance's settings <see cref="IndentationLevel"/> and <see cref="IndentationChar"/> (or their default values <see cref="DefaultIndentationLevel"/> and <see cref="DefaultIndentationChar"/> if not set).
    /// </summary>
    /// <param name="separator">The string to use as a separator between the values. If null, no separator is inserted.</param>
    /// <param name="values">An array of <see cref="string"/> to join and append.</param>
    /// <returns>The current instance of the <see cref="PooledStringBuilder"/>, enabling method chaining.</returns>
    public PooledStringBuilder AppendIndentedJoin(string? separator, params string?[] values)
    {
        _ = Indent()
            .AppendJoin(separator, values);
        return this;
    }

    public PooledStringBuilder AppendJoin(char separator, params object?[] values)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.AppendJoin(separator, values);
        return this;
    }

    /// <summary>
    /// Appends the specified values to the current instance, joined by a separator and preceded by a repeated
    /// indentation character.
    /// </summary>
    /// <remarks>This method is useful for formatting structured text output with indentation, such as
    /// generating code or hierarchical data representations.</remarks>
    /// <param name="separator">The character to use as a separator between the values.</param>
    /// <param name="indentationLevel">The number of times to repeat the indentation character. Must be zero or greater.</param>
    /// <param name="indentationChar">The character to use for indentation. Defaults to a space character if not specified.</param>
    /// <param name="values">An array of objects to join and append. Each <see cref="object"/> is converted to its <see cref="string"/> representation.</param>
    /// <returns>The current instance of the <see cref="PooledStringBuilder"/>, enabling method chaining.</returns>
    public PooledStringBuilder AppendIndentedJoin(char separator, int indentationLevel, char indentationChar, params object?[] values)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(indentationLevel);

        StringBuilder builder = StringBuilder;
        _ = builder.Append(indentationChar, indentationLevel)
            .AppendJoin(separator, values);
        return this;
    }

    /// <summary>
    /// Appends the specified values to the current instance, separated by the specified character, with indentation
    /// applied to the beginning of the joined string.
    /// </summary>
    /// <remarks>The indentation applied is determined by the current indentation level and character, which
    /// may be customized for the instance.</remarks>
    /// <param name="separator">The character to use as a separator between each value.</param>
    /// <param name="values">An array of objects to join and append. Each object is converted to its string representation. Null values are
    /// represented as empty strings.</param>
    /// <returns>The current instance with the indented, joined values appended.</returns>
    public PooledStringBuilder AppendIndentedJoin(char separator, params object?[] values)
    {
        _ = Indent()
            .AppendJoin(separator, values);
        return this;
    }

    public PooledStringBuilder AppendJoin<T>(char separator, IEnumerable<T> values)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.AppendJoin(separator, values);
        return this;
    }

    /// <summary>
    /// Appends the specified values to the current instance, joined by a separator and preceded by a repeated
    /// indentation character.
    /// </summary>
    /// <remarks>This method is useful for formatting structured text output with indentation, such as
    /// generating code or hierarchical data representations.</remarks>
    /// <typeparam name="T">The type of the elements in the collection to join. Each element is converted to its string representation.</typeparam>
    /// <param name="separator">The character to use as a separator between the values.</param>
    /// <param name="indentationLevel">The number of times to repeat the indentation character. Must be zero or greater.</param>
    /// <param name="indentationChar">The character to use for indentation.</param>
    /// <param name="values">A collection of objects to join and append. Each object is converted to its <see cref="string"/> representation.</param>
    /// <returns>The current instance of the <see cref="PooledStringBuilder"/>, enabling method chaining.</returns>
    public PooledStringBuilder AppendIndentedJoin<T>(char separator, IEnumerable<T> values, int indentationLevel, char indentationChar)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(indentationLevel);

        StringBuilder builder = StringBuilder;
        _ = builder.Append(indentationChar, indentationLevel)
            .AppendJoin(separator, values);
        return this;
    }

    /// <summary>
    /// Appends the specified values to the current instance, separated by the specified character, with indentation
    /// applied to the beginning of the line.
    /// </summary>
    /// <remarks>The indentation is determined by the current indentation level and character settings. This
    /// method is useful for formatting collections with consistent indentation in the resulting string.</remarks>
    /// <typeparam name="T">The type of the elements in the values collection.</typeparam>
    /// <param name="separator">The character to use as a separator between each value.</param>
    /// <param name="values">The collection of values to append, each separated by the specified character.</param>
    /// <returns>The current instance with the indented, joined values appended.</returns>
    public PooledStringBuilder AppendIndentedJoin<T>(char separator, IEnumerable<T> values)
    {
        _ = Indent()
            .AppendJoin(separator, values);
        return this;
    }

    public PooledStringBuilder AppendJoin(char separator, params string?[] values)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.AppendJoin(separator, values);
        return this;
    }

    /// <summary>
    /// Appends the specified values to the current instance, joined by a separator and preceded by a repeated
    /// indentation character.
    /// </summary>
    /// <remarks>This method is useful for formatting structured text output with indentation, such as
    /// generating code or hierarchical data representations.</remarks>
    /// <param name="separator">The character to use as a separator between the values.</param>
    /// <param name="indentationLevel">The number of times to repeat the indentation character. Must be zero or greater.</param>
    /// <param name="indentationChar">The character to use for indentation.</param>
    /// <param name="values">An array of <see cref="string"/> to join and append.</param>
    /// <returns>The current instance of the <see cref="PooledStringBuilder"/>, enabling method chaining.</returns>
    public PooledStringBuilder AppendIndentedJoin(char separator, int indentationLevel, char indentationChar, params string?[] values)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(indentationLevel);

        StringBuilder builder = StringBuilder;
        _ = builder.Append(indentationChar, indentationLevel)
            .AppendJoin(separator, values);
        return this;
    }

    /// <summary>
    /// Appends the specified values to the current instance, separated by the specified character, with indentation
    /// applied to the beginning of the line.
    /// </summary>
    /// <remarks>The indentation is determined by the current indentation level and character settings. This
    /// method is useful for formatting collections with consistent indentation in the resulting string.</remarks>
    /// <typeparam name="T">The type of the elements in the values collection.</typeparam>
    /// <param name="separator">The character to use as a separator between each value.</param>
    /// <param name="values">The collection of values to append, each separated by the specified character.</param>
    /// <returns>The current instance with the indented, joined values appended.</returns>
    public PooledStringBuilder AppendIndentedJoin(char separator, params string?[] values)
    {
        _ = Indent()
            .AppendJoin(separator, values);
        return this;
    }

#if NET9_0_OR_GREATER

    public PooledStringBuilder AppendJoin(string? separator, params ReadOnlySpan<object?> values)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.AppendJoin(separator, values);
        return this;
    }

    /// <summary>
    /// Appends the specified values to the current instance, joined by a separator and preceded by a repeated
    /// indentation character.
    /// </summary>
    /// <remarks>This method is useful for formatting structured text output with indentation, such as
    /// generating code or hierarchical data representations.</remarks>
    /// <param name="separator">The string to use as a separator between the values. If null, no separator is inserted.</param>
    /// <param name="indentationLevel">The number of times to repeat the indentation character. Must be zero or greater.</param>
    /// <param name="indentationChar">The character to use for indentation. Defaults to a space character if not specified.</param>
    /// <param name="values">An array of <see cref="ReadOnlySpan{T}"/> to join and append. Each <see cref="object"/> is converted to its <see cref="string"/> representation.</param>
    /// <returns>The current instance of the <see cref="PooledStringBuilder"/>, enabling method chaining.</returns>
    public PooledStringBuilder AppendIndentedJoin(string? separator, int indentationLevel, char indentationChar, params ReadOnlySpan<object?> values)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(indentationLevel);

        StringBuilder builder = StringBuilder;
        _ = builder.Append(indentationChar, indentationLevel)
            .AppendJoin(separator, values);
        return this;
    }

    /// <summary>
    /// Appends the specified values to the current instance, separated by the specified separator string, with each
    /// value preceded by the current indentation.
    /// </summary>
    /// <remarks>The indentation applied to each value is determined by the current indentation level and
    /// character settings of the instance. This method does not add a separator before the first value or after the
    /// last value.</remarks>
    /// <param name="separator">The string to use as a separator between each value. If null, no separator is used.</param>
    /// <param name="values">A read-only span containing the values to append. Each value is preceded by the current indentation.</param>
    /// <returns>The current instance with the appended, indented values.</returns>
    public PooledStringBuilder AppendIndentedJoin(string? separator, params ReadOnlySpan<object?> values)
    {
        _ = Indent()
            .AppendJoin(separator, values);
        return this;
    }

    public PooledStringBuilder AppendJoin(string? separator, params ReadOnlySpan<string?> values)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.AppendJoin(separator, values);
        return this;
    }

    /// <summary>
    /// Appends the specified values to the current instance, joined by a separator and preceded by a repeated
    /// indentation character.
    /// </summary>
    /// <remarks>This method is useful for formatting structured text output with indentation, such as
    /// generating code or hierarchical data representations.</remarks>
    /// <param name="separator">The string to use as a separator between the values. If null, no separator is inserted.</param>
    /// <param name="indentationLevel">The number of times to repeat the indentation character. Must be zero or greater.</param>
    /// <param name="indentationChar">The character to use for indentation. Defaults to a space character if not specified.</param>
    /// <param name="values">An array of <see cref="ReadOnlySpan{T}"/> to join and append.</param>
    /// <returns>The current instance of the <see cref="PooledStringBuilder"/>, enabling method chaining.</returns>
    public PooledStringBuilder AppendIndentedJoin(string? separator, int indentationLevel, char indentationChar, params ReadOnlySpan<string?> values)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(indentationLevel);

        StringBuilder builder = StringBuilder;
        _ = builder.Append(indentationChar, indentationLevel)
            .AppendJoin(separator, values);
        return this;
    }

    /// <summary>
    /// Appends the specified values to the builder, separated by the specified separator, with the current indentation
    /// applied to the beginning of the line.
    /// </summary>
    /// <remarks>The indentation applied is determined by the current indentation level and character settings
    /// of the builder. This method is useful for constructing indented, delimited lists within the builder.</remarks>
    /// <param name="separator">The string to use as a separator between each value. Can be null to omit a separator.</param>
    /// <param name="values">A read-only span containing the string values to append. Each value will be separated by the specified
    /// separator.</param>
    /// <returns>The current instance of <see cref="PooledStringBuilder"/> with the appended values.</returns>
    public PooledStringBuilder AppendIndentedJoin(string? separator, params ReadOnlySpan<string?> values)
    {
        _ = Indent()
            .AppendJoin(separator, values);
        return this;
    }

    public PooledStringBuilder AppendJoin(char separator, params ReadOnlySpan<object?> values)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.AppendJoin(separator, values);
        return this;
    }

    /// <summary>
    /// Appends the specified values to the current instance, joined by a separator and preceded by a repeated
    /// indentation character.
    /// </summary>
    /// <remarks>This method is useful for formatting structured text output with indentation, such as
    /// generating code or hierarchical data representations.</remarks>
    /// <param name="separator">The string to use as a separator between the values. If null, no separator is inserted.</param>
    /// <param name="indentationLevel">The number of times to repeat the indentation character. Must be zero or greater.</param>
    /// <param name="indentationChar">The character to use for indentation. Defaults to a space character if not specified.</param>
    /// <param name="values">An array of <see cref="ReadOnlySpan{T}"/> to join and append. Each <see cref="object"/> is converted to its <see cref="string"/> representation.</param>
    /// <returns>The current instance of the <see cref="PooledStringBuilder"/>, enabling method chaining.</returns>
    public PooledStringBuilder AppendIndentedJoin(char separator, int indentationLevel, char indentationChar, params ReadOnlySpan<object?> values)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(indentationLevel);

        StringBuilder builder = StringBuilder;
        _ = builder.Append(indentationChar, indentationLevel)
            .AppendJoin(separator, values);
        return this;
    }

    /// <summary>
    /// Appends the specified values to the builder, separated by the specified character, with the current indentation
    /// applied.
    /// </summary>
    /// <remarks>The method applies the current indentation settings before appending the joined values. If
    /// custom indentation settings are not specified, default values are used.</remarks>
    /// <param name="separator">The character to use as a separator between each value.</param>
    /// <param name="values">A span of objects to append to the builder, separated by the specified character.</param>
    /// <returns>The current instance of <see cref="PooledStringBuilder"/> with the appended values.</returns>
    public PooledStringBuilder AppendIndentedJoin(char separator, params ReadOnlySpan<object?> values)
    {
        _ = Indent()
            .AppendJoin(separator, values);
        return this;
    }

    public PooledStringBuilder AppendJoin(char separator, params ReadOnlySpan<string?> values)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.AppendJoin(separator, values);
        return this;
    }

    /// <summary>
    /// Appends the specified values to the current instance, joined by a separator and preceded by a repeated
    /// indentation character.
    /// </summary>
    /// <remarks>This method is useful for formatting structured text output with indentation, such as
    /// generating code or hierarchical data representations.</remarks>
    /// <param name="separator">The character to use as a separator between the values. If null, no separator is inserted.</param>
    /// <param name="indentationLevel">The number of times to repeat the indentation character. Must be zero or greater.</param>
    /// <param name="indentationChar">The character to use for indentation. Defaults to a space character if not specified.</param>
    /// <param name="values">An array of <see cref="ReadOnlySpan{T}"/> to join and append.</param>
    /// <returns>The current instance of the <see cref="PooledStringBuilder"/>, enabling method chaining.</returns>
    public PooledStringBuilder AppendIndentedJoin(char separator, int indentationLevel, char indentationChar, params ReadOnlySpan<string?> values)
    {
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(indentationLevel);

        StringBuilder builder = StringBuilder;
        _ = builder.Append(indentationChar, indentationLevel)
            .AppendJoin(separator, values);
        return this;
    }

    /// <summary>
    /// Appends the specified string values to the builder, separated by the specified character, with the current
    /// indentation applied.
    /// </summary>
    /// <remarks>The method applies the current indentation settings before appending the joined values. If
    /// custom indentation settings are not specified, default values are used.</remarks>
    /// <param name="separator">The character to use as a separator between each value.</param>
    /// <param name="values">A span of string values to append, each separated by the specified character. Null values are treated as empty
    /// strings.</param>
    /// <returns>The current instance of <see cref="PooledStringBuilder"/> with the appended indented and joined values.</returns>
    public PooledStringBuilder AppendIndentedJoin(char separator, params ReadOnlySpan<string?> values)
    {
        _ = Indent()
            .AppendJoin(separator, values);
        return this;
    }

#endif

    // ============================================================================
    // APPENDFORMAT METHODS - String format and CompositeFormat
    // ============================================================================

    public PooledStringBuilder AppendFormat([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.AppendFormat(format, arg0);
        return this;
    }

    public PooledStringBuilder AppendFormat([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0, object? arg1)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.AppendFormat(format, arg0, arg1);
        return this;
    }

    public PooledStringBuilder AppendFormat([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0, object? arg1, object? arg2)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.AppendFormat(format, arg0, arg1, arg2);
        return this;
    }

    public PooledStringBuilder AppendFormat([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, params object?[] args)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.AppendFormat(format, args);
        return this;
    }

    public PooledStringBuilder AppendFormat(IFormatProvider? provider, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.AppendFormat(provider, format, arg0);
        return this;
    }

    public PooledStringBuilder AppendFormat(IFormatProvider? provider, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0, object? arg1)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.AppendFormat(provider, format, arg0, arg1);
        return this;
    }

    public PooledStringBuilder AppendFormat(IFormatProvider? provider, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0, object? arg1, object? arg2)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.AppendFormat(provider, format, arg0, arg1, arg2);
        return this;
    }

    public PooledStringBuilder AppendFormat(IFormatProvider? provider, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, params object?[] args)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.AppendFormat(provider, format, args);
        return this;
    }

#if NET9_0_OR_GREATER

    public PooledStringBuilder AppendFormat([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, params ReadOnlySpan<object?> args)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.AppendFormat(format, args);
        return this;
    }

    public PooledStringBuilder AppendFormat(IFormatProvider? provider, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, params ReadOnlySpan<object?> args)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.AppendFormat(provider, format, args);
        return this;
    }

#endif

    public PooledStringBuilder AppendFormat<TArg0>(IFormatProvider? provider, CompositeFormat format, TArg0 arg0)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.AppendFormat(provider, format, arg0);
        return this;
    }

    public PooledStringBuilder AppendFormat<TArg0, TArg1>(IFormatProvider? provider, CompositeFormat format, TArg0 arg0, TArg1 arg1)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.AppendFormat(provider, format, arg0, arg1);
        return this;
    }

    public PooledStringBuilder AppendFormat<TArg0, TArg1, TArg2>(IFormatProvider? provider, CompositeFormat format, TArg0 arg0, TArg1 arg1, TArg2 arg2)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.AppendFormat(provider, format, arg0, arg1, arg2);
        return this;
    }

    public PooledStringBuilder AppendFormat(IFormatProvider? provider, CompositeFormat format, params object?[] args)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.AppendFormat(provider, format, args);
        return this;
    }

#if NET9_0_OR_GREATER

    public PooledStringBuilder AppendFormat(IFormatProvider? provider, CompositeFormat format, params ReadOnlySpan<object?> args)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.AppendFormat(provider, format, args);
        return this;
    }

#endif

    // ============================================================================
    // INSERT METHODS
    // ============================================================================

    public PooledStringBuilder Insert(int index, string? value)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Insert(index, value);
        return this;
    }

    public PooledStringBuilder Insert(int index, string? value, int count)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Insert(index, value, count);
        return this;
    }

    public PooledStringBuilder Insert(int index, bool value)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Insert(index, value);
        return this;
    }

    public PooledStringBuilder Insert(int index, sbyte value)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Insert(index, value);
        return this;
    }

    public PooledStringBuilder Insert(int index, byte value)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Insert(index, value);
        return this;
    }

    public PooledStringBuilder Insert(int index, short value)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Insert(index, value);
        return this;
    }

    public PooledStringBuilder Insert(int index, char value)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Insert(index, value);
        return this;
    }

    public PooledStringBuilder Insert(int index, char[]? value)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Insert(index, value);
        return this;
    }

    public PooledStringBuilder Insert(int index, string? value, int startIndex, int count)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Insert(index, value?[startIndex..(startIndex + count)]);
        return this;
    }

    public PooledStringBuilder Insert(int index, char[]? value, int startIndex, int charCount)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Insert(index, value, startIndex, charCount);
        return this;
    }

    public PooledStringBuilder Insert(int index, int value)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Insert(index, value);
        return this;
    }

    public PooledStringBuilder Insert(int index, long value)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Insert(index, value);
        return this;
    }

    public PooledStringBuilder Insert(int index, float value)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Insert(index, value);
        return this;
    }

    public PooledStringBuilder Insert(int index, double value)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Insert(index, value);
        return this;
    }

    public PooledStringBuilder Insert(int index, decimal value)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Insert(index, value);
        return this;
    }

    public PooledStringBuilder Insert(int index, ushort value)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Insert(index, value);
        return this;
    }

    public PooledStringBuilder Insert(int index, uint value)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Insert(index, value);
        return this;
    }

    public PooledStringBuilder Insert(int index, ulong value)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Insert(index, value);
        return this;
    }

    public PooledStringBuilder Insert(int index, object? value)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Insert(index, value);
        return this;
    }

    public PooledStringBuilder Insert(int index, ReadOnlySpan<char> value)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Insert(index, value);
        return this;
    }

    // ============================================================================
    // REMOVE & REPLACE
    // ============================================================================

    public PooledStringBuilder Remove(int startIndex, int length)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Remove(startIndex, length);
        return this;
    }

    public PooledStringBuilder Replace(string oldValue, string? newValue)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Replace(oldValue, newValue);
        return this;
    }

    public PooledStringBuilder Replace(string oldValue, string? newValue, int startIndex, int count)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Replace(oldValue, newValue, startIndex, count);
        return this;
    }

    public PooledStringBuilder Replace(char oldChar, char newChar)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Replace(oldChar, newChar);
        return this;
    }

    public PooledStringBuilder Replace(char oldChar, char newChar, int startIndex, int count)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Replace(oldChar, newChar, startIndex, count);
        return this;
    }

#if NET9_0_OR_GREATER

    public PooledStringBuilder Replace(ReadOnlySpan<char> oldValue, ReadOnlySpan<char> newValue)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Replace(oldValue, newValue);
        return this;
    }

    public PooledStringBuilder Replace(ReadOnlySpan<char> oldValue, ReadOnlySpan<char> newValue, int startIndex, int count)
    {
        StringBuilder builder = StringBuilder;
        _ = builder.Replace(oldValue, newValue, startIndex, count);
        return this;
    }

#endif

    // ============================================================================
    // COPY
    // ============================================================================

    public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
    {
        StringBuilder builder = StringBuilder;
        builder.CopyTo(sourceIndex, destination, destinationIndex, count);
    }

    public void CopyTo(int sourceIndex, Span<char> destination, int count)
    {
        StringBuilder builder = StringBuilder;
        builder.CopyTo(sourceIndex, destination, count);
    }

    // ============================================================================
    // EQUALS
    // ============================================================================

    public bool Equals([NotNullWhen(true)] StringBuilder? sb)
    {
        StringBuilder builder = StringBuilder;
        return builder.Equals(sb);
    }

    public bool Equals(ReadOnlySpan<char> span)
    {
        StringBuilder builder = StringBuilder;
        return builder.Equals(span);
    }

    // ----------------------------
    // Lifetime / pooling
    // ----------------------------

    public void Recycle()
    {
        if (_stringBuilder is null)
        {
            return;
        }

        StringBuilderFactory.Recycle(_stringBuilder);
        _stringBuilder = null;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!IsDisposed)
        {
            if (disposing)
            {
                Recycle();
            }
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private StringBuilder Indent()
    {
        (int indentationLevel, char indentationChar) = CurrentIndentationInfo;
        StringBuilder builder = StringBuilder;
        _ = builder.Append(indentationChar, indentationLevel);

        return builder;
    }

    // Nested handler that wraps StringBuilder's handler
    [InterpolatedStringHandler]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Interpolated-string handler requires access to the containing instance's internals like the private static StringBuilder; this nested public ref struct is intentional.")]
    public ref struct AppendInterpolatedStringHandler
    {
        private StringBuilder.AppendInterpolatedStringHandler _inner;
        private readonly char _indentationChar;
        private readonly int _indentationLevel;
        private readonly int _repeatCount;
        private readonly StringBuilder? _buffer;

        public bool IsBuffered => _repeatCount != 1;

        // Constructor for WriteTo($"...")
        public AppendInterpolatedStringHandler(
            int literalLength,
            int formattedCount,
            PooledStringBuilder pooledBuilder) : this(
                literalLength,
                formattedCount,
                pooledBuilder,
                provider: null,
                indentationChar: DefaultIndentationChar,
                indentationLevel: 0,
                repeatCount: 1)
        {
        }

        // Constructor for WriteTo(repeatCount, $"...")
        public AppendInterpolatedStringHandler(
            int literalLength,
            int formattedCount,
            PooledStringBuilder pooledBuilder,
            int repeatCount) : this(
                literalLength,
                formattedCount,
                pooledBuilder,
                provider: null,
                indentationChar: DefaultIndentationChar,
                indentationLevel: 0,
                repeatCount)
        {
        }

        // Constructor for WriteTo(provider, $"...")
        public AppendInterpolatedStringHandler(
            int literalLength,
            int formattedCount,
            PooledStringBuilder pooledBuilder,
            IFormatProvider? provider) : this(
                literalLength,
                formattedCount,
                pooledBuilder,
                provider,
                indentationChar: DefaultIndentationChar,
                indentationLevel: 0,
                repeatCount: 1)
        {
        }

        // Constructor for WriteTo(provider, repeatCount, $"...")
        public AppendInterpolatedStringHandler(
            int literalLength,
            int formattedCount,
            PooledStringBuilder pooledBuilder,
            IFormatProvider? provider,
            int repeatCount) : this(
                literalLength,
                formattedCount,
                pooledBuilder,
                provider,
                indentationChar: '\0',
                indentationLevel: 0,
                repeatCount)
        {
        }

        // Constructor for WriteTo(provider, indentationChar, indentationLevel, $"...")
        public AppendInterpolatedStringHandler(
            int literalLength,
            int formattedCount,
            PooledStringBuilder pooledBuilder,
            IFormatProvider? provider,
            char indentationChar,
            int indentationLevel) : this(
                literalLength,
                formattedCount,
                pooledBuilder,
                provider,
                indentationChar,
                indentationLevel,
                repeatCount: 1)
        {
        }

        // Constructor for WriteTo(indentationChar, indentationLevel, $"...")
        public AppendInterpolatedStringHandler(
            int literalLength,
            int formattedCount,
            PooledStringBuilder pooledBuilder,
            char indentationChar,
            int indentationLevel) : this(
                literalLength,
                formattedCount,
                pooledBuilder,
                provider: null,
                indentationChar,
                indentationLevel,
                repeatCount: 1)
        {
        }

        // Constructor for WriteTo(indentationChar, indentationLevel, repeatCount, $"...")
        public AppendInterpolatedStringHandler(
            int literalLength,
            int formattedCount,
            PooledStringBuilder pooledBuilder,
            char indentationChar,
            int indentationLevel,
            int repeatCount) : this(
                literalLength,
                formattedCount,
                pooledBuilder,
                provider: null,
                indentationChar,
                indentationLevel,
                repeatCount)
        {
        }

        // Constructor for WriteTo(provider, indentationChar, indentationLevel, repeatCount, $"...")
        public AppendInterpolatedStringHandler(
            int literalLength,
            int formattedCount,
            PooledStringBuilder pooledBuilder,
            IFormatProvider? provider,
            char indentationChar,
            int indentationLevel,
            int repeatCount)
        {
            ArgumentNullException.ThrowIfNull(pooledBuilder);
            ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(indentationLevel);
            ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(repeatCount);
            _indentationChar = indentationChar;
            _indentationLevel = indentationLevel;
            _repeatCount = repeatCount;

            StringBuilder capturedOrBufferStringBuilder;
            if (IsBuffered)
            {
                _buffer = new StringBuilder(Math.Max(literalLength, 16));
                capturedOrBufferStringBuilder = _buffer;
            }
            else
            {
                _buffer = null;
                capturedOrBufferStringBuilder = pooledBuilder.StringBuilder;

                // If not buffering, we need to append the indentation before the interpolated content
                if (indentationLevel > 0)
                {
                    _ = capturedOrBufferStringBuilder.Append(indentationChar, indentationLevel);
                }
            }

            _inner = new StringBuilder.AppendInterpolatedStringHandler(
                literalLength,
                formattedCount,
                capturedOrBufferStringBuilder,
                provider);
        }

        public PooledStringBuilder WriteTo(PooledStringBuilder pooledBuilder) => WriteTo(pooledBuilder, _indentationLevel, _indentationChar);

        public PooledStringBuilder WriteTo(PooledStringBuilder pooledBuilder, int indentationLevel, char indentationChar)
        {
            ArgumentNullException.ThrowIfNull(pooledBuilder);
            ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(indentationLevel);

            if (!IsBuffered)
            {
                return pooledBuilder;
            }

            for (int i = 0; i < _repeatCount; i++)
            {
                _ = indentationLevel > 0
                    ? pooledBuilder.AppendIndented(_buffer, indentationLevel, indentationChar)
                    : pooledBuilder.Append(_buffer);
            }

            return pooledBuilder;
        }

        public PooledStringBuilder WriteLineTo(PooledStringBuilder pooledBuilder) => WriteLineTo(pooledBuilder, _indentationLevel, _indentationChar);

        public PooledStringBuilder WriteLineTo(PooledStringBuilder pooledBuilder, int indentationLevel, char indentationChar)
        {
            ArgumentNullException.ThrowIfNull(pooledBuilder);
            ArgumentOutOfRangeExceptionAdvanced.ThrowIfNegative(indentationLevel);

            if (!IsBuffered)
            {
                return pooledBuilder;
            }

            for (int i = 0; i < _repeatCount; i++)
            {
                _ = indentationLevel > 0
                    ? pooledBuilder.AppendIndentedLine(_buffer!, indentationLevel, indentationChar)
                    : pooledBuilder.AppendLine(_buffer!);
            }

            return pooledBuilder;
        }

        // Forward all calls to the inner handler
        #region Compiler contract methods
        /*
         * These are called by the compiler when processing the interpolated string 
         * and must be implemented to forward to the inner handler
         */

        public void AppendLiteral(string value) => _inner.AppendLiteral(value);
        public void AppendFormatted<T>(T value) => _inner.AppendFormatted(value);
        public void AppendFormatted<T>(T value, string? format) => _inner.AppendFormatted(value, format);
        public void AppendFormatted<T>(T value, int alignment) => _inner.AppendFormatted(value, alignment);
        public void AppendFormatted<T>(T value, int alignment, string? format) => _inner.AppendFormatted(value, alignment, format);
        public void AppendFormatted(ReadOnlySpan<char> value) => _inner.AppendFormatted(value);
        public void AppendFormatted(ReadOnlySpan<char> value, int alignment = 0, string? format = null) => _inner.AppendFormatted(value, alignment, format);
        public void AppendFormatted(string? value) => _inner.AppendFormatted(value);
        public void AppendFormatted(string? value, int alignment = 0, string? format = null) => _inner.AppendFormatted(value, alignment, format);
        public void AppendFormatted(object? value, int alignment = 0, string? format = null) => _inner.AppendFormatted(value, alignment, format);
        #endregion Compiler contract methods
    }
}