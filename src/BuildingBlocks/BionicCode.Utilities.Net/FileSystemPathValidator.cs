namespace BionicCode.Utilities.Net;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

/// <summary>
/// Validates file-system path symbols using platform-native lexical rules plus normal
/// application-level path policy.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="FileSystemPathValidator"/> validates whether a string is shaped like a normal
/// file-system path or name for the current platform. It intentionally performs lexical validation
/// only. It does not resolve relative paths, normalize path text, expand the current working
/// directory, check whether the target exists, check whether the parent directory exists, check
/// permissions, check path length limits, or check whether a consuming service supports the path.
/// </para>
/// <para>
/// Use <see cref="TryValidateFilePath"/> for a <c>FileDescriptor</c>-style contract, where the
/// input must end in a file-name segment. Use <see cref="TryValidateDirectoryPath"/> for a
/// <c>DirectoryDescriptor</c>-style contract, where root-only paths, <c>.</c>, <c>..</c>, and a
/// trailing directory separator are allowed.
/// </para>
/// <para>
/// Use <see cref="TryValidateFileName"/> and <see cref="TryValidateDirectoryName"/> when the input
/// must be a single name segment rather than a path.
/// </para>
/// <para>
/// Valid file path forms include, depending on the current platform:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>A plain file name, for example <c>report.csv</c>.</description>
///   </item>
///   <item>
///     <description>A relative file path, for example <c>exports/report.csv</c>, <c>exports\report.csv</c> on Windows, or <c>..\report.csv</c> on Windows.</description>
///   </item>
///   <item>
///     <description>An absolute file path, for example <c>/home/user/report.csv</c> on Unix-like systems or <c>C:\Temp\report.csv</c> on Windows.</description>
///   </item>
///   <item>
///     <description>A Windows drive-relative file path, for example <c>C:report.csv</c>. This is syntactically valid Windows input but remains relative to the current directory of the specified drive.</description>
///   </item>
///   <item>
///     <description>A Windows root-relative file path, for example <c>\Temp\report.csv</c>.</description>
///   </item>
///   <item>
///     <description>A Windows UNC file path, for example <c>\\server\share\report.csv</c>.</description>
///   </item>
/// </list>
/// <para>
/// Invalid normal path symbols include:
/// </para>
/// <list type="bullet">
///   <item>
///     <description><see langword="null"/>, empty, or whitespace-only input.</description>
///   </item>
///   <item>
///     <description>A file path that ends with a directory separator.</description>
///   </item>
///   <item>
///     <description>A file path whose final segment is empty or consists only of dots.</description>
///   </item>
///   <item>
///     <description>A path containing empty non-root segments caused by repeated separators.</description>
///   </item>
///   <item>
///     <description>A path segment that consists only of dots, except for <c>.</c> and <c>..</c> when they are used as directory-capable relative path symbols.</description>
///   </item>
///   <item>
///     <description>A path containing characters that are invalid for normal file-system path segments on the current platform.</description>
///   </item>
///   <item>
///     <description>On Windows, path segments using reserved device names such as <c>CON</c>, <c>PRN</c>, <c>AUX</c>, <c>NUL</c>, <c>COM1</c> through <c>COM9</c>, <c>COM¹</c>, <c>COM²</c>, <c>COM³</c>, <c>LPT1</c> through <c>LPT9</c>, <c>LPT¹</c>, <c>LPT²</c>, or <c>LPT³</c>, including the same reserved stem followed by an extension.</description>
///   </item>
///   <item>
///     <description>On Windows, path segments ending with a space or period.</description>
///   </item>
///   <item>
///     <description>On Windows, DOS device namespace paths such as <c>\\?\C:\Temp\file.txt</c> or <c>\\.\PhysicalDrive0</c>. These are valid Windows namespace forms, but they are intentionally rejected as non-normal application-level file paths.</description>
///   </item>
/// </list>
/// </remarks>
public static class FileSystemPathValidator
{
    /// <summary>
    /// Determines whether <paramref name="path"/> is a syntactically valid file path symbol for the
    /// current platform.
    /// </summary>
    /// <param name="path">The path text to validate.</param>
    /// <returns><see langword="true"/> if <paramref name="path"/> is a valid file path symbol; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidFilePath(string? path) => TryValidateFilePath(path, out _);

