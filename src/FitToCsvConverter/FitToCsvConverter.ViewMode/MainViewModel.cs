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
    private readonly IArchiveManager _zipArchiveManager;
    private readonly IFitToCsvConverter _garminFitCsvToolConverter;

    public MainViewModel(IZipArchiveManager zipArchiveManager, IGarminFitCsvToolConverter garminFitCsvToolConverter)
    {
        _fitFilePathsValidator = IsFitFilePathsValid();
        _filePathsValidator = IsFilePathsValid();
        _folderPathValidator = IsFolderPathValid();
        _setValueOptions = SetValueOptions.Default with { IsRejectInvalidValueEnabled = true, IsThrowExceptionOnValidationErrorEnabled = true, IsRejectEqualValuesEnabled = true };
        _destinationFolder = DefaultDestinationFolder;
        ExportData = [];
        _selectedFitFilePaths = [];
        _selectedFitFilePath = string.Empty;
        _zipArchiveManager = zipArchiveManager;
        _garminFitCsvToolConverter = garminFitCsvToolConverter;
        DeleteExtraFileCommand = new RelayCommand<ObservableFileDescriptor>(fileDescriptor => SelectedExportData?.SelectedExtraFilePaths.Remove(fileDescriptor), (fileDescriptor) => SelectedExportData is not null && SelectedExportData.SelectedExtraFilePaths.Contains(fileDescriptor));
        ExportCommand = new AsyncRelayCommand(ExecuteExportCommandAsync, CanExecuteExportCommand);
        StartNewSessionCommand = new RelayCommand(ExecuteStartNewSessionCommand);
    }

    // For design-time data only
    public MainViewModel()
    {
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

    public PropertyValidationResult IsFitFilePathValid(string fitFilePath)
    {
        PropertyValidationResult result = IsFilePathValid(fitFilePath);
        if (!result.IsValid)
        {
            return result;
        }

        if (!Path.GetExtension(fitFilePath).Equals(FitFileExtension, StringComparison.OrdinalIgnoreCase)
            && !_zipArchiveManager.IsFileTypeSupportedArchive(fitFilePath))
        {
            return new PropertyValidationResult(false, [$"Invalid file type: only .fit files are allowed. Found: '{fitFilePath}'."]);
        }

        return new PropertyValidationResult(true, Array.Empty<string>());
    }

    public static PropertyValidationResult IsFilePathValid(string filePath)
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

    public async Task AddFitFilePathsAsync(IEnumerable<string> fitFilePaths, CancellationToken cancellationToken)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNullOrEmpty(fitFilePaths);

        foreach (string fitFilePath in fitFilePaths)
        {
            if (_zipArchiveManager.IsFileTypeSupportedArchive(fitFilePath))
            {
                IProgress<ProgressData> progressReporter = StartNewObservableProgressReporting(string.Empty, $"Extracting '{Path.GetFileName(fitFilePath)}'...");
                await foreach (string extractedFIlePath in _zipArchiveManager.ExtractArchiveAsync(fitFilePath, progressReporter, cancellationToken).ConfigureAwait(true))
                {
                    AddFitFilePath(extractedFIlePath);
                }

                RemoveAllObservableProgressData();
            }
            else
            {
                AddFitFilePath(fitFilePath);
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

    private void AddFitFilePath(string fitFilePath)
    {
        if (string.IsNullOrWhiteSpace(fitFilePath))
        {
            return;
        }

        PropertyValidationResult filePathValidationResult = IsFitFilePathValid(fitFilePath);
        if (filePathValidationResult.IsValid
            && SelectedFitFilePaths.Add(fitFilePath))
        {
            var exportData = new ExportData(_filePathsValidator)
            {
                FitFilePath = fitFilePath
            };

            string temporaryDestinationFilePath = Path.Combine(Path.GetTempPath(), $"{exportData.FitFileNameWithoutExtension}.csv");
            exportData.ExportedFilePath = temporaryDestinationFilePath;
            exportData.AddExtraFilePaths([temporaryDestinationFilePath], isRenamingRequired: false);

            ExportData.Add(exportData);
        }
    }

    private bool CanExecuteExportCommand() => ExportData.Any()
        && !string.IsNullOrEmpty(DestinationFolder)
        && SelectedFitFilePaths.Any();

    private async Task ExecuteExportCommandAsync()
    {
        IsReportingProgress = true;
        IEnumerable<ConversionInfo> conversionInfoEnumerable = EnumerateConversionInfo();
        IProgress<ProgressData> exportProgressReporter = StartNewObservableProgressReporting(string.Empty, "Export FIT to CSV");
        await _garminFitCsvToolConverter.ExportToCsvAsync(conversionInfoEnumerable, ExportData.Count, exportProgressReporter);
        await CreateArchivesAsync();
    }

    private IEnumerable<ConversionInfo> EnumerateConversionInfo()
    {
        foreach (ExportData exportData in ExportData)
        {
            var conversionInfo = new ConversionInfo(exportData.FitFilePath, exportData.ExportedFilePath);

            yield return conversionInfo;
        }
    }

    private async Task CreateArchivesAsync()
    {
        IEnumerable<FileBatch> enumerableFileBatches = EnumerateFileBatches();

        var batches = new FileBatches(enumerableFileBatches, ExportData.Count);
        IProgress<ProgressData> packProgressReporter = StartNewObservableProgressReporting(string.Empty, "Pack files to ZIP archives.");
        await _zipArchiveManager.CreateArchivesAsync(batches, packProgressReporter);
    }

    private IEnumerable<FileBatch> EnumerateFileBatches()
    {
        foreach (ExportData exportData in ExportData)
        {
            // Extra files count + 1 exported csv file + 1 original fit file
            int sourceFilesCount = exportData.SelectedExtraFilePaths.Count + 2;

            IEnumerable<FileDescriptor> fileDescriptors = exportData.SelectedExtraFilePaths
                .Select(observableFileDescriptor => observableFileDescriptor.ToFileDescriptor());

            string batchName = exportData.IsAutoRenamingEnabled || string.IsNullOrWhiteSpace(exportData.BatchName)
                ? exportData.AutoRenameBatchName
                : exportData.BatchName;
            var batch = new FileBatch(
                fileDescriptors,
                sourceFilesCount,
                DestinationFolder,
                batchName,
                Encoding.UTF8,
                CompressionLevel.SmallestSize);

            yield return batch;
        }
    }

    private void ExecuteStartNewSessionCommand() => StartNewSession();

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

    public IRelayCommand<ObservableFileDescriptor> DeleteExtraFileCommand { get; }
    public IAsyncRelayCommand ExportCommand { get; }
    public IRelayCommand StartNewSessionCommand { get; }

    public ObservableCollection<ExportData> ExportData { get; private set; }
    public ExportData? SelectedExportData
    {
        get => _selectedExportData;
        set => TrySetValue(value, ref _selectedExportData);
    }

    private PropertyValidationDelegate<ObservableFileSystemPathHashSet> IsFitFilePathsValid() => fitFilePaths =>
        {
            if (fitFilePaths.Count == 0)
            {
                return new PropertyValidationResult(false, ["At least one file must be selected."]);
            }

            foreach (string fitFilePath in fitFilePaths)
            {
                PropertyValidationResult result = IsFitFilePathValid(fitFilePath);
            }

            return new PropertyValidationResult(true, Array.Empty<string>());
        };

    private static PropertyValidationDelegate<string> IsFilePathsValid() => filePath =>
        {
            ArgumentNullExceptionAdvanced.ThrowIfNull(filePath);

            PropertyValidationResult result = IsFilePathValid(filePath);
            return new PropertyValidationResult(true, Array.Empty<string>());
        };

    private static PropertyValidationDelegate<string> IsFolderPathValid() => folderPath => new PropertyValidationResult(
        !string.IsNullOrWhiteSpace(folderPath) && Directory.Exists(folderPath),
        ["Destination folder cannot be empty and must exist."]);
}
