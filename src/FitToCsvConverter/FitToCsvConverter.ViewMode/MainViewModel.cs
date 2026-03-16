namespace FitToCsvConverter.ViewModel;

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Text;
using BionicCode.Utilities.Net;
using FitToCsvConverter.Data;

public class MainViewModel : ViewModel
{
    public const string DefaultDestinationFolder = @"C:\Temp\FitToCsvConverterOutput";
    private const string FitFileExtension = ".fit";
    private string _destinationFolder;
    private ObservableFileSystemPathHashSet _selectedFitFilePaths;
    private ExportData _selectedExportData;
    private string _selectedFitFilePath;
    private readonly PropertyValidationDelegate<ObservableFileSystemPathHashSet> _fitFilePathsValidator;
    private readonly PropertyValidationDelegate<string> _filePathsValidator;
    private readonly PropertyValidationDelegate<string> _folderPathValidator;
    private readonly SetValueOptions _setValueOptions;

    public MainViewModel()
    {
        _fitFilePathsValidator = IsFitFilePathsValid();
        _filePathsValidator = IsFilePathsValid();
        _folderPathValidator = IsFolderPathValid();
        _setValueOptions = SetValueOptions.Default with { IsRejectInvalidValueEnabled = true, IsThrowExceptionOnValidationErrorEnabled = true, IsRejectEqualValuesEnabled = true };
        _destinationFolder = DefaultDestinationFolder;
        ExportData = [];
        _selectedFitFilePaths = [];
        DeleteExtraFileCommand = new RelayCommand<ExportData>(exportData => ExportData.Remove(exportData), (exportData) => ExportData.Contains(exportData));
        ExportCommand = new AsyncRelayCommand(ExecuteExportCommandAsync, CanExecuteExportCommand);
    }

    private bool CanExecuteExportCommand() => throw new NotImplementedException();
    private async Task ExecuteExportCommandAsync()
    {
        var conversionInfoList = new List<ConversionInfo>();
        foreach (ExportData exportData in ExportData)
        {
            string fitFilePath = exportData.FitFilePath;
            string temporaryDestinationFilePath = Path.Combine(Path.GetTempPath(), $"{exportData.FileName}.csv");
            exportData.ExportedFilePath = temporaryDestinationFilePath;
            // Here we would perform the actual conversion from .fit to .csv using the fitFilePath and destinationFilePath.
            // For the sake of this example, we'll just create a ConversionInfo object and throw a NotImplementedException.
            var conversionInfo = new ConversionInfo(fitFilePath, temporaryDestinationFilePath);
            conversionInfoList.Add(conversionInfo);
        }

        IProgress<ProgressData> exportProgressReporter = StartNewObservableProgressReporting(string.Empty, "Export FIT to CSV");
        _ = await FitConverter.RunFitToCsvAsync(conversionInfoList.AsReadOnly(), exportProgressReporter);

        await CreateArchivesAsync();
        Cleanup();
    }

    private async Task CreateArchivesAsync()
    {
        var batchList = new List<FileBatch>();
        foreach (ExportData exportData in ExportData)
        {
            IEnumerable<FileDescriptor> fileDescriptors = exportData.SelectedExtraFilePaths.Select(filePath => new FileDescriptor(filePath));
            var batch = new FileBatch(fileDescriptors, DestinationFolder, exportData.FileName, Encoding.UTF8, CompressionLevel.SmallestSize);
            batchList.Add(batch);
        }

        var batches = new FileBatches(batchList);
        IProgress<ProgressData> packProgressReporter = StartNewObservableProgressReporting(string.Empty, "Pack files to ZIP archives.");
        await ArchiveCreator.CreateArchivesAsync(batches, packProgressReporter);
    }

    public void AddFitFilePaths(IEnumerable<string> fitFilePaths)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNullOrEmpty(fitFilePaths);

        foreach (string fitFilePath in fitFilePaths)
        {
            if (SelectedFitFilePaths.Add(fitFilePath))
            {
                var exportData = new ExportData(_filePathsValidator)
                {
                    FitFilePath = fitFilePath
                };

                ExportData.Add(exportData);
            }
        }
    }

    public void AddExtraFilePaths(ExportData exportData, string[] filePaths)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(exportData);
        ArgumentNullExceptionAdvanced.ThrowIfNull(filePaths);

        if (filePaths.Length == 0)
        {
            return;
        }

        SelectedExportData = exportData;
        SelectedExportData.AddExtraFilePaths(filePaths);
    }

    public string DestinationFolder
    {
        get => _destinationFolder;
        set => _ = TrySetValue(value, _folderPathValidator, ref _destinationFolder, _setValueOptions);
    }

    public string SelectedFitFilePath
    {
        get => _selectedFitFilePath;
        set => _ = TrySetValue(value, value => SelectedFitFilePaths.Contains(value) ? new PropertyValidationResult(true, []) : new PropertyValidationResult(false, ["Selected file is not in the list of fit files."]), ref _selectedFitFilePath, _setValueOptions);
    }

    public ObservableFileSystemPathHashSet SelectedFitFilePaths
    {
        get => _selectedFitFilePaths;
        private set
        {
            if (TrySetValue(value, _fitFilePathsValidator, ref _selectedFitFilePaths, _setValueOptions))
            {
                ExportData.Clear();
                foreach (string fitFilePath in SelectedFitFilePaths)
                {
                    var exportData = new ExportData(_filePathsValidator)
                    {
                        FitFilePath = fitFilePath
                    };

                    ExportData.Add(exportData);
                }
            }
        }
    }

    public IRelayCommand<ExportData> DeleteExtraFileCommand { get; }
    public IAsyncRelayCommand ExportCommand { get; }

    public ObservableCollection<ExportData> ExportData { get; private set; }
    public ExportData SelectedExportData
    {
        get => _selectedExportData;
        set => TrySetValue(value, ref _selectedExportData);
    }

    private static PropertyValidationDelegate<ObservableFileSystemPathHashSet> IsFitFilePathsValid() => fitFilePaths =>
        {
            if (fitFilePaths.Count == 0)
            {
                return new PropertyValidationResult(false, ["At least one file must be selected."]);
            }

            foreach (string fitFilePath in fitFilePaths)
            {
                PropertyValidationResult result = IsFilePathValid(fitFilePath);
                if (!result.IsValid)
                {
                    return result;
                }

                if (!Path.GetExtension(fitFilePath).Equals(FitFileExtension, StringComparison.OrdinalIgnoreCase))
                {
                    return new PropertyValidationResult(false, [$"Invalid file type: only .fit files are allowed. Found: '{fitFilePath}'."]);
                }
            }

            return new PropertyValidationResult(true, Array.Empty<string>());
        };

    private static PropertyValidationDelegate<string> IsFilePathsValid() => filePath =>
        {
            ArgumentNullExceptionAdvanced.ThrowIfNull(filePath);

            PropertyValidationResult result = IsFilePathValid(filePath);
            return new PropertyValidationResult(true, Array.Empty<string>());
        };

    private static PropertyValidationResult IsFilePathValid(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return new PropertyValidationResult(false, ["File path cannot be NULL or empty."]);
        }

        if (!File.Exists(filePath))
        {
            return new PropertyValidationResult(false, [$"Invalid file path: '{filePath}'."]);
        }

        return new PropertyValidationResult(true, Array.Empty<string>());
    }

    private static PropertyValidationDelegate<string> IsFolderPathValid() => folderPath => new PropertyValidationResult(
        !string.IsNullOrWhiteSpace(folderPath) && Directory.Exists(folderPath),
        ["Destination folder cannot be empty and must exist."]);

    public event PropertyChangedEventHandler? PropertyChanged;
}

public class ExportData : ViewModel
{
    private readonly PropertyValidationDelegate<string> _filePathsValidator;

    public ExportData(PropertyValidationDelegate<string> filePathsValidator)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(filePathsValidator);

        _filePathsValidator = filePathsValidator;
        SelectedExtraFilePaths = [];
        SelectedExtraFilePaths.CollectionChanged += ValidateOnItemAdded;
        FileName = string.Empty;
        ExportedFilePath = string.Empty;
    }

    public void AddExtraFilePaths(IEnumerable<string> filePaths)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(filePaths);
        foreach (string filePath in filePaths)
        {
            _ = SelectedExtraFilePaths.Add(filePath);
        }
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

    public string FitFilePath { get; init; }
    public string FileName { get; set; }
    internal string ExportedFilePath { get; set; }

    public ObservableFileSystemPathHashSet SelectedExtraFilePaths { get; }
}