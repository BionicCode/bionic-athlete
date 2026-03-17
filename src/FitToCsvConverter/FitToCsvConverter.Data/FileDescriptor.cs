namespace FitToCsvConverter.Data;

using BionicCode.Utilities.Net;

public readonly struct FileDescriptor
{
    private readonly string _filePath;

    public FileDescriptor(string name, string location, bool isRenamingRequired)
    {
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(name);
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(location);

        Name = name;
        Location = location;
        _filePath = Path.Combine(Location, Name);
        IsRenamingRequired = isRenamingRequired;
        OriginalName = string.Empty;
        OriginalFullPath = string.Empty;
    }

    public FileDescriptor(string filePath, bool isRenamingRequired)
    {
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(filePath);

        Name = Path.GetFileName(filePath);
        Location = Path.GetDirectoryName(filePath) ?? string.Empty;
        _filePath = filePath;
        IsRenamingRequired = isRenamingRequired;
        OriginalName = string.Empty;
        OriginalFullPath = string.Empty;
    }

    public bool IsRenamingRequired { get; init; }
    public string Name { get; init; }
    public string Location { get; init; }
    public string FullPath => _filePath;
    public string OriginalFullPath { get; init; }
    public string OriginalName { get; init; }
}
