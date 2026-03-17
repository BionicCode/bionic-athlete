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
    }

    public FileDescriptor(string filePath, bool isRenamingRequired)
    {
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(filePath);

        Name = Path.GetFileName(filePath);
        Location = Path.GetDirectoryName(filePath) ?? string.Empty;
        _filePath = filePath;
        IsRenamingRequired = isRenamingRequired;
    }

    public bool IsRenamingRequired { get; }
    public string Name { get; }
    public string Location { get; }
    public string FullPath => _filePath;
}
