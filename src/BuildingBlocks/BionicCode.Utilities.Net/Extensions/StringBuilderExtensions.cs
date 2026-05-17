namespace BionicCode.Utilities.Net;

using System;
using System.Runtime.CompilerServices;
using System.Text;

public static class StringBuilderExtensions
{
    #region StringBuilder

    /// <summary>
    /// Appends the specified string a specified number of times to this instance of <see cref="StringBuilder"/>.
    /// </summary>
    /// <param name="stringBuilder">The <see cref="StringBuilder"/> instance to append to.</param>
    /// <param name="value">The string to append.</param>
    /// <param name="count">The number of times to append the string. Must be greater than or equal to 0.</param>
    /// <returns>The modified <see cref="StringBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="stringBuilder"/> or <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="count"/> is less than 0.</exception>
    public static StringBuilder Append(this StringBuilder stringBuilder, string value, int count)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(stringBuilder);
        ArgumentNullExceptionAdvanced.ThrowIfNull(value);
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfLessThan(count, 0);

        if (string.IsNullOrWhiteSpace(value)
            || count < 1)
        {
            return stringBuilder;
        }

        for (int i = 0; i < count; i++)
        {
            _ = stringBuilder.Append([.. value]);
        }

        return stringBuilder;
    }

    /// <summary>
    /// Appends the specified string a specified number of times to this instance of <see cref="StringBuilder"/>.
    /// </summary>
    /// <param name="stringBuilder">The <see cref="StringBuilder"/> instance to append to.</param>
    /// <param name="value">The string to append.</param>
    /// <param name="count">The number of times to append the string. Must be greater than or equal to 0.</param>
    /// <returns>The modified <see cref="StringBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="stringBuilder"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="count"/> is less than 0.</exception>
    public static StringBuilder Append(this StringBuilder stringBuilder, ReadOnlySpan<char> value, int count)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(stringBuilder);
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfLessThan(count, 0);

        if (value.IsEmpty
            || count < 1)
        {
            return stringBuilder;
        }

        for (int i = 0; i < count; i++)
        {
            _ = stringBuilder.Append(value);
        }

        return stringBuilder;
    }
    #endregion StringBuilder

    #region PooledStringBuilder
    /// <summary>
    /// Appends the specified string a specified number of times to this instance of <see cref="PooledStringBuilder"/>.
    /// </summary>
    /// <param name="stringBuilder">The <see cref="PooledStringBuilder"/> instance to append to.</param>
    /// <param name="value">The string to append.</param>
    /// <param name="count">The number of times to append the string. Must be greater than or equal to 0.</param>
    /// <returns>The modified <see cref="PooledStringBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="stringBuilder"/> or <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="count"/> is less than 0.</exception>
    public static PooledStringBuilder Append(this PooledStringBuilder stringBuilder, string value, int count)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(stringBuilder);
        ArgumentNullExceptionAdvanced.ThrowIfNull(value);
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfLessThan(count, 0);

        if (string.IsNullOrWhiteSpace(value)
            || count < 1)
        {
            return stringBuilder;
        }

        for (int i = 0; i < count; i++)
        {
            _ = stringBuilder.Append([.. value]);
        }

        return stringBuilder;
    }

    /// <summary>
    /// Appends the specified string a specified number of times to this instance of <see cref="PooledStringBuilder"/>.
    /// </summary>
    /// <param name="stringBuilder">The <see cref="PooledStringBuilder"/> instance to append to.</param>
    /// <param name="value">The string to append.</param>
    /// <param name="count">The number of times to append the string. Must be greater than or equal to 0.</param>
    /// <returns>The modified <see cref="PooledStringBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="stringBuilder"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="count"/> is less than 0.</exception>
    public static PooledStringBuilder Append(this PooledStringBuilder stringBuilder, ReadOnlySpan<char> value, int count)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(stringBuilder);
        ArgumentOutOfRangeExceptionAdvanced.ThrowIfLessThan(count, 0);

        if (value.IsEmpty
            || count < 1)
        {
            return stringBuilder;
        }

        for (int i = 0; i < count; i++)
        {
            _ = stringBuilder.Append(value);
        }

        return stringBuilder;
    }
    #endregion PooledStringBuilder
}
