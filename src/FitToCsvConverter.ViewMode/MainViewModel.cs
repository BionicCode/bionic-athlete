namespace FitToCsvConverter.ViewModel;

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using BionicCode.Utilities.Net;
using FitToCsvConverter.Data;
using FitToCsvConverter.Data.Decoding;

public class MainViewModel : ViewModel, IDisposableAdvanced
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
    private readonly Dictionary<string, ExportData> _fitFilePathToExportDataLookup;
    private readonly string _allowedFileExtensions;
    private readonly SemaphoreSlim _addFitFilesSemaphore;

    public MainViewModel(IZipArchiveManager zipArchiveManager,
        IGarminFitCsvToolConverter garminFitCsvToolConverter,
        ITemporaryFileManager temporaryFileManager,
        Func<IFitActivityDecoder> cachingFitActivityDecoderFactory)
    {
        _addFitFilesSemaphore = new SemaphoreSlim(1, 1);
        _fitFilePathsValidator = IsFitFilePathsValid();
        _filePathsValidator = IsFilePathsValid();
        _folderPathValidator = IsFolderPathValid();
        _setValueOptions = SetValueOptions.Default with { IsRejectInvalidValueEnabled = true, IsThrowExceptionOnValidationErrorEnabled = true, IsRejectEqualValuesEnabled = true };
        ExportData = [];
        FitFilePaths = [];
        _fitFilePathToExportDataLookup = [];
        _selectedFitFilePath = string.Empty;
        _destinationFolder = string.Empty;
        _zipArchiveManager = zipArchiveManager;
        _garminFitCsvToolConverter = garminFitCsvToolConverter;
        _temporaryFileManager = temporaryFileManager;
        _cachingFitActivityDecoderFactory = cachingFitActivityDecoderFactory;
        ExportCommand = new AsyncRelayCommand(ExecuteExportCommandAsync, CanExecuteExportCommand);
        StartNewSessionCommand = new RelayCommand(ExecuteStartNewSessionCommand, () => !((IProgressReporter)this).IsReportingProgress);
        _allowedFileExtensions = _zipArchiveManager.SupportedArchiveFileExtensions.Concat([FitFileExtension]).JoinToString();
    }

    // For design-time data only
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public MainViewModel()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
        // CHeck if in debug mode and throw if not, to prevent usage of this constructor in production code.
#if !DEBUG
        throw new InvalidOperationException("This constructor is for design-time data only and should not be used in production code."); 
#endif
    }

    public void StartNewSession()
    {
        FitFilePaths.Clear();
        SelectedFitFilePath = string.Empty;
        ExportData.Clear();
        _fitFilePathToExportDataLookup.Clear();
        SelectedExportData = null;
        RemoveAllObservableProgressData();
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

    public PropertyValidationResult IsFilePathValid(string filePath)
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

        bool isSemaphoreEntered = false;
        var addedFitFilePathsLookup = new HashSet<string>();
        bool wasAdded = false;
        string addedFilePath = string.Empty;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            await _addFitFilesSemaphore.WaitAsync(cancellationToken);
            isSemaphoreEntered = true;

            IProgress<ProgressData> progressReporter = StartNewObservableProgressReporting(string.Empty, $"Adding files...", isIndeterminate: true);
            foreach (string fitFilePath in fitFilePaths)
            {
                cancellationToken.ThrowIfCancellationRequested();

                progressReporter.Report(new ProgressData { Message = $"Adding '{Path.GetFileName(fitFilePath)}'..." });
                if (_zipArchiveManager.IsFileTypeSupportedArchive(fitFilePath))
                {
                    await foreach (string extractedFilePath in _zipArchiveManager.ExtractArchiveAsync(fitFilePath, progressReporter, cancellationToken).ConfigureAwait(true))
                    {
                        wasAdded = await AddFitFilePathAsync(extractedFilePath, cancellationToken);
                        addedFilePath = extractedFilePath;
                    }

                    RemoveAllObservableProgressData();
                }
                else
                {
                    wasAdded = await AddFitFilePathAsync(fitFilePath, cancellationToken);
                    addedFilePath = fitFilePath;
                }

                if (wasAdded)
                {
                    _ = addedFitFilePathsLookup.Add(addedFilePath);
                }
            }
        }
        catch (OperationCanceledException)
        {
            for (int index = ExportData.Count - 1; index >= 0; index--)
            {
                ExportData exportData = ExportData[index];
                if (addedFitFilePathsLookup.Contains(exportData.FitFilePath))
                {
                    ExportData.RemoveAt(index);
                    _ = FitFilePaths.Remove(exportData.FitFilePath);
                }
            }

            throw;
        }
        finally
        {
            if (isSemaphoreEntered)
            {
                _ = _addFitFilesSemaphore.Release();
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

    public void RemoveFitFilePath(string filePath)
    {
        if (FitFilePaths.Contains(filePath))
        {
            Debug.Assert(_fitFilePathToExportDataLookup.ContainsKey(filePath), $"File path '{filePath}' is in '{nameof(FitFilePaths)}' collection but not in '{nameof(_fitFilePathToExportDataLookup)}' lookup.");
            if (FitFilePaths.Remove(filePath)
                && _fitFilePathToExportDataLookup.TryGetValue(filePath, out ExportData? exportData))
            {
                _ = _fitFilePathToExportDataLookup.Remove(filePath);
                _ = ExportData.Remove(exportData);
                SelectedExportData = ExportData.FirstOrDefault();
            }
        }
        else
        {
            throw new InvalidOperationException($"File path '{filePath}' not found in collection '{nameof(FitFilePaths)}'.");
        }
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
            || !FitFilePaths.Add(fitFilePath))
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
        _fitFilePathToExportDataLookup.Add(fitFilePath, exportData);
        SelectedExportData ??= exportData;

        return true;
    }

    private bool CanExecuteExportCommand() => ExportData.Any()
        && !string.IsNullOrEmpty(DestinationFolder)
        && FitFilePaths.Any();

    private async Task ExecuteExportCommandAsync()
    {
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
        set => _ = TrySetValue(value, value => value is null || FitFilePaths.IsEmpty() || FitFilePaths.Contains(value) ? new PropertyValidationResult(true, []) : new PropertyValidationResult(false, ["Selected file is not in the list of fit files."]), ref _selectedFitFilePath, _setValueOptions);
    }

    public ObservableFileSystemPathHashSet FitFilePaths { get; }
    public IAsyncRelayCommand ExportCommand { get; }
    public IRelayCommand StartNewSessionCommand { get; }
    public bool IsDisposed { get; private set; }
    public ObservableCollection<ExportData> ExportData { get; private set; }
    public ExportData? SelectedExportData
    {
        get => _selectedExportData;
        set
        {
            if (TrySetValue(value, ref _selectedExportData))
            {
                SelectedFitFilePath = value?.FitFilePath;
            }
        }
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

    private PropertyValidationDelegate<string> IsFilePathsValid() => filePath =>
        {
            ArgumentNullExceptionAdvanced.ThrowIfNull(filePath);

            PropertyValidationResult result = IsFilePathValid(filePath);
            return new PropertyValidationResult(true, Array.Empty<string>());
        };

    private static PropertyValidationDelegate<string> IsFolderPathValid() => folderPath => new PropertyValidationResult(
        !string.IsNullOrWhiteSpace(folderPath) && Directory.Exists(folderPath),
        ["Destination folder cannot be empty and must exist."]);

    protected virtual void Dispose(bool disposing)
    {
        if (!IsDisposed)
        {
            if (disposing)
            {
                _addFitFilesSemaphore.Dispose();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            IsDisposed = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~MainWindow()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
