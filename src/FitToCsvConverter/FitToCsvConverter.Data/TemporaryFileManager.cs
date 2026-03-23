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
}