    /// <summary>
    /// Determines whether <paramref name="path"/> is a syntactically valid directory path symbol for
    /// the current platform.
    /// </summary>
    /// <param name="path">The path text to validate.</param>
    /// <returns><see langword="true"/> if <paramref name="path"/> is a valid directory path symbol; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidDirectoryPath(string? path) => TryValidateDirectoryPath(path, out _);

    /// <summary>
    /// Determines whether <paramref name="path"/> is a syntactically valid file-system path symbol for
    /// the current platform.
    /// </summary>
    /// <param name="path">The path text to validate.</param>
    /// <returns><see langword="true"/> if <paramref name="path"/> is a valid file-system path symbol; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidFileOrDirectoryPath(string? path) => TryValidateFileOrDirectoryPath(path, out _);

    /// <summary>
    /// Determines whether <paramref name="name"/> is a syntactically valid file name segment for the
    /// current platform.
    /// </summary>
    /// <param name="name">The file name text to validate.</param>
    /// <returns><see langword="true"/> if <paramref name="name"/> is a valid file name segment; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidFileName(string? name) => TryValidateFileName(name, out _);

    /// <summary>
    /// Determines whether <paramref name="name"/> is a syntactically valid directory name segment for
    /// the current platform.
    /// </summary>
    /// <param name="name">The directory name text to validate.</param>
    /// <returns><see langword="true"/> if <paramref name="name"/> is a valid directory name segment; otherwise, <see langword="false"/>.</returns>
    public static bool IsValidDirectoryName(string? name) => TryValidateDirectoryName(name, out _);

    /// <summary>
    /// Validates that <paramref name="path"/> is a syntactically valid file path symbol for the
    /// current platform.
    /// </summary>
    /// <param name="path">The path text to validate.</param>
    /// <param name="errorMessage">The validation error when the method returns <see langword="false"/>; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="path"/> is a valid file path symbol; otherwise, <see langword="false"/>.</returns>
    public static bool TryValidateFilePath(
        [NotNullWhen(true)] string? path,
        [NotNullWhen(false)] out string? errorMessage) => TryValidate(path, FileSystemPathKind.File, out errorMessage);

    /// <summary>
    /// Throws when <paramref name="path"/> is not a syntactically valid file path symbol for the
    /// current platform.
    /// </summary>
    /// <param name="path">The path text to validate.</param>
    /// <param name="paramName">The argument expression used as the exception parameter name.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="path"/> is not a valid file path symbol. This includes:
    /// <list type="bullet">
    ///   <item><description><paramref name="path"/> is <see langword="null"/>, empty, or whitespace-only.</description></item>
    ///   <item><description><paramref name="path"/> ends with a directory separator.</description></item>
    ///   <item><description><paramref name="path"/> is root-only or does not contain a valid file-name leaf segment.</description></item>
    ///   <item><description><paramref name="path"/> contains malformed path syntax or invalid platform-specific path characters.</description></item>
    /// </list>
    /// </exception>
    public static void ThrowIfInvalidFilePath(
        [NotNull] string? path,
        [CallerArgumentExpression(nameof(path))] string? paramName = null)
    {
        if (!TryValidateFilePath(path, out string? errorMessage))
        {
            throw new ArgumentException(errorMessage, paramName ?? nameof(path));
        }
    }

