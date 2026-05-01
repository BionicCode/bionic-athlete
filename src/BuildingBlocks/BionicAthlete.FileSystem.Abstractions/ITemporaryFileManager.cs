namespace BionicAthlete.FileSystem.Abstractions;

public interface ITemporaryFileManager
{
    string TemporaryDirectoryPath { get; }
    string CreateTemporaryFilePath();
    string CreateTemporaryFilePath(string fileName);
    string CreateTemporaryFilePath(string subfolder, string fileName);
    string MakeFileNameUnique(string fileName);
    void RegisterTemporaryFilePath(string filePath);
    void CleanUpTemporaryFiles();
}