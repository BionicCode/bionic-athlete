namespace BionicAthlete.FileSystem.Abstractions;

using System.Text;

public interface IFileManager<TValue>
{
    /// <summary>
    /// Writes the provided value to a temporary file with the specified name and returns the file path. 
    /// The file is created in the current user's temporary directory determined by calling 
    /// <see cref="System.IO.Path.GetTempPath"/> which usually points to "%USERPROFILE%\AppData\Local\Temp" on Windows 
    /// or "/tmp" on Unix-based systems, intended for short-term use. 
    /// </summary>
    /// <remarks>Generates a unique temporary file name if none is provided. Otherwise call an overload to specify a file name.</remarks>
    /// <param name="value">The value to write to the temporary file.</param>
    /// <param name="isTemporaryFileManaged">Indicates whether the temporary file should be automatically managed 
    /// and deleted by the system when the application closes or crashes.</param>
    /// <returns>The path to the created temporary file.</returns>
    Task<string> WriteTemporaryAsync(TValue value, Encoding encoding, bool isTemporaryFileManaged, CancellationToken cancellationToken);

    /// <summary>
    /// Writes the provided value to a temporary file with the specified name and returns the file path. 
    /// The subdirectory that includes the file is created in the current user's temporary directory determined by calling 
    /// <see cref="System.IO.Path.GetTempPath"/> which usually points to "%USERPROFILE%\AppData\Local\Temp" on Windows 
    /// or "/tmp" on Unix-based systems, intended for short-term use. 
    /// </summary>
    /// <remarks>
    /// If a file with the same name already exists in the temporary directory, a unique suffix will be appended to the file name to avoid conflicts.</remarks>
    /// <param name="value">The value to write to the temporary file.</param>
    /// <param name="isTemporaryFileManaged">Indicates whether the temporary file should be automatically managed 
    /// and deleted by the system when the application closes or crashes.</param>
    /// <param name="subdirectoryName">The name of the subdirectory within the temporary directory where the file should be created. 
    /// If the subdirectory does not exist, it will be created.</param>
    /// <returns>The path to the created temporary file.</returns>
    Task<string> WriteTemporaryAsync(TValue value, Encoding encoding, string subdirectoryName, bool isTemporaryFileManaged, CancellationToken cancellationToken);
    /// <summary>
    /// Writes the provided value to a specified file path using UTF-8 encoding. 
    /// </summary>
    /// <param name="value">The value to write to the file.</param>
    /// <param name="filePath">The path to the file where the value should be written.</param>
    /// <param name="isOverWriteAllowed">Indicates whether the file should be overwritten if it already exists.</param>
    Task WriteAsync(TValue value, string filePath, bool isOverWriteAllowed);
    Task WriteAsync(TValue value, Encoding encoding, string filePath, bool isOverWriteAllowed, CancellationToken cancellationToken);
    void Write(TValue value, Encoding encoding, string filePath, bool isOverWriteAllowed);
    Task<TValue> ReadAsync(string filePath, Encoding encoding, CancellationToken cancellationToken);
    TValue Read(string filePath, Encoding encoding);
    Task<TValue> ReadAsync(string filePath, CancellationToken cancellationToken);
}