    /// <summary>
    /// Validates that <paramref name="name"/> is a syntactically valid file name segment for the
    /// current platform.
    /// </summary>
    /// <param name="name">The file name text to validate. The value must be one name segment and must not include directory separators, root syntax, drive prefixes, or UNC prefixes.</param>
    /// <param name="errorMessage">The validation error when the method returns <see langword="false"/>; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="name"/> is a valid file name segment; otherwise, <see langword="false"/>.</returns>
    public static bool TryValidateFileName(
        [NotNullWhen(true)] string? name,
        [NotNullWhen(false)] out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            errorMessage = "A file name must not be null, empty, or whitespace.";
            return false;
        }

        ReadOnlySpan<char> nameSpan = name.AsSpan();

        if (!TryValidateSingleNameShape(nameSpan, "file", out errorMessage))
        {
            return false;
        }

        if (IsAllDots(nameSpan))
        {
            errorMessage = "A file name must not consist only of dots.";
            return false;
        }

        return OperatingSystem.IsWindows()
            ? TryValidateWindowsNameSegment(nameSpan, "file", out errorMessage)
            : TryValidateUnixNameSegment(nameSpan, "file", out errorMessage);
    }

    /// <summary>
    /// Throws when <paramref name="name"/> is not a syntactically valid file name segment for the
    /// current platform.
    /// </summary>
    /// <param name="name">The file name text to validate.</param>
    /// <param name="paramName">The argument expression used as the exception parameter name.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="name"/> is not a valid file name segment. This includes:
    /// <list type="bullet">
    ///   <item><description><paramref name="name"/> is <see langword="null"/>, empty, or whitespace-only.</description></item>
    ///   <item><description><paramref name="name"/> contains directory separators, root syntax, drive prefixes, or UNC prefixes.</description></item>
    ///   <item><description><paramref name="name"/> consists only of dots.</description></item>
    ///   <item><description><paramref name="name"/> contains invalid platform-specific name characters.</description></item>
    /// </list>
    /// </exception>
    public static void ThrowIfInvalidFileName(
        [NotNull] string? name,
        [CallerArgumentExpression(nameof(name))] string? paramName = null)
    {
        if (!TryValidateFileName(name, out string? errorMessage))
        {
            throw new ArgumentException(errorMessage, paramName ?? nameof(name));
        }
    }

    /// <summary>
    /// Validates that <paramref name="path"/> is a syntactically valid directory path symbol for the
    /// current platform.
    /// </summary>
    /// <param name="path">The path text to validate.</param>
    /// <param name="errorMessage">The validation error when the method returns <see langword="false"/>; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="path"/> is a valid directory path symbol; otherwise, <see langword="false"/>.</returns>
    public static bool TryValidateDirectoryPath(
        [NotNullWhen(true)] string? path,
        [NotNullWhen(false)] out string? errorMessage) => TryValidate(path, FileSystemPathKind.Directory, out errorMessage);

    /// <summary>
    /// Throws when <paramref name="path"/> is not a syntactically valid directory path symbol for the
    /// current platform.
    /// </summary>
    /// <param name="path">The path text to validate.</param>
    /// <param name="paramName">The argument expression used as the exception parameter name.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="path"/> is not a valid directory path symbol. This includes:
    /// <list type="bullet">
    ///   <item><description><paramref name="path"/> is <see langword="null"/>, empty, or whitespace-only.</description></item>
    ///   <item><description><paramref name="path"/> contains malformed path syntax or empty non-root segments.</description></item>
    ///   <item><description><paramref name="path"/> contains invalid platform-specific path characters.</description></item>
    ///   <item><description><paramref name="path"/> contains an all-dot name segment other than <c>.</c> or <c>..</c>.</description></item>
    /// </list>
    /// </exception>
    public static void ThrowIfInvalidDirectoryPath(
        [NotNull] string? path,
        [CallerArgumentExpression(nameof(path))] string? paramName = null)
    {
        if (!TryValidateDirectoryPath(path, out string? errorMessage))
        {
            throw new ArgumentException(errorMessage, paramName ?? nameof(path));
        }
    }

    /// <summary>
    /// Validates that <paramref name="name"/> is a syntactically valid directory name segment for the
    /// current platform.
    /// </summary>
    /// <param name="name">The directory name text to validate. The value must be one name segment and must not include directory separators, root syntax, drive prefixes, or UNC prefixes.</param>
    /// <param name="errorMessage">The validation error when the method returns <see langword="false"/>; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="name"/> is a valid directory name segment; otherwise, <see langword="false"/>.</returns>
    public static bool TryValidateDirectoryName(
        [NotNullWhen(true)] string? name,
        [NotNullWhen(false)] out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            errorMessage = "A directory name must not be null, empty, or whitespace.";
            return false;
        }

        ReadOnlySpan<char> nameSpan = name.AsSpan();

        if (!TryValidateSingleNameShape(nameSpan, "directory", out errorMessage))
        {
            return false;
        }

        if (IsAllDots(nameSpan))
        {
            errorMessage = "A directory name must not consist only of dots.";
            return false;
        }

        return OperatingSystem.IsWindows()
            ? TryValidateWindowsNameSegment(nameSpan, "directory", out errorMessage)
            : TryValidateUnixNameSegment(nameSpan, "directory", out errorMessage);
    }

    /// <summary>
    /// Throws when <paramref name="name"/> is not a syntactically valid directory name segment for the
    /// current platform.
    /// </summary>
    /// <param name="name">The directory name text to validate.</param>
    /// <param name="paramName">The argument expression used as the exception parameter name.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="name"/> is not a valid directory name segment. This includes:
    /// <list type="bullet">
    ///   <item><description><paramref name="name"/> is <see langword="null"/>, empty, or whitespace-only.</description></item>
    ///   <item><description><paramref name="name"/> contains directory separators, root syntax, drive prefixes, or UNC prefixes.</description></item>
    ///   <item><description><paramref name="name"/> consists only of dots.</description></item>
    ///   <item><description><paramref name="name"/> contains invalid platform-specific name characters.</description></item>
    /// </list>
    /// </exception>
    public static void ThrowIfInvalidDirectoryName(
        [NotNull] string? name,
        [CallerArgumentExpression(nameof(name))] string? paramName = null)
    {
        if (!TryValidateDirectoryName(name, out string? errorMessage))
        {
            throw new ArgumentException(errorMessage, paramName ?? nameof(name));
        }
    }

    /// <summary>
    /// Validates that <paramref name="path"/> is a syntactically valid file-system path symbol for the
    /// current platform, without requiring file-like or directory-like leaf semantics.
    /// </summary>
    /// <param name="path">The path text to validate.</param>
    /// <param name="errorMessage">The validation error when the method returns <see langword="false"/>; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="path"/> is a valid file-system path symbol; otherwise, <see langword="false"/>.</returns>
    public static bool TryValidateFileOrDirectoryPath(
        [NotNullWhen(true)] string? path,
        [NotNullWhen(false)] out string? errorMessage) => TryValidate(path, FileSystemPathKind.FileOrDirectory, out errorMessage);

    /// <summary>
    /// Throws when <paramref name="path"/> is not a syntactically valid file-system path symbol for the
    /// current platform.
    /// </summary>
    /// <param name="path">The path text to validate.</param>
    /// <param name="paramName">The argument expression used as the exception parameter name.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="path"/> is not a valid file-system path symbol. This includes:
    /// <list type="bullet">
    ///   <item><description><paramref name="path"/> is <see langword="null"/>, empty, or whitespace-only.</description></item>
    ///   <item><description><paramref name="path"/> contains malformed path syntax or empty non-root segments.</description></item>
    ///   <item><description><paramref name="path"/> contains invalid platform-specific path characters.</description></item>
    ///   <item><description><paramref name="path"/> contains an all-dot name segment other than <c>.</c> or <c>..</c>.</description></item>
    /// </list>
    /// </exception>
    public static void ThrowIfInvalidFileOrDirectoryPath(
        [NotNull] string? path,
        [CallerArgumentExpression(nameof(path))] string? paramName = null)
    {
        if (!TryValidateFileOrDirectoryPath(path, out string? errorMessage))
        {
            throw new ArgumentException(errorMessage, paramName ?? nameof(path));
        }
    }

    /// <summary>
    /// Validates that <paramref name="path"/> is a syntactically valid file-system path symbol for the
    /// current platform and the requested <paramref name="kind"/>.
    /// </summary>
    /// <param name="path">The path text to validate.</param>
    /// <param name="kind">The descriptor-level path kind expected by the caller.</param>
    /// <param name="errorMessage">The validation error when the method returns <see langword="false"/>; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="path"/> is valid for <paramref name="kind"/>; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="kind"/> is not a defined <see cref="FileSystemPathKind"/> value.
    /// </exception>
    public static bool TryValidate(
        [NotNullWhen(true)] string? path,
        FileSystemPathKind kind,
        [NotNullWhen(false)] out string? errorMessage)
    {
        ArgumentExceptionAdvanced.ThrowIfEnumIsNotDefined<FileSystemPathKind>(
            kind,
            message: $"Unsupported file-system path kind: '{kind}'.");

        if (string.IsNullOrWhiteSpace(path))
        {
            errorMessage = "The path must not be null, empty, or whitespace.";
            return false;
        }

        return OperatingSystem.IsWindows()
            ? TryValidateWindowsPath(path.AsSpan(), kind, out errorMessage)
            : TryValidateUnixPath(path.AsSpan(), kind, out errorMessage);
    }

    private static bool TryValidateSingleNameShape(
        ReadOnlySpan<char> name,
        string nameKind,
        [NotNullWhen(false)] out string? errorMessage)
    {
        if (OperatingSystem.IsWindows())
        {
            if (name.Contains('\\') || name.Contains('/'))
            {
                errorMessage = $"A {nameKind} name must not include directory separators.";
                return false;
            }

            if (name.Length >= 2 && IsAsciiLetter(name[0]) && name[1] == ':')
            {
                errorMessage = $"A {nameKind} name must not include a drive-relative or drive-qualified prefix.";
                return false;
            }
        }
        else if (name.Contains('/'))
        {
            errorMessage = $"A {nameKind} name must not include directory separators.";
            return false;
        }

        errorMessage = null;
        return true;
    }

    private static bool TryValidateUnixNameSegment(
        ReadOnlySpan<char> segment,
        string nameKind,
        [NotNullWhen(false)] out string? errorMessage)
    {
        if (segment.Contains('\0'))
        {
            errorMessage = $"The {nameKind} name contains a NUL character.";
            return false;
        }

        errorMessage = null;
        return true;
    }

    private static bool TryValidateWindowsNameSegment(
        ReadOnlySpan<char> segment,
        string nameKind,
        [NotNullWhen(false)] out string? errorMessage)
    {
        foreach (char character in segment)
        {
            if (IsInvalidWindowsSegmentCharacter(character))
            {
                errorMessage = $"The {nameKind} name '{segment.ToString()}' contains an invalid character.";
                return false;
            }
        }

        if (segment[^1] is ' ' or '.')
        {
            errorMessage = $"The {nameKind} name '{segment.ToString()}' must not end with a space or period.";
            return false;
        }

        if (IsReservedWindowsDeviceName(segment))
        {
            errorMessage = $"The {nameKind} name '{segment.ToString()}' is a reserved Windows device name.";
            return false;
        }

        errorMessage = null;
        return true;
    }

    private static bool TryValidateUnixPath(
        ReadOnlySpan<char> path,
        FileSystemPathKind kind,
        [NotNullWhen(false)] out string? errorMessage)
    {
        if (path.Contains('\0'))
        {
            errorMessage = "The path contains a NUL character.";
            return false;
        }

        if (IsUnixRootOnly(path))
        {
            return TryValidateRootOnly(kind, out errorMessage);
        }

        bool hasTrailingSeparator = path[^1] == '/';

        if (hasTrailingSeparator && !AllowsTrailingSeparator(kind))
        {
            errorMessage = "A file path must not end with a directory separator.";
            return false;
        }

        if (hasTrailingSeparator && path.Length > 1 && path[^2] == '/')
        {
            errorMessage = "The path contains an empty path segment.";
            return false;
        }

        int effectiveLength = hasTrailingSeparator
            ? path.Length - 1
            : path.Length;

        int segmentStart = path[0] == '/'
            ? 1
            : 0;

        if (segmentStart == effectiveLength)
        {
            return TryValidateRootOnly(kind, out errorMessage);
        }

        for (int i = segmentStart; i <= effectiveLength; i++)
        {
            bool isEnd = i == effectiveLength;
            bool isSeparator = !isEnd && path[i] == '/';

            if (!isEnd && !isSeparator)
            {
                continue;
            }

            ReadOnlySpan<char> segment = path[segmentStart..i];

            if (segment.IsEmpty)
            {
                errorMessage = "The path contains an empty path segment.";
                return false;
            }

            bool isLeaf = isEnd;

            if (!TryValidateUnixPathSegment(segment, kind, isLeaf, out errorMessage))
            {
                return false;
            }

            segmentStart = i + 1;
        }

        errorMessage = null;
        return true;
    }

    private static bool TryValidateUnixPathSegment(
        ReadOnlySpan<char> segment,
        FileSystemPathKind kind,
        bool isLeaf,
        [NotNullWhen(false)] out string? errorMessage)
    {
        if (!IsAllDots(segment))
        {
            errorMessage = null;
            return true;
        }

        if (IsCurrentOrParentDirectorySegment(segment))
        {
            if (isLeaf && kind == FileSystemPathKind.File)
            {
                errorMessage = "A file path must end with a file-name segment, not '.' or '..'.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        errorMessage = $"The path segment '{segment.ToString()}' must not consist only of dots.";
        return false;
    }

    private static bool TryValidateWindowsPath(
        ReadOnlySpan<char> path,
        FileSystemPathKind kind,
        [NotNullWhen(false)] out string? errorMessage)
    {
        if (path.Contains('\0'))
        {
            errorMessage = "The path contains a NUL character.";
            return false;
        }

        int rootLength = GetWindowsRootLength(path, out errorMessage);

        if (rootLength < 0)
        {
            return false;
        }

        if (rootLength == path.Length)
        {
            return TryValidateRootOnly(kind, out errorMessage);
        }

        bool hasTrailingSeparator = IsWindowsDirectorySeparator(path[^1]);

        if (hasTrailingSeparator && !AllowsTrailingSeparator(kind))
        {
            errorMessage = "A file path must not end with a directory separator.";
            return false;
        }

        if (hasTrailingSeparator && path.Length > rootLength && IsWindowsDirectorySeparator(path[^2]))
        {
            errorMessage = "The path contains an empty path segment.";
            return false;
        }

        int effectiveLength = hasTrailingSeparator
            ? path.Length - 1
            : path.Length;

        if (rootLength == effectiveLength)
        {
            return TryValidateRootOnly(kind, out errorMessage);
        }

        int segmentStart = rootLength;

        for (int i = segmentStart; i <= effectiveLength; i++)
        {
            bool isEnd = i == effectiveLength;
            bool isSeparator = !isEnd && IsWindowsDirectorySeparator(path[i]);

            if (!isEnd && !isSeparator)
            {
                continue;
            }

            ReadOnlySpan<char> segment = path[segmentStart..i];

            if (segment.IsEmpty)
            {
                errorMessage = "The path contains an empty path segment.";
                return false;
            }

            bool isLeaf = isEnd;

            if (!TryValidateWindowsPathSegment(segment, kind, isLeaf, out errorMessage))
            {
                return false;
            }

            segmentStart = i + 1;
        }

        errorMessage = null;
        return true;
    }

    private static int GetWindowsRootLength(
        ReadOnlySpan<char> path,
        out string? errorMessage)
    {
        errorMessage = null;

        if (StartsWithWindowsDeviceNamespacePrefix(path))
        {
            errorMessage = "DOS device namespace paths are not accepted as normal application-level file paths.";
            return -1;
        }

        if (StartsWithUncPrefix(path))
        {
            int serverEnd = IndexOfNextWindowsSeparator(path, 2);

            if (serverEnd < 0)
            {
                errorMessage = "A UNC path must include a non-empty server name and a non-empty share name.";
                return -1;
            }

            if (serverEnd == 2)
            {
                errorMessage = "A UNC path must include a non-empty server name.";
                return -1;
            }

            ReadOnlySpan<char> serverName = path[2..serverEnd];

            if (!TryValidateWindowsUncRootSegment(serverName, "server", out errorMessage))
            {
                return -1;
            }

            int shareStart = serverEnd + 1;

            if (shareStart >= path.Length)
            {
                errorMessage = "A UNC path must include a non-empty share name.";
                return -1;
            }

            int shareEnd = IndexOfNextWindowsSeparator(path, shareStart);

            if (shareEnd < 0)
            {
                shareEnd = path.Length;
            }

            if (shareEnd == shareStart)
            {
                errorMessage = "A UNC path must include a non-empty share name.";
                return -1;
            }

            ReadOnlySpan<char> shareName = path[shareStart..shareEnd];

            if (!TryValidateWindowsUncRootSegment(shareName, "share", out errorMessage))
            {
                return -1;
            }

            return shareEnd < path.Length
                ? shareEnd + 1
                : shareEnd;
        }

        if (path.Length >= 2 && IsAsciiLetter(path[0]) && path[1] == ':')
        {
            return path.Length >= 3 && IsWindowsDirectorySeparator(path[2])
                ? 3
                : 2;
        }

        if (IsWindowsDirectorySeparator(path[0]))
        {
            return 1;
        }

        return 0;
    }

    private static bool TryValidateWindowsPathSegment(
        ReadOnlySpan<char> segment,
        FileSystemPathKind kind,
        bool isLeaf,
        [NotNullWhen(false)] out string? errorMessage)
    {
        if (IsCurrentOrParentDirectorySegment(segment))
        {
            if (isLeaf && kind == FileSystemPathKind.File)
            {
                errorMessage = "A file path must end with a file-name segment, not '.' or '..'.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        if (IsAllDots(segment))
        {
            errorMessage = $"The path segment '{segment.ToString()}' must not consist only of dots.";
            return false;
        }

        foreach (char character in segment)
        {
            if (IsInvalidWindowsSegmentCharacter(character))
            {
                errorMessage = $"The path segment '{segment.ToString()}' contains an invalid character.";
                return false;
            }
        }

        if (segment[^1] is ' ' or '.')
        {
            errorMessage = $"The path segment '{segment.ToString()}' must not end with a space or period.";
            return false;
        }

        if (IsReservedWindowsDeviceName(segment))
        {
            errorMessage = $"The path segment '{segment.ToString()}' is a reserved Windows device name.";
            return false;
        }

        errorMessage = null;
        return true;
    }

    private static bool TryValidateWindowsUncRootSegment(
        ReadOnlySpan<char> segment,
        string segmentRole,
        [NotNullWhen(false)] out string? errorMessage)
    {
        if (IsAllDots(segment))
        {
            errorMessage = $"The UNC {segmentRole} name '{segment.ToString()}' must not consist only of dots.";
            return false;
        }

        foreach (char character in segment)
        {
            if (IsInvalidWindowsSegmentCharacter(character))
            {
                errorMessage = $"The UNC {segmentRole} name '{segment.ToString()}' contains an invalid character.";
                return false;
            }
        }

        if (segment[^1] is ' ' or '.')
        {
            errorMessage = $"The UNC {segmentRole} name '{segment.ToString()}' must not end with a space or period.";
            return false;
        }

        errorMessage = null;
        return true;
    }

    private static bool TryValidateRootOnly(
        FileSystemPathKind kind,
        [NotNullWhen(false)] out string? errorMessage)
    {
        if (AllowsRootOnly(kind))
        {
            errorMessage = null;
            return true;
        }

        errorMessage = "A file path must include a file-name segment.";
        return false;
    }

    private static bool AllowsTrailingSeparator(FileSystemPathKind kind) => kind is FileSystemPathKind.Directory
        or FileSystemPathKind.FileOrDirectory;

    private static bool AllowsRootOnly(FileSystemPathKind kind) => kind is FileSystemPathKind.Directory
        or FileSystemPathKind.FileOrDirectory;

    private static bool IsUnixRootOnly(ReadOnlySpan<char> path) => path.Length == 1 && path[0] == '/';

    private static bool StartsWithWindowsDeviceNamespacePrefix(ReadOnlySpan<char> path) => path.Length >= 4
        && IsWindowsDirectorySeparator(path[0])
        && IsWindowsDirectorySeparator(path[1])
        && path[2] is '?' or '.'
        && IsWindowsDirectorySeparator(path[3]);

    private static bool StartsWithUncPrefix(ReadOnlySpan<char> path) => path.Length >= 2
        && IsWindowsDirectorySeparator(path[0])
        && IsWindowsDirectorySeparator(path[1]);

    private static bool IsWindowsDirectorySeparator(char character) => character is '\\' or '/';

    private static int IndexOfNextWindowsSeparator(ReadOnlySpan<char> path, int startIndex)
    {
        for (int i = startIndex; i < path.Length; i++)
        {
            if (IsWindowsDirectorySeparator(path[i]))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool IsInvalidWindowsSegmentCharacter(char character) => character is < (char)32
        or '<'
        or '>'
        or ':'
        or '"'
        or '|'
        or '?'
        or '*';

    private static bool IsReservedWindowsDeviceName(ReadOnlySpan<char> segment)
    {
        int dotIndex = segment.IndexOf('.');
        ReadOnlySpan<char> stem = dotIndex >= 0
            ? segment[..dotIndex]
            : segment;

        return stem.Equals("CON", StringComparison.OrdinalIgnoreCase)
            || stem.Equals("PRN", StringComparison.OrdinalIgnoreCase)
            || stem.Equals("AUX", StringComparison.OrdinalIgnoreCase)
            || stem.Equals("NUL", StringComparison.OrdinalIgnoreCase)
            || IsReservedWindowsPortName(stem, "COM")
            || IsReservedWindowsPortName(stem, "LPT");
    }

    private static bool IsReservedWindowsPortName(ReadOnlySpan<char> stem, string prefix) => stem.Length == 4
        && stem[..3].Equals(prefix, StringComparison.OrdinalIgnoreCase)
        && IsWindowsReservedPortDigit(stem[3]);

    private static bool IsWindowsReservedPortDigit(char character) => character is (>= '1' and <= '9')
        or '\u00B9'
        or '\u00B2'
        or '\u00B3';

    private static bool IsCurrentOrParentDirectorySegment(ReadOnlySpan<char> segment) => (segment.Length == 1 && segment[0] == '.')
        || (segment.Length == 2 && segment[0] == '.' && segment[1] == '.');

    private static bool IsAllDots(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
        {
            return false;
        }

        foreach (char character in value)
        {
            if (character != '.')
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsAsciiLetter(char character) => character is (>= 'A' and <= 'Z')
        or (>= 'a' and <= 'z');
}