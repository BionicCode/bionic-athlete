namespace FitToCsvConverter.ViewModel;

using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Text;
using BionicCode.Utilities.Net;
using FitToCsvConverter.Data;
using FitToCsvConverter.Data.Decoding;

public class MainViewModel : ViewModel
{
    private const string FitFileExtension = ".fit";
    private string _destinationFolder;
    private ExportData? _selectedExportData;
    private string? _selectedFitFilePath;
    private readonly PropertyValidationDelegate<ObservableFileSystemPathHashSet> _fitFilePathsValidator;
    private readonly PropertyValidationDelegate<string> _filePathsValidator;
    private readonly PropertyValidationDelegate<string> _folderPathValidator;
    private readonly SetValueOptions _setValueOptions;
    private readonly IArchiveManager _zipArchiveManager;
    private readonly IFitToCsvConverter _garminFitCsvToolConverter;
    private readonly ITemporaryFileManager _temporaryFileManager;
    private readonly Func<IFitActivityDecoder> _cachingFitActivityDecoderFactory;
    private readonly string _allowedFileExtensions;

    public MainViewModel(IZipArchiveManager zipArchiveManager,
        IGarminFitCsvToolConverter garminFitCsvToolConverter,
        ITemporaryFileManager temporaryFileManager,
        Func<IFitActivityDecoder> cachingFitActivityDecoderFactory)
    {
        _fitFilePathsValidator = IsFitFilePathsValid();
        _filePathsValidator = IsFilePathsValid();
        _folderPathValidator = IsFolderPathValid();
        _setValueOptions = SetValueOptions.Default with { IsRejectInvalidValueEnabled = true, IsThrowExceptionOnValidationErrorEnabled = true, IsRejectEqualValuesEnabled = true };
        ExportData = [];
        SelectedFitFilePaths = [];
        _selectedFitFilePath = string.Empty;
        _zipArchiveManager = zipArchiveManager;
        _garminFitCsvToolConverter = garminFitCsvToolConverter;
        _temporaryFileManager = temporaryFileManager;
        _cachingFitActivityDecoderFactory = cachingFitActivityDecoderFactory;
        DeleteExtraFileCommand = new RelayCommand<ObservableFileDescriptor>(fileDescriptor => SelectedExportData?.RemoveExtraFilePath(fileDescriptor), (fileDescriptor) => SelectedExportData is not null && SelectedExportData.SelectedExtraFilePaths.Contains(fileDescriptor));
        ExportCommand = new AsyncRelayCommand(ExecuteExportCommandAsync, CanExecuteExportCommand);
        StartNewSessionCommand = new RelayCommand(ExecuteStartNewSessionCommand);
        _allowedFileExtensions = _zipArchiveManager.SupportedArchiveFileExtensions.Concat([FitFileExtension]).JoinToString();
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
            return new PropertyValidationResult(false, [$"Invalid file type: only '{_allowedFileExtensions}' files are allowed. Found: '{fitFilePath}'."]);
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
        ArgumentExceptionAdvanced.ThrowIfNullOrEmpty(fitFilePaths);

        foreach (string fitFilePath in fitFilePaths)
        {
            if (_zipArchiveManager.IsFileTypeSupportedArchive(fitFilePath))
            {
                IProgress<ProgressData> progressReporter = StartNewObservableProgressReporting(string.Empty, $"Extracting '{Path.GetFileName(fitFilePath)}'...");
                await foreach (string extractedFilePath in _zipArchiveManager.ExtractArchiveAsync(fitFilePath, progressReporter, cancellationToken).ConfigureAwait(true))
                {
                    _ = await AddFitFilePathAsync(extractedFilePath, cancellationToken);
                }

                RemoveAllObservableProgressData();
            }
            else
            {
                _ = await AddFitFilePathAsync(fitFilePath, cancellationToken);
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
    public void SetAllActivityFieldsSelected(bool isSelected) => SelectedExportData?.SetAllActivityFieldsSelected(isSelected);
    public void SetAllRecordFieldsSelected(bool isSelected) => SelectedExportData?.SetAllRecordFieldsSelected(isSelected);
    public void SetAllSessionFieldsSelected(bool isSelected) => SelectedExportData?.SetAllSessionFieldsSelected(isSelected);
    public void SetAllLapFieldsSelected(bool isSelected) => SelectedExportData?.SetAllLapFieldsSelected(isSelected);

    private async Task<bool> AddFitFilePathAsync(string fitFilePath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fitFilePath))
        {
            return false;
        }

        PropertyValidationResult filePathValidationResult = IsFitFilePathValid(fitFilePath);
        if (!filePathValidationResult.IsValid
            || !SelectedFitFilePaths.Add(fitFilePath))
        {
            return false;
        }

        var exportData = new ExportData(_filePathsValidator, _cachingFitActivityDecoderFactory.Invoke());
        await exportData.SetFitFileAsync(fitFilePath, cancellationToken).ConfigureAwait(true);

        string csvFileName = $"{exportData.FitFileNameWithoutExtension}.csv";
        string temporaryDestinationFilePath = Path.Combine(Path.GetTempPath(), csvFileName);
        if (File.Exists(temporaryDestinationFilePath))
        {
            string temporaryUniqueFileName = _temporaryFileManager.MakeFileNameUnique(csvFileName);
            temporaryDestinationFilePath = Path.Combine(Path.GetTempPath(), temporaryUniqueFileName);
        }

        exportData.ExportedFilePath = temporaryDestinationFilePath;
        exportData.AddExtraFilePaths([temporaryDestinationFilePath], isRenamingRequired: false);

        ExportData.Add(exportData);
        SelectedExportData ??= exportData;

        return true;
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
            IEnumerable<FileDescriptor> fileDescriptors = exportData.SelectedExtraFilePaths
                .Select(observableFileDescriptor => observableFileDescriptor.ToFileDescriptor());

            string batchName = exportData.IsAutoRenamingEnabled || string.IsNullOrWhiteSpace(exportData.BatchName)
                ? exportData.AutoRenameBatchName
                : exportData.BatchName;
            int sourceFilesCount = exportData.SelectedExtraFilePaths.Count;
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

    public ObservableFileSystemPathHashSet SelectedFitFilePaths { get; }

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
