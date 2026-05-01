namespace BionicAthlete.Infrastructure.FileSystem;

using System.Diagnostics;
using System.IO;
using BionicAthlete.FileSystem.Abstractions;
using BionicAthlete.Shared.Logging;
using BionicCode.Utilities.Net;

public sealed partial class TemporaryFileManager : ITemporaryFileManager
{
    public const string DefaultDestinationFolderName = "FitToCsvConverter";
    private static readonly ObservableFileSystemPathHashSet s_temporaryFilePaths = [];
    private readonly string _temporaryDirectoryPath;
    private readonly IApplicationLogger<TemporaryFileManager> _logger;

    public string TemporaryDirectoryPath => _temporaryDirectoryPath;

    public TemporaryFileManager(IApplicationLogger<TemporaryFileManager> logger)
    {
        _temporaryDirectoryPath = Path.Combine(Path.GetTempPath(), DefaultDestinationFolderName);
        if (!Directory.Exists(_temporaryDirectoryPath))
        {
            _ = Directory.CreateDirectory(_temporaryDirectoryPath);
        }

        _logger = logger;
    }

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
                _logger.LogErrorMessage($"Failed to delete temporary file '{filePath}': {ex.Message}");
                Debug.WriteLine($"Failed to delete temporary file '{filePath}': {ex.Message}");
            }
        }

        s_temporaryFilePaths.Clear();

        if (Directory.Exists(_temporaryDirectoryPath))
        {
            try
            {
                Directory.Delete(_temporaryDirectoryPath, recursive: true);
            }
            catch (Exception ex)
            {
                _logger.LogErrorMessage($"Failed to delete temporary directory '{_temporaryDirectoryPath}': {ex.Message}");
                Debug.WriteLine($"Failed to delete temporary directory '{_temporaryDirectoryPath}': {ex.Message}");
            }
        }
    }

    // Uses Path.GetFileName() to ensure that only the file name is combined with the temporary directory path, preventing any directory traversal issues.
    public string CreateTemporaryFilePath() => Path.Combine(TemporaryDirectoryPath, Path.GetTempFileName());

    // Uses Path.GetFileName() to ensure that only the file name is combined with the temporary directory path, preventing any directory traversal issues.
    public string CreateTemporaryFilePath(string fileName) => Path.Combine(TemporaryDirectoryPath, Path.GetFileName(fileName));

    // Uses Path.GetFileName() to ensure that only the file name is combined with the temporary directory path, preventing any directory traversal issues.
    public string CreateTemporaryFilePath(string subFolder, string fileName)
    {
        string directory = Path.Combine(TemporaryDirectoryPath, subFolder);
        if (!Directory.Exists(directory))
        {
            _ = Directory.CreateDirectory(directory);
        }

        return Path.Combine(directory, Path.GetFileName(fileName));
    }

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

