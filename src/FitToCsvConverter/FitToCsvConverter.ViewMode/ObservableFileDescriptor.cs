namespace FitToCsvConverter.ViewModel;

using System.Diagnostics;
using System.IO;
using BionicCode.Utilities.Net;
using FitToCsvConverter.Data;

[DebuggerDisplay("Name = {Name}, Location = {Location}, IsRenamingEnabled = {IsRenamingEnabled}, IsRenamed = {IsRenamed}")]
public class ObservableFileDescriptor : ViewModel
{
    private bool _isRenamingEnabled;
    private string _newName;
    private string _name;
    private readonly string _originalName;
    private readonly SetValueOptions _setValueOptions;

    public ObservableFileDescriptor(FileDescriptor fileDescriptor)
    {
        _isRenamingEnabled = fileDescriptor.IsRenamingRequired;
        Name = fileDescriptor.Name;
        _originalName = Name;
        _newName = Name;
        Location = fileDescriptor.Location;
        FullPath = fileDescriptor.FullPath;
        Extension = Path.GetExtension(FullPath);
        OriginalFullPath = FullPath;

        _setValueOptions = new SetValueOptions
        {
            IsRejectEqualValuesEnabled = true,
            IsThrowExceptionOnValidationErrorEnabled = true,
        };
    }

    public ObservableFileDescriptor(string filePath, bool isRenamingRequired)
    {
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(filePath);

        Name = Path.GetFileName(filePath);
        Location = Path.GetDirectoryName(filePath) ?? string.Empty;
        _isRenamingEnabled = isRenamingRequired;
        _originalName = Name;
        _newName = Name;
        FullPath = filePath;
        Extension = Path.GetExtension(FullPath);
        OriginalFullPath = FullPath;

        _setValueOptions = new SetValueOptions
        {
            IsRejectEqualValuesEnabled = true,
            IsThrowExceptionOnValidationErrorEnabled = true,
        };
    }

    public void UndoRenaming()
    {
        if (!IsRenamed)
        {
            return;
        }

        Name = _originalName;
        FullPath = Path.Combine(Location, Name);
        IsRenamed = false;
    }

    public void RedoRenaming()
    {
        if (!IsRenamingEnabled || IsRenamed)
        {
            return;
        }

        Name = _newName;
        FullPath = Path.Combine(Location, Name);
        IsRenamed = true;
    }

    public void Rename(string newName)
    {
        _newName = newName;
        if (IsRenamingEnabled)
        {
            IsRenamed = true;
            Name = _newName;
            FullPath = Path.Combine(Location, Name);
        }
    }

    public FileDescriptor ToFileDescriptor() => new(Name, Location, IsRenamingEnabled)
    {
        OriginalFullPath = OriginalFullPath,
        OriginalName = _originalName,
    };

    protected virtual void OnRenamed(string oldName, string oldFullPath) => Renamed?.Invoke(this, new FileDescriptorChangedEventArgs(oldName, Name, oldFullPath, FullPath, OriginalFullPath));

    public event EventHandler<FileDescriptorChangedEventArgs>? Renamed;

    public string Location { get; }
    public string FullPath { get; private set; }
    public string Extension { get; }
    public string OriginalFullPath { get; }
    public bool IsRenamed { get; private set; }

    public string Name
    {
        get => _name;
        private set => TrySetValue(value, ref _name, _setValueOptions);
    }

    public bool IsRenamingEnabled
    {
        get => _isRenamingEnabled;
        set
        {
            if (TrySetValueSilent(value, ref _isRenamingEnabled, _setValueOptions))
            {
                string oldName = Name;
                string oldFullPath = FullPath;

                if (!IsRenamingEnabled)
                {
                    UndoRenaming();
                }
                else
                {
                    RedoRenaming();
                }

                OnPropertyChanged();
                OnRenamed(oldName, oldFullPath);
            }
        }
    }
}

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
