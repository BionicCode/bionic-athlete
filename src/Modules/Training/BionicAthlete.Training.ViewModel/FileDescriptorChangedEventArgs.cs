namespace BionicAthlete.Training.Presentation;

public class FileDescriptorChangedEventArgs : EventArgs
{
    public FileDescriptorChangedEventArgs(string oldName, string newName, string oldFullPath, string newFullPath, string originalFullPath)
    {
        OldName = oldName;
        NewName = newName;
        OldFullPath = oldFullPath;
        NewFullPath = newFullPath;
        OriginalFullPath = originalFullPath;
    }

    public string OldName { get; }
    public string NewName { get; }
    public string OldFullPath { get; }
    public string NewFullPath { get; }
    public string OriginalFullPath { get; }
}
