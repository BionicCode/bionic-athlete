namespace FitToCsvConverter.Data;

public interface ITemporaryFileManager
{
    string TemporaryDirectoryPath { get; }
    string CreateTemporaryFilePath(string fileName);
    string MakeFileNameUnique(string fileName);
    void RegisterTemporaryFilePath(string filePath);
    void CleanUpTemporaryFiles();
}