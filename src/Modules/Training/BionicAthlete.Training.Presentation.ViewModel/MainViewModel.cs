namespace BionicAthlete.Training.Presentation.ViewModel;

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using BionicAthlete.Application.Exporting;
using BionicAthlete.Application.Reporting;
using BionicAthlete.Application.Reporting.Html;
using BionicAthlete.FileSystem.Abstractions;
using BionicAthlete.Training.Application.Decoding;
using BionicAthlete.Training.Application.Reporting;
using BionicAthlete.Training.Exporting;
using BionicCode.Utilities.Net;

public class MainViewModel : ViewModel, IDisposableAdvanced, IDisposable
{
    private const string CoachingContextFileName = "coach_context.md";
    private string _destinationFolder;
    private ObservableFitActivityExportData? _selectedExportData;
    private string? _selectedFitFilePath;
    private readonly PropertyValidationDelegate<ObservableFileSystemPathHashSet> _fitFilePathsValidator;
    private readonly PropertyValidationDelegate<string> _filePathsValidator;
    private readonly PropertyValidationDelegate<string> _folderPathValidator;
    private readonly SetValueOptions _setValueOptions;
    private readonly IArchiveManager _zipArchiveManager;
    private readonly ICsvActivityExporter _csvActivityExporter;
    private readonly ITemporaryFileManager _temporaryFileManager;
    private readonly Func<IFitActivityDecoder> _cachingFitActivityDecoderFactory;
    private readonly Dictionary<string, ObservableFitActivityExportData> _fitFilePathToExportDataLookup;
    private readonly Dictionary<string, string> _pdfFilePathToManifestFilePathMap;
    private readonly string _allowedFileExtensions;
    private readonly SemaphoreSlim _addFitFilesSemaphore;
    private readonly FitActivityReportCreator _fitActivityReportCreator;
    private static readonly FileStreamOptions s_createFileStreamOptions = new()
    {
        Mode = FileMode.CreateNew,
        Access = FileAccess.Write,
        Share = FileShare.None,
        Options = FileOptions.Asynchronous | FileOptions.SequentialScan
    };

    public MainViewModel(IZipArchiveManager zipArchiveManager,
        ICsvActivityExporter csvActivityExporter,
        IActivityReportProjector activityReportProjector,
        IReportHtmlRenderer activityReportHtmlRenderer,
        ITemporaryFileManager temporaryFileManager,
        Func<IFitActivityDecoder> cachingFitActivityDecoderFactory,
        IReportManifestHandler manifestHandler,
        IHtmlExporter htmlExporter,
        IHtmlExporterArgsFactory htmlExporterArgsFactory,
        FitActivityReportCreator fitActivityReportCreator)
    {
        _temporaryFileManager = temporaryFileManager;
        _zipArchiveManager = zipArchiveManager;
        _csvActivityExporter = csvActivityExporter;
        _cachingFitActivityDecoderFactory = cachingFitActivityDecoderFactory;
        _addFitFilesSemaphore = new SemaphoreSlim(1, 1);
        _fitFilePathsValidator = IsFitFilePathsValid();
        _filePathsValidator = IsFilePathsValid();
        _folderPathValidator = IsFolderPathValid();
        _setValueOptions = SetValueOptions.Default with { IsRejectInvalidValueEnabled = true, IsThrowExceptionOnValidationErrorEnabled = true, IsRejectEqualValuesEnabled = true };
        ExportData = [];
        FitFilePaths = [];
        _fitFilePathToExportDataLookup = [];
        _selectedFitFilePath = string.Empty;
        _destinationFolder = _temporaryFileManager.TemporaryDirectoryPath;
        ExportCommand = new AsyncRelayCommand(ExecuteExportToCsvCommandAsync, CanExecuteExportToCsvCommand);
        StartNewSessionCommand = new RelayCommand(ExecuteStartNewSessionCommand, () => !((IProgressReporter)this).IsReportingProgress);
        _allowedFileExtensions = _zipArchiveManager.SupportedArchiveFileExtensions.Concat([FitFileExtension]).JoinToString();
        _pdfFilePathToManifestFilePathMap = [];
        _fitActivityReportCreator = fitActivityReportCreator;
    }

#if DEBUG
    // For design-time data only
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public MainViewModel()
        // Check if in debug mode and throw if not, to prevent usage of this constructor in production code.
        => throw new InvalidOperationException("This constructor is for design-time data only and should not be used in production code.");
#endif

    public void StartNewSession()
    {
        FitFilePaths.Clear();
        SelectedFitFilePath = string.Empty;
        ExportData.Clear();
        _fitFilePathToExportDataLookup.Clear();
        SelectedExportData = null;

        RemoveAllObservableProgressData();
    }

    public PropertyValidationResult IsFitFilePathValid(FileDescriptor fitFilePath)
    {
        PropertyValidationResult result = IsFilePathValid(fitFilePath);
        if (!result.IsValid)
        {
            return result;
        }

        if (!fitFilePath.Extension.Equals(FileExtensions.Fit)
            && !_zipArchiveManager.IsFileTypeSupportedArchive(fitFilePath))
        {
            return new PropertyValidationResult(false, [$"Invalid file type: only '{_allowedFileExtensions}' files are allowed. Found: '{fitFilePath}'."]);
        }

        return new PropertyValidationResult(true, Array.Empty<string>());
    }

    public static PropertyValidationResult IsFilePathValid(FileDescriptor filePath)
    {
        if (!filePath.IsExisting)
        {
            return new PropertyValidationResult(false, [$"Invalid file path: '{filePath}'."]);
        }

        return new PropertyValidationResult(true, Array.Empty<string>());
    }

    public async Task AddFitFilePathsAsync(IList<FileDescriptor> fitFilePaths, CancellationToken cancellationToken)
    {
        ArgumentExceptionAdvanced.ThrowIfNullOrEmpty(fitFilePaths);

        RemoveAllCompletedObservableProgressData();

        bool isSemaphoreEntered = false;
        var addedFitFilePathsLookup = new HashSet<string>();
        bool wasAdded = false;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            await _addFitFilesSemaphore.WaitAsync(cancellationToken);
            isSemaphoreEntered = true;

            IProgress<ProgressData> addFileProgressReporter = StartNewObservableProgressReporting(string.Empty, $"Adding .fit files...", isIndeterminate: false, maxValue: fitFilePaths.Count);
            for (int index = 0; index < fitFilePaths.Count; index++)
            {
                FileDescriptor fitFilePath = fitFilePaths[index];
                cancellationToken.ThrowIfCancellationRequested();

                addFileProgressReporter.Report(new ProgressData(index + 1, fitFilePaths.Count, $"Adding '{fitFilePath}'..."));
                if (_zipArchiveManager.IsFileTypeSupportedArchive(fitFilePath))
                {
                    await foreach (FileDescriptor extractedFilePath in _zipArchiveManager.ExtractArchiveAsync(fitFilePath, (int maxValue, string operationTitle) => StartNewObservableProgressReporting(string.Empty, operationTitle, isIndeterminate: false, maxValue: maxValue), cancellationToken).ConfigureAwait(true))
                    {
                        wasAdded = await AddFitFilePathAsync(extractedFilePath, cancellationToken);
                        if (wasAdded)
                        {
                            _ = addedFitFilePathsLookup.Add(extractedFilePath);
                        }
                    }
                }
                else
                {
                    wasAdded = await AddFitFilePathAsync(fitFilePath, cancellationToken);
                    if (wasAdded)
                    {
                        _ = addedFitFilePathsLookup.Add(fitFilePath);
                    }
                }
            }

            addFileProgressReporter.Report(new ProgressData(fitFilePaths.Count, fitFilePaths.Count, "Completed adding .fit files.", isIndeterminate: false));
        }
        catch (OperationCanceledException)
        {
            for (int index = ExportData.Count - 1; index >= 0; index--)
            {
                ObservableFitActivityExportData exportData = ExportData[index];
                if (addedFitFilePathsLookup.Contains(exportData.FitFilePath))
                {
                    ExportData.RemoveAt(index);
                    _ = FitFilePaths.Remove(exportData.FitFilePath);
                    _ = _fitFilePathToExportDataLookup.Remove(exportData.FitFilePath);
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

    public void AddExtraFilePaths(ObservableFitActivityExportData exportData, string[] filePaths)
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

    public void RemoveAllFitFilePaths()
    {
        RemoveAllCompletedObservableProgressData();

        SelectedExportData = null;
        SelectedFitFilePath = string.Empty;
        FitFilePaths.Clear();
        _fitFilePathToExportDataLookup.Clear();
        ExportData.Clear();
    }

    public void RemoveFitFilePath(string filePath)
    {
        RemoveAllCompletedObservableProgressData();

        if (FitFilePaths.Contains(filePath))
        {
            Debug.Assert(_fitFilePathToExportDataLookup.ContainsKey(filePath), $"File path '{filePath}' is in '{nameof(FitFilePaths)}' collection but not in '{nameof(_fitFilePathToExportDataLookup)}' lookup.");
            if (FitFilePaths.Remove(filePath)
                && _fitFilePathToExportDataLookup.TryGetValue(filePath, out ObservableFitActivityExportData? exportData))
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

    /// <summary>
    /// Creates the neutral View C HTML report package for the selected decoded activity and saves it to the specified <paramref fileNameWithoutExtension="outputTarget"/>.
    /// </summary>
    /// <param fileNameWithoutExtension="exportData">The decoded activity and UI-facing batch state to export.</param>
    /// <param fileNameWithoutExtension="outputTarget">The requested View C output target.</param>
    /// <param fileNameWithoutExtension="cancellationToken">Cancellation token.</param>
    /// <returns>The generated HTML report package.</returns>
    public async Task<HtmlReportPackage> CreateHtmlReportAsync(
        ObservableFitActivityExportData exportData,
        ReportOutputTarget outputTarget,
        bool isOverWriteExistingAllowed,
        CancellationToken cancellationToken)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(exportData);

        DirectoryDescriptor outputDirectoryPath = CreateReportOutputDirectory(exportData);
        var options = new ReportExportOptions(
            outputDirectoryPath,
            outputTarget,
            CultureInfo.CurrentCulture,
            TimeZoneInfo.Local,
            DateTimeOffset.UtcNow,
            PageSettings.A4Portrait);

        var fitFileDescriptor = exportData.FitFileDescriptor.ToFileDescriptor();
        var exportArgs = new FitFileExportData(fitFileDescriptor, exportData.Activity, outputDirectoryPath, options);

        // TODO::Add progress reporting and cancellation support to the report creation and export process.
        return await _fitActivityReportCreator.CreateHtmlReportAsync(exportArgs, outputTarget, isOverWriteExistingAllowed, cancellationToken);
    }

    /// <summary>
    /// Creates the neutral View C HTML report package for the selected decoded activity 
    /// and saves it to the specified <paramref fileNameWithoutExtension="outputTarget"/> 
    /// and returns a <see cref="PdfExportRequest"/> which can be used to finalize 
    /// the export using the <see cref="IReportPdfExporter"/> export service.
    /// </summary>
    /// <param fileNameWithoutExtension="exportData">The decoded activity and UI-facing batch state to export.</param>
    /// <param fileNameWithoutExtension="outputTarget">The requested View C output target.</param>
    /// <param fileNameWithoutExtension="cancellationToken">Cancellation token.</param>
    /// <returns>The generated HTML report package.</returns>
    public async Task<PdfExportRequest> CreatePdfExportRequestAsync(
        ObservableFitActivityExportData exportData,
        ReportOutputTarget outputTarget,
        bool isOverWriteExistingAllowed,
        CancellationToken cancellationToken)
    {
        // TODO::Improve the design of this method and move domain logic related to report generation and export request creation out of the ViewModel layer.
        ArgumentNullExceptionAdvanced.ThrowIfNull(exportData);
        ArgumentExceptionAdvanced.ThrowIfTrue(outputTarget is ReportOutputTarget.HtmlOnly, "Output target must be PDF or HTML and PDF.");

        var fitFileDescriptor = exportData.FitFileDescriptor.ToFileDescriptor();
        DirectoryDescriptor outputDirectoryPath = CreateReportOutputDirectory(exportData);
        var options = new ReportExportOptions(
            outputDirectoryPath,
            outputTarget,
            CultureInfo.CurrentCulture,
            TimeZoneInfo.Local,
            DateTimeOffset.UtcNow,
            PageSettings.A4Portrait);
        var exportArgs = new FitFileExportData(fitFileDescriptor, exportData.Activity, outputDirectoryPath, options);
        return await _fitActivityReportCreator.CreatePdfExportRequestAsync(exportArgs, outputTarget, isOverWriteExistingAllowed, cancellationToken).ConfigureAwait(true);
    }

    public void UpdateManifestWithPdfEntry() => throw new NotImplementedException();

    private async Task<bool> AddFitFilePathAsync(FileDescriptor fitFilePath, CancellationToken cancellationToken)
    {
        PropertyValidationResult filePathValidationResult = IsFitFilePathValid(fitFilePath);
        if (FitFilePaths.Contains(fitFilePath)
            || !filePathValidationResult.IsValid)
        {
            return false;
        }

        var exportData = new ObservableFitActivityExportData(_filePathsValidator, _cachingFitActivityDecoderFactory.Invoke(), CoachingContextFileName);
        await exportData.SetFitFileAsync(fitFilePath, cancellationToken).ConfigureAwait(true);

        _ = FitFilePaths.Add(fitFilePath);
        ExportData.Add(exportData);
        _fitFilePathToExportDataLookup.Add(fitFilePath, exportData);
        SelectedExportData ??= exportData;

        return true;
    }

    private bool CanExecuteExportToCsvCommand() => ExportData.Any()
        && !string.IsNullOrEmpty(DestinationFolder)
        && FitFilePaths.Any();

    private async Task ExecuteExportToCsvCommandAsync()
    {
        RemoveAllCompletedObservableProgressData();

        IProgress<ProgressData> exportProgressReporter = StartNewObservableProgressReporting(
            string.Empty,
            "Export FIT to CSV",
            isIndeterminate: false,
            maxValue: ExportData.Count);

        int completedCount = 0;
        foreach (ObservableFitActivityExportData exportData in ExportData)
        {
            exportProgressReporter.Report(new ProgressData(
                progress: completedCount,
                maxValue: ExportData.Count,
                message: $"Exporting decoded activity {completedCount + 1} of {ExportData.Count}: {exportData.FitFileName}"));

            DirectoryDescriptor exportOutputDirectoryPath = CreateExportOutputDirectory(exportData.FitFileNameWithoutExtension);
            CsvExportRequest exportRequest = exportData.CreateCsvExportRequest(exportOutputDirectoryPath);
            CsvExportResult exportResult = await _csvActivityExporter.ExportAsync(exportRequest).ConfigureAwait(true);
            exportData.SetExportArtifacts(exportResult.ExportedArtifacts);

            foreach (ExportedArtifact exportedArtifact in exportResult.ExportedArtifacts)
            {
                _temporaryFileManager.RegisterTemporaryFilePath(exportedArtifact.FilePath);
            }

            completedCount++;
            exportProgressReporter.Report(new ProgressData(
                progress: completedCount,
                maxValue: ExportData.Count,
                message: $"Completed exporting decoded activity {completedCount} of {ExportData.Count}: {exportData.FitFileName}"));
        }

        await CreateArchivesAsync();
    }

    private DirectoryDescriptor CreateExportOutputDirectory(string fitFileNameWithoutExtension)
    {
        // Keep generated CSV file names stable inside the archive while isolating each export run in its own
        // temporary directory to avoid collisions between activities that share the same source file fileNameWithoutExtension.
        string directoryName = _temporaryFileManager.MakeFileNameUnique(fitFileNameWithoutExtension);
        string exportOutputDirectoryPath = Path.Combine(_temporaryFileManager.TemporaryDirectoryPath.FullPath, directoryName);
        return new DirectoryDescriptor(exportOutputDirectoryPath);
    }

    private DirectoryDescriptor CreateReportOutputDirectory(ObservableFitActivityExportData exportData)
    {
        string batchName = exportData.IsAutoRenamingEnabled || string.IsNullOrWhiteSpace(exportData.BatchName)
            ? exportData.AutoRenameBatchName
            : exportData.BatchName;
        string baseDirectoryName = $"{SanitizeFileNameSegment(batchName)}_report";
        string outputDirectoryPath = Path.Combine(DestinationFolder, baseDirectoryName);
        int duplicateCounter = 1;

        while (Directory.Exists(Path.Combine(outputDirectoryPath, "report")))
        {
            string alternativeDirectoryName = $"{baseDirectoryName}_{duplicateCounter.ToString(CultureInfo.InvariantCulture)}";
            outputDirectoryPath = Path.Combine(DestinationFolder, alternativeDirectoryName);
            duplicateCounter++;
        }

        return new DirectoryDescriptor(outputDirectoryPath);
    }

    private async Task CreateArchivesAsync()
    {
        List<ArchiveContentBatch> batchesToArchive = await EnumerateFileBatchesAsync().ToListAsync();
        if (batchesToArchive.Count == 0)
        {
            return;
        }

        var batches = new FileBatches(batchesToArchive, batchesToArchive.Count);
        IProgress<ProgressData> packProgressReporter = StartNewObservableProgressReporting(string.Empty, "Pack files to ZIP archives.");
        await _zipArchiveManager.CreateArchivesAsync(batches, packProgressReporter);
    }

    private async IAsyncEnumerable<ArchiveContentBatch> EnumerateFileBatchesAsync()
    {
        foreach (ObservableFitActivityExportData exportData in ExportData)
        {
            var fileDescriptors = exportData.EnumerateArchiveFileDescriptors().ToList();
            if (fileDescriptors.Count == 0)
            {
                continue;
            }

            string batchName = exportData.IsAutoRenamingEnabled || string.IsNullOrWhiteSpace(exportData.BatchName)
                ? exportData.AutoRenameBatchName
                : exportData.BatchName;

            if (!string.IsNullOrWhiteSpace(exportData.ReportSummary))
            {
                string fileName = string.IsNullOrWhiteSpace(exportData.ReportSummaryFileName)
                    ? CoachingContextFileName
                    : exportData.ReportSummaryFileName;
                string destinationFilePath = _temporaryFileManager.CreateTemporaryFilePath(batchName, fileName);
                _temporaryFileManager.RegisterTemporaryFilePath(destinationFilePath);
                await using var stream = new FileStream(destinationFilePath, s_createFileStreamOptions);
                await using var writer = new StreamWriter(stream, Encoding.UTF8);
                await writer.WriteAsync(exportData.ReportSummary).ConfigureAwait(true);
                var reportSummaryFileDescriptor = new FileDescriptor(destinationFilePath, isRenamingRequired: false);
                fileDescriptors.Add(reportSummaryFileDescriptor);
            }

            var batch = new ArchiveContentBatch(
                fileDescriptors,
                fileDescriptors.Count,
                DestinationFolder,
                batchName,
                Encoding.UTF8,
                CompressionLevel.SmallestSize);

            yield return batch;
        }
    }

    private void ExecuteStartNewSessionCommand() => StartNewSession();

    private static string SanitizeFileNameSegment(string value)
    {
        char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
        char[] sanitizedChars = value.Select(character => invalidFileNameChars.Contains(character) ? '_' : character).ToArray();
        string sanitizedValue = new(sanitizedChars);
        return string.IsNullOrWhiteSpace(sanitizedValue) ? "activity" : sanitizedValue.Trim();
    }

    public string DestinationFolder
    {
        get => _destinationFolder;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                _destinationFolder = _temporaryFileManager.TemporaryDirectoryPath;
            }
            else
            {
                _ = TrySetValue(value, _folderPathValidator, ref _destinationFolder, _setValueOptions);
            }
        }
    }

    public string? SelectedFitFilePath
    {
        get => _selectedFitFilePath;
        set => _ = TrySetValue(value, value => string.IsNullOrEmpty(value) || FitFilePaths.Contains(value) ? new PropertyValidationResult(true, []) : new PropertyValidationResult(false, ["Selected file is not in the list of fit files."]), ref _selectedFitFilePath, _setValueOptions);
    }

    public ObservableFileSystemPathHashSet FitFilePaths { get; }
    public IAsyncRelayCommand ExportCommand { get; }
    public IRelayCommand StartNewSessionCommand { get; }
    public bool IsDisposed { get; private set; }
    public ObservableCollection<ObservableFitActivityExportData> ExportData { get; private set; }
    public ObservableFitActivityExportData? SelectedExportData
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

    private static PropertyValidationDelegate<ObservableFileSystemPathHashSet> IsFitFilePathsValid() => fitFilePaths =>
        {
            if (fitFilePaths.Count == 0)
            {
                return new PropertyValidationResult(false, ["At least one file must be selected."]);
            }

            foreach (string fitFilePath in fitFilePaths)
            {
                PropertyValidationResult validationResult = IsFitFilePathValid(fitFilePath);
                if (!validationResult.IsValid)
                {
                    return validationResult;
                }
            }

            return new PropertyValidationResult(true, Array.Empty<string>());
        };

    private static PropertyValidationDelegate<string> IsFilePathsValid() => filePath =>
        {
            ArgumentNullExceptionAdvanced.ThrowIfNull(filePath);

            return IsFilePathValid(filePath);
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
