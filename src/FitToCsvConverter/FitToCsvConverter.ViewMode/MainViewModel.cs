namespace FitToCsvConverter.ViewModel;

using System.Collections.ObjectModel;
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
    private ExportData? _selectedExportData;
    private string? _selectedFitFilePath;
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
        _selectedFitFilePath = string.Empty;
        DeleteExtraFileCommand = new RelayCommand<string>(filePath => SelectedExportData.SelectedExtraFilePaths.Remove(filePath), (filePath) => SelectedExportData is not null && SelectedExportData.SelectedExtraFilePaths.Contains(filePath));
        ExportCommand = new AsyncRelayCommand(ExecuteExportCommandAsync, CanExecuteExportCommand);
        StartNewSessionCommand = new RelayCommand(ExecuteStartNewSessionCommand);
    }

    public void StartNewSession()
    {
        SelectedFitFilePaths.Clear();
        SelectedFitFilePath = string.Empty;
        ExportData.Clear();
        SelectedExportData = null;
        RemoveAllObservableProgressData();
        IsReportingProgress = false;
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

    private bool CanExecuteExportCommand() => ExportData.Any()
        && !string.IsNullOrEmpty(DestinationFolder)
        && SelectedFitFilePaths.Any();

    private async Task ExecuteExportCommandAsync()
    {
        IsReportingProgress = true;
        try
        {
            IEnumerable<ConversionInfo> conversionInfoEnumerable = EnumerateConversionInfo();
            IProgress<ProgressData> exportProgressReporter = StartNewObservableProgressReporting(string.Empty, "Export FIT to CSV");
            _ = await FitConverter.RunFitToCsvAsync(conversionInfoEnumerable, ExportData.Count, exportProgressReporter);

            await CreateArchivesAsync();
        }
        finally
        {
            CleanUp();
        }
    }

    private void ExecuteStartNewSessionCommand() => StartNewSession();

    private IEnumerable<ConversionInfo> EnumerateConversionInfo()
    {
        foreach (ExportData exportData in ExportData)
        {
            string fitFilePath = exportData.FitFilePath;
            string temporaryDestinationFilePath = Path.Combine(Path.GetTempPath(), $"{exportData.FileName}.csv");
            exportData.ExportedFilePath = temporaryDestinationFilePath;
            var conversionInfo = new ConversionInfo(fitFilePath, temporaryDestinationFilePath);

            yield return conversionInfo;
        }
    }

    private void CleanUp()
    {
        foreach (ExportData exportData in ExportData)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(exportData.ExportedFilePath)
                    && File.Exists(exportData.ExportedFilePath))
                {
                    File.Delete(exportData.ExportedFilePath);
                }
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed. For this example, we'll just write to the console.
                Console.WriteLine($"Failed to delete temporary file '{exportData.ExportedFilePath}': {ex.Message}");
            }
        }
    }

    private async Task CreateArchivesAsync()
    {
        IEnumerable<FileBatch> enumerableFileBatches = EnumerateFileBatches();

        var batches = new FileBatches(enumerableFileBatches, ExportData.Count);
        IProgress<ProgressData> packProgressReporter = StartNewObservableProgressReporting(string.Empty, "Pack files to ZIP archives.");
        await ArchiveCreator.CreateArchivesAsync(batches, packProgressReporter);
    }

    private IEnumerable<FileBatch> EnumerateFileBatches()
    {
        foreach (ExportData exportData in ExportData)
        {
            var exportedCsvFileDescriptor = new FileDescriptor(exportData.ExportedFilePath, false);

            // Extra files count + 1 exported csv file
            int sourceFilesCount = exportData.SelectedExtraFilePaths.Count + 1;

            IEnumerable<FileDescriptor> fileDescriptors = exportData.SelectedExtraFilePaths
                .Select(filePath => new FileDescriptor(filePath, exportData.IsRenamingEnabled))
                .Concat([exportedCsvFileDescriptor]);
            var batch = new FileBatch(fileDescriptors, sourceFilesCount, DestinationFolder, exportData.FileName, Encoding.UTF8, CompressionLevel.SmallestSize);

            yield return batch;
        }
    }

    public string DestinationFolder
    {
        get => _destinationFolder;
        set => _ = TrySetValue(value, _folderPathValidator, ref _destinationFolder, _setValueOptions);
    }

    public string? SelectedFitFilePath
    {
        get => _selectedFitFilePath;
        set => _ = TrySetValue(value, value => SelectedFitFilePaths.IsEmpty() || SelectedFitFilePaths.Contains(value) ? new PropertyValidationResult(true, []) : new PropertyValidationResult(false, ["Selected file is not in the list of fit files."]), ref _selectedFitFilePath, _setValueOptions);
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

    public IRelayCommand<string> DeleteExtraFileCommand { get; }
    public IAsyncRelayCommand ExportCommand { get; }
    public IRelayCommand StartNewSessionCommand { get; }

    public ObservableCollection<ExportData> ExportData { get; private set; }
    public ExportData? SelectedExportData
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
}
