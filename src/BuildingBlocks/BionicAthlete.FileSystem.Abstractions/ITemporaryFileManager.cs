namespace BionicAthlete.FileSystem.Abstractions;

using BionicCode.Utilities.Net;

public interface ITemporaryFileManager
{
    DirectoryDescriptor TemporaryDirectoryPath { get; }
    FileDescriptor CreateTemporaryFilePath();
    FileDescriptor CreateTemporaryFilePath(string fileName);
    FileDescriptor CreateTemporaryFilePath(string subfolder, string fileName);
    string MakeFileNameUnique(string fileName);
    void RegisterTemporaryFilePath(FileDescriptor filePath);
    void CleanUpTemporaryFiles();
}