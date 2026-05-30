namespace BionicCode.Utilities.Net;

public enum PathSegmentKind
{
    Undefined = 0,

    /// <summary>
    /// A fully qualified path root segment that represents the root of a file system path. Such segment is not relative. Examples include "C:\" on Windows or "/" on Unix-based systems or "\\server\share" for UNC paths.
    /// </summary>
    FullyQualifiedRoot,
    /// <summary>
    /// A relative drive root segment that represents the root of a relative path on a specific drive. Such segment is relative. Examples include "C:" for e.g., "C:Directory".
    /// </summary>
    RelativeDriveRoot,
    /// <summary>
    /// A relative root segment that represents the root of a relative path. Such segment is relative. Examples include "/" for e.g., "/Subdirectory".
    /// </summary>
    RelativeRoot,
    /// <summary>
    /// A segment that represents the current directory in a relative path. Examples include ".".
    /// </summary>
    CurrentDirectory,
    /// <summary>
    /// A segment that represents the parent directory in a relative path. Examples include "..".
    /// </summary>
    ParentDirectory,
    /// <summary>
    /// A segment that represents a directory name in a path. Examples include "Documents" or "Temp".
    /// </summary>
    DirectoryName,
    /// <summary>
    /// A segment that represents a file name in a path. Examples include "file.txt" or "image.png".
    /// </summary>
    FileName
}
