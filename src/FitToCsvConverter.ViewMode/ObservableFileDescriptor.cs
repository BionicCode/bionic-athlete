namespace FitToCsvConverter.ViewModel;

using System.Diagnostics;
using System.IO;
using System.Reflection;
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
    private readonly Assembly _assemblyOfEmbeddedFile;

    public ObservableFileDescriptor(FileDescriptor fileDescriptor)
    {
        _isRenamingEnabled = fileDescriptor.IsRenamingRequired;
        IsEmbeddedResource = false;
        _assemblyOfEmbeddedFile = null!;

        _name = fileDescriptor.Name;
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

        _assemblyOfEmbeddedFile = null!;
        _name = Path.GetFileName(filePath);
        Location = Path.GetDirectoryName(filePath) ?? string.Empty;
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(Location, $"The argument '{nameof(filePath)}' does not contain a directory. Found: '{filePath}'", nameof(filePath));
        _isRenamingEnabled = isRenamingRequired;
        IsEmbeddedResource = false;
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

    public ObservableFileDescriptor(string embeddedFileName, string folderName, Assembly assemblyOfEmbeddedFile)
    {
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(embeddedFileName);
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(folderName);
        ArgumentNullException.ThrowIfNull(assemblyOfEmbeddedFile);

        _name = embeddedFileName;
        _assemblyOfEmbeddedFile = assemblyOfEmbeddedFile;
        Location = $"{_assemblyOfEmbeddedFile.GetName().Name}.{folderName.Trim('/', '\\', '.')}";
        _isRenamingEnabled = false;
        IsEmbeddedResource = true;
        _originalName = Name;
        _newName = Name;
        FullPath = $"{Location}.{Name}";
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

    public FileDescriptor ToFileDescriptor() => IsEmbeddedResource
            ? new(Name, Location, IsRenamingEnabled, _assemblyOfEmbeddedFile)
            {
                OriginalFullPath = OriginalFullPath,
                OriginalName = _originalName,
            }
            : new(Name, Location, IsRenamingEnabled)
            {
                OriginalFullPath = OriginalFullPath,
                OriginalName = _originalName,
            };

    protected virtual void OnRenamed(string oldName, string oldFullPath) => Renamed?.Invoke(this, new FileDescriptorChangedEventArgs(oldName, Name, oldFullPath, FullPath, OriginalFullPath));

    public event EventHandler<FileDescriptorChangedEventArgs>? Renamed;

    public string Location { get; }
    public string FullPath { get; private set; }
    public bool IsEmbeddedResource { get; }
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
