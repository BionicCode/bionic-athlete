namespace BionicAthlete.Training.Presentation.ViewModel;

using System.Diagnostics;
using System.IO;
using System.Reflection;
using BionicCode.Utilities.Net;

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
        _isRenamingEnabled = fileDescriptor.HasRenamingInformation;
        IsEmbeddedResource = false;
        _assemblyOfEmbeddedFile = null!;

        _name = fileDescriptor.Name;
        _originalName = Name;
        _newName = Name;
        Location = fileDescriptor.Location;
        FullPath = fileDescriptor.FullPath;
        Extension = FileExtension.FromFilePath(FullPath);
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
        Location = new DirectoryDescriptor(Path.GetDirectoryName(filePath) ?? string.Empty);
        _isRenamingEnabled = isRenamingRequired;
        IsEmbeddedResource = false;
        _originalName = Name;
        _newName = Name;
        FullPath = filePath;
        Extension = FileExtension.FromFilePath(FullPath);
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
        Location = new DirectoryDescriptor($"{_assemblyOfEmbeddedFile.GetName().Name}.{folderName.Trim('/', '\\', '.')}");
        _isRenamingEnabled = false;
        IsEmbeddedResource = true;
        _originalName = Name;
        _newName = Name;
        FullPath = $"{Location.FullPath}.{Name}";
        Extension = FileExtension.FromFilePath(FullPath);
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
        FullPath = Path.Combine(Location.FullPath, Name);
        IsRenamed = false;
    }

    public void RedoRenaming()
    {
        if (!IsRenamingEnabled || IsRenamed)
        {
            return;
        }

        Name = _newName;
        FullPath = Path.Combine(Location.FullPath, Name);
        IsRenamed = true;
    }

    public void Rename(string newName)
    {
        _newName = newName;
        if (IsRenamingEnabled)
        {
            IsRenamed = true;
            Name = _newName;
            FullPath = Path.Combine(Location.FullPath, Name);
        }
    }

    public FileDescriptor ToFileDescriptor() => IsEmbeddedResource
            ? new(Name, Location, _assemblyOfEmbeddedFile)
            {
                OriginalFullPath = OriginalFullPath,
                OriginalName = _originalName,
            }
            : new(Name, Location)
            {
                OriginalFullPath = OriginalFullPath,
                OriginalName = _originalName,
            };

    protected virtual void OnRenamed(string oldName, string oldFullPath) => Renamed?.Invoke(this, new FileDescriptorChangedEventArgs(oldName, Name, oldFullPath, FullPath, OriginalFullPath));

    public event EventHandler<FileDescriptorChangedEventArgs>? Renamed;

    public DirectoryDescriptor Location { get; }
    public string FullPath { get; private set; }
    public bool IsEmbeddedResource { get; }
    public FileExtension Extension { get; }
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
