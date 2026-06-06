namespace BionicAthlete.FileSystem.Abstractions;

using BionicCode.Utilities.Net;

public interface ITemporaryFileManager
{
    DirectoryDescriptor TemporaryDirectoryPath { get; }
    FileSystemPathDescriptor CreateTemporaryFilePath();
    FileSystemPathDescriptor CreateTemporaryFilePath(string fileName);
    FileSystemPathDescriptor CreateTemporaryFilePath(string subfolder, string fileName);
    string MakeFileNameUnique(string fileName);
    void RegisterTemporaryFilePath(FileSystemPathDescriptor filePath);
    void CleanUpTemporaryFiles();
}