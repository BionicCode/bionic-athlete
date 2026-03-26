namespace FitToCsvConverter.ViewModel;

using System.Collections.Specialized;
using System.IO;
using BionicCode.Utilities.Net;

public class ExportData : ViewModel
{
    private readonly PropertyValidationDelegate<string> _filePathsValidator;
    private readonly HashSet<string> _newFilenames = [];
    private string _batchName;
    private bool _hasCorrectedDuplicateNewNames;
    private bool _isAutoRenamingEnabled;
    private string? _fitFileName;
    private string? _autoRenameBatchName;
    private string? _fitFileNameWithoutExtension;
    private bool _isIncludeFitFileEnabled;
    private string _fitFilePath;

    public ObservableFileDescriptor FitFileDescriptor { get; private set; }

    private readonly SetValueOptions _setValueOptions;

    public ExportData(PropertyValidationDelegate<string> filePathsValidator)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(filePathsValidator);

        _filePathsValidator = filePathsValidator;
        SelectedExtraFilePaths = [];
        SelectedExtraFilePaths.CollectionChanged += OnSelectedExtraFilePathsCollectionChanged;
        _newFilenames = [];
        SelectedExtraFilePaths.CollectionChanged += ValidateOnItemAdded;
        _fitFilePath = string.Empty;
        _batchName = string.Empty;
        ExportedFilePath = string.Empty;
        _isAutoRenamingEnabled = true;
        _isIncludeFitFileEnabled = true;

        _setValueOptions = new SetValueOptions
        {
            IsRejectEqualValuesEnabled = true,
            IsThrowExceptionOnValidationErrorEnabled = true,
        };
    }

    public void AddExtraFilePaths(IEnumerable<string> filePaths, bool isRenamingRequired = false)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(filePaths);
        foreach (string filePath in filePaths)
        {
            if (filePath.Equals(FitFilePath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var fileDescriptor = new ObservableFileDescriptor(filePath, isRenamingRequired);
            _ = SelectedExtraFilePaths.Add(fileDescriptor);
        }
    }

    private void RenameFile(ObservableFileDescriptor fileDescriptor)
    {
        if (!IsAutoRenamingEnabled
            && string.IsNullOrWhiteSpace(BatchName))
        {
            fileDescriptor.UndoRenaming();
        }
        else if (!fileDescriptor.IsRenamingEnabled)
        {
            return;
        }
        else
        {
            string newName = IsAutoRenamingEnabled
                ? $"{AutoRenameBatchName}{fileDescriptor.Extension}"
                : $"{BatchName}{fileDescriptor.Extension}";
            if (!_newFilenames.Add(newName))
            {
                _hasCorrectedDuplicateNewNames = true;
                int counter = 1;
                do
                {
                    newName = IsAutoRenamingEnabled
                        ? $"{AutoRenameBatchName}_{counter}{fileDescriptor.Extension}"
                        : $"{BatchName}_{counter}{fileDescriptor.Extension}";
                    counter++;
                } while (!_newFilenames.Add(newName));
            }

            fileDescriptor.Rename(newName);
        }
    }

    private void RenameAllFiles()
    {
        _newFilenames.Clear();
        _hasCorrectedDuplicateNewNames = false;
        foreach (ObservableFileDescriptor fileDescriptor in SelectedExtraFilePaths)
        {
            RenameFile(fileDescriptor);
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "<Pending>")]
    private string GetAutoRenameBatchName()
    {
        if (_autoRenameBatchName is null)
        {
            // TODO::Replace with new API call
            //DateTime dataDate = FitFileAnalyzer.GetSessionDate(FitFilePath);
            string batchFileName = $"{DateTime.Now:yyyy-MM-dd}_{FitFileNameWithoutExtension}";
            _autoRenameBatchName = batchFileName;
        }

        return _autoRenameBatchName;
    }

    private void ValidateOnItemAdded(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
            case NotifyCollectionChangedAction.Replace:
                foreach (string newFilePath in e.NewItems?.OfType<string>() ?? [])
                {
                    PropertyValidationResult validationResult = _filePathsValidator.Invoke(newFilePath);
                    if (!validationResult.IsValid)
                    {
                        throw new InvalidOperationException(validationResult.ErrorMessages.JoinToString($",{Environment.NewLine}"));
                    }
                }

                break;
        }
    }

    private void OnSelectedExtraFilePathsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                foreach (ObservableFileDescriptor fileDescriptor in e.NewItems?.OfType<ObservableFileDescriptor>() ?? [])
                {
                    fileDescriptor.Renamed += OnFileDescriptorRenamed;
                    RenameFile(fileDescriptor);
                }

                break;
            case NotifyCollectionChangedAction.Remove:
                foreach (ObservableFileDescriptor fileDescriptor in e.OldItems?.OfType<ObservableFileDescriptor>() ?? [])
                {
                    fileDescriptor.Renamed -= OnFileDescriptorRenamed;
                    _ = _newFilenames.Remove(fileDescriptor.Name);
                    if (_hasCorrectedDuplicateNewNames)
                    {
                        RenameAllFiles();
                    }
                }

                break;
        }
    }

    private void OnFileDescriptorRenamed(object? sender, FileDescriptorChangedEventArgs e)
    {
        if (sender is ObservableFileDescriptor fileDescriptor)
        {
            if (fileDescriptor.IsRenamingEnabled)
            {
                RenameFile(fileDescriptor);
            }
            else
            {
                if (_hasCorrectedDuplicateNewNames)
                {
                    RenameAllFiles();
                }
                else
                {
                    _ = _newFilenames.Remove(e.OldName);
                }
            }
        }
    }

    public string FitFilePath
    {
        get => _fitFilePath;
        init
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            _fitFilePath = value;
            if (FitFileDescriptor is not null)
            {
                _ = SelectedExtraFilePaths.Remove(FitFileDescriptor);
            }

            FitFileDescriptor = new ObservableFileDescriptor(FitFilePath, isRenamingRequired: false);
            if (IsIncludeFitFileEnabled)
            {
                _ = SelectedExtraFilePaths.Add(FitFileDescriptor);
            }
        }
    }

    public string FitFileName => _fitFileName ??= Path.GetFileName(FitFilePath);
    public string FitFileNameWithoutExtension => _fitFileNameWithoutExtension ??= Path.GetFileNameWithoutExtension(FitFilePath);

    public string BatchName
    {
        get => _batchName;
        set
        {
            if (TrySetValue(value, ref _batchName))
            {
                RenameAllFiles();
            }
        }
    }

    public bool IsAutoRenamingEnabled
    {
        get => _isAutoRenamingEnabled;
        set => TrySetValue(value, ref _isAutoRenamingEnabled, _setValueOptions);
    }

    public bool IsIncludeFitFileEnabled
    {
        get => _isIncludeFitFileEnabled;
        set
        {
            if (TrySetValue(value, ref _isIncludeFitFileEnabled, _setValueOptions)
                && FitFileDescriptor is not null)
            {
                if (IsIncludeFitFileEnabled)
                {
                    _ = SelectedExtraFilePaths.Add(FitFileDescriptor);
                }
                else
                {
                    _ = SelectedExtraFilePaths.Remove(FitFileDescriptor);
                }
            }
        }
    }

    internal string ExportedFilePath { get; set; }
    public ObservableHashSet<ObservableFileDescriptor> SelectedExtraFilePaths { get; }
    public string AutoRenameBatchName => _autoRenameBatchName ??= GetAutoRenameBatchName();
}
