namespace BionicCode.Utilities.Net;

using System.Diagnostics.CodeAnalysis;
using BionicCode.Utilities.Net;

public readonly record struct FileExtension
{
    private readonly string? _value;

    public string Value => _value
        ?? throw new InvalidOperationException("The default FileExtension value is not valid.");

    private FileExtension(string value) => _value = value;

    /// <summary>
    /// Creates a <see cref="FileExtension"/> from a raw extension value with mandatory leading dot.
    /// </summary>
    /// <param name="value">The raw extension with a leading dot.</param>
    /// <returns>A normalized file extension that always starts with a dot.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value"/> is not a single extension token:
    /// it must start with '.', contain at least one character after the dot,
    /// and must not contain whitespace, control characters, path separators, or another dot.
    /// </exception>
    public static FileExtension FromExtensionToken(string value)
    {
        ArgumentExceptionAdvanced.ThrowIfFalse(IsFileExtension(value), $"The argument '{nameof(value)}' must be a valid file extension token starting with a dot and containing no whitespace or path separator characters.", nameof(value));

        return FromResolvedExtensionCandidate(value, nameof(value));
    }

    /// <summary>
    /// Creates a <see cref="FileExtension"/> from the extension of the specified <see cref="FileInfo"/>.
    /// </summary>
    /// <param name="fileInfo">The file info instance to read the extension from.</param>
    /// <returns>A normalized file extension that always starts with a dot.</returns>
    public static FileExtension FromFileInfo(FileInfo fileInfo)
    {
        ArgumentNullException.ThrowIfNull(fileInfo);

        return FromFileName(fileInfo.Name);
    }

    /// <summary>
    /// Creates a <see cref="FileExtension"/> from the last extension token of a file path.
    /// Leading-dot filenames such as ".gitignore" are treated as extensionless.
    /// </summary>
    /// <param name="filePath">The file path to inspect.</param>
    /// <returns>A normalized file extension that always starts with a dot.</returns>
    public static FileExtension FromFilePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null, empty, or whitespace.", nameof(filePath));
        }

        return FromFileName(Path.GetFileName(filePath));
    }

    /// <summary>
    /// Creates a <see cref="FileExtension"/> from the last extension token of a file name.
    /// Leading-dot filenames such as ".gitignore" are treated as extensionless.
    /// </summary>
    /// <param name="fileName">The file name to inspect.</param>
    /// <returns>A normalized file extension that always starts with a dot.</returns>
    public static FileExtension FromFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name cannot be null, empty, or whitespace.", nameof(fileName));
        }

        if (!string.Equals(fileName, Path.GetFileName(fileName), StringComparison.Ordinal))
        {
            throw new ArgumentException("File name must not include directory information.", nameof(fileName));
        }

        if (fileName.LastIndexOf('.') <= 0)
        {
            throw new ArgumentException($"The argument '{nameof(fileName)}' looks like a file without an extension.", nameof(fileName));
        }

        return FromResolvedExtensionCandidate(Path.GetExtension(fileName), nameof(fileName));
    }

    /// <summary>
    /// Determines whether the specified value represents a single file extension token.
    /// </summary>
    /// <param name="value">The value to inspect.</param>
    /// <returns>
    /// <see langword="true"/> when <paramref name="value"/> starts with a dot and contains exactly one extension token
    /// without whitespace, control characters, or path separator characters; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// This method validates syntax only. It does not verify that the extension is known by any MIME type provider.
    /// </remarks>
    public static bool IsFileExtension([NotNullWhen(true)] string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        ReadOnlySpan<char> extensionCandidate = value.AsSpan().Trim();
        if (extensionCandidate.Length < 2 || extensionCandidate[0] != '.')
        {
            return false;
        }

        extensionCandidate = extensionCandidate[1..];
        if (extensionCandidate.Contains('.'))
        {
            return false;
        }
        Path.in
        foreach (char character in extensionCandidate)
        {
            if (char.IsWhiteSpace(character)
                || char.IsControl(character)
                || character == Path.DirectorySeparatorChar
                || character == Path.AltDirectorySeparatorChar
                || character == Path.VolumeSeparatorChar)
            {
                return false;
            }
        }

        return true;
    }

    public override string ToString() => Value;

    private static FileExtension FromResolvedExtensionCandidate(string? extension, string parameterName)
    {
        if (!IsFileExtension(extension))
        {
            throw new ArgumentException("The specified file name or path must include a valid file extension.", parameterName);
        }

        return new(NormalizeExtension(extension, parameterName));
    }

    private static string NormalizeExtension(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("File extension cannot be null, empty, or whitespace.", parameterName);
        }

        string trimmedValue = value.Trim();
        string normalizedValue = trimmedValue.StartsWith('.')
            ? trimmedValue
            : $".{trimmedValue}";

        return normalizedValue.ToLowerInvariant();
    }
}