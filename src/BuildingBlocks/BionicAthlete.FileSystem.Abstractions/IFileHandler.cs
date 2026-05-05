namespace BionicAthlete.FileSystem.Abstractions;

public interface IFileHandler
{
    Task<bool> IsFileSupportedAsync(string filePath, CancellationToken cancellationToken = default);
    bool IsFileSupported(string filePath);
}