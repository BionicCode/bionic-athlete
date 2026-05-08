namespace BionicCode.Utilities.Net;

/// <summary>
/// Specifies the descriptor-level meaning expected from a syntactically valid file-system path symbol.
/// </summary>
/// <remarks>
/// <para>
/// The value controls only lexical path-shape validation. It does not check whether the path exists,
/// whether it points to a file or directory on disk, whether a drive/share is available, or whether a
/// consuming service can read or write the target.
/// </para>
/// </remarks>
public enum FileSystemPathKind
{
    /// <summary>
    /// The path must describe a file-like symbol. It must end with a non-empty file-name segment and
    /// must not end with a directory separator.
    /// </summary>
    File,

    /// <summary>
    /// The path must describe a directory-like symbol. It may be root-only, may be <c>.</c> or
    /// <c>..</c>, and may end with a directory separator.
    /// </summary>
    Directory,

    /// <summary>
    /// The path may describe either a file-like or directory-like symbol. This is useful for APIs that
    /// only need to know whether the input is a syntactically valid path symbol.
    /// </summary>
    FileOrDirectory,
}
