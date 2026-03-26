namespace FitToCsvConverter.Data;

using System.IO;
using BionicCode.Utilities.Net;

public sealed class TemporaryFileManager : ITemporaryFileManager
{
    private static readonly ObservableFileSystemPathHashSet s_temporaryFilePaths = [];

    public string TemporaryDirectoryPath => Path.GetTempPath();

    public void RegisterTemporaryFilePath(string filePath)
    {
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            _ = s_temporaryFilePaths.Add(filePath);
        }
    }

    public void CleanUpTemporaryFiles()
    {
        foreach (string filePath in s_temporaryFilePaths)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                Console.WriteLine($"Failed to delete temporary file '{filePath}': {ex.Message}");
            }
        }

        s_temporaryFilePaths.Clear();
    }

    // Uses Path.GetFileName() to ensure that only the file name is combined with the temporary directory path, preventing any directory traversal issues.
    public string CreateTemporaryFilePath(string fileName) => Path.Combine(TemporaryDirectoryPath, Path.GetFileName(fileName));

    /// <summary>
    /// Generates a unique file name by appending a new GUID to the original file name.
    /// </summary>
    /// <remarks>This method is useful for avoiding file name collisions when saving files. The generated file
    /// name will always be unique due to the use of a GUID.
    /// <para/>For example, if the original file name is "example.txt", the generated file name might be "example_123e4567-e89b-12d3-a456-426614174000.txt".</remarks>
    /// <param name="fileName">The original file name, including its extension. Cannot be null or empty.</param>
    /// <returns>A new file name string that combines the original name with a unique GUID, preserving the original file
    /// extension.</returns>
    public string MakeFileNameUnique(string fileName) => $"{Path.GetFileNameWithoutExtension(fileName)}_{Guid.NewGuid()}{Path.GetExtension(fileName)}";
}

