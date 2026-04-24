namespace FitToCsvConverter.ViewModel;

using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using BionicCode.Utilities.Net;
using FitToCsvConverter.Data;
using FitToCsvConverter.Data.Activities;
using FitToCsvConverter.Data.Decoding;
using FitToCsvConverter.Data.Exporting;
using FitToCsvConverter.Data.Fields;

public class ExportData : ViewModel
{
    private const char DefaultCsvDelimiter = ',';
    private readonly PropertyValidationDelegate<string> _filePathsValidator;
    private readonly HashSet<string> _newFilenames = [];
    private readonly ObservableHashSet<DataField> _activityFields;
    private readonly ObservableHashSet<DataField> _sessionFields;
    private readonly ObservableHashSet<DataField> _recordFields;
    private readonly ObservableHashSet<DataField> _lapFields;
    private readonly ObservableHashSet<ObservableFileDescriptor> _selectedExtraFilePaths;
    private string _batchName;
    private bool _hasCorrectedDuplicateNewNames;
    private bool _isAutoRenamingEnabled;
    private string? _fitFileName;
    private string? _autoRenameBatchName;
    private string? _fitFileNameWithoutExtension;
    private bool _isIncludeFitFileEnabled;
    private string _fitFilePath;
    private ImmutableArray<ExportedArtifact> _exportedArtifacts;
    private readonly IFitActivityDecoder _fitActivityDecoder;

    public ObservableFileDescriptor FitFileDescriptor { get; private set; } = null!;

    private readonly SetValueOptions _setValueOptions;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0028:Simplify collection initialization", Justification = "Feature not available.")]
    public ExportData(PropertyValidationDelegate<string> filePathsValidator,
        IFitActivityDecoder fitActivityDecoder)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(filePathsValidator);
        ArgumentNullExceptionAdvanced.ThrowIfNull(fitActivityDecoder);

        _filePathsValidator = filePathsValidator;
        _fitActivityDecoder = fitActivityDecoder;
        _newFilenames = [];
        _fitFilePath = string.Empty;
        _batchName = string.Empty;
        _exportedArtifacts = ImmutableArray<ExportedArtifact>.Empty;
        _isAutoRenamingEnabled = true;
        _isIncludeFitFileEnabled = true;

        _setValueOptions = new SetValueOptions
        {
            IsRejectEqualValuesEnabled = true,
            IsThrowExceptionOnValidationErrorEnabled = true,
        };

        _selectedExtraFilePaths = [];
        SelectedExtraFilePaths = new(_selectedExtraFilePaths);
        SelectedExtraFilePaths.CollectionChanged += OnSelectedExtraFilePathsCollectionChanged;
        SelectedExtraFilePaths.CollectionChanged += ValidateOnItemAdded;

        var dataFieldComparer = EqualityComparer<DataField>.Create(IsDataFieldEqual, GetHashCodeForDataField);
        _activityFields = new ObservableHashSet<DataField>(dataFieldComparer);
        ActivityFields = new(_activityFields);
        _sessionFields = new ObservableHashSet<DataField>(dataFieldComparer);
        SessionFields = new(_sessionFields);
        _recordFields = new ObservableHashSet<DataField>(dataFieldComparer);
        RecordFields = new(_recordFields);
        _lapFields = new ObservableHashSet<DataField>(dataFieldComparer);
        LapFields = new(_lapFields);

        DeleteExtraFileCommand = new RelayCommand<ObservableFileDescriptor>(
            execute: RemoveExtraFilePath,
            canExecute: SelectedExtraFilePaths.Contains);
    }

    public async Task SetFitFileAsync(string fitFilePath, CancellationToken cancellationToken)
    {
        FitFilePath = fitFilePath;
        await CreateDataViewsAsync(FitFilePath, cancellationToken);
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
            _ = _selectedExtraFilePaths.Add(fileDescriptor);
        }
    }

    public void RemoveExtraFilePath(ObservableFileDescriptor fileDescriptor)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(fileDescriptor);

        _ = _selectedExtraFilePaths.Remove(fileDescriptor);
    }

    private async Task CreateDataViewsAsync(string fitFilePath, CancellationToken cancellationToken)
    {
        FitActivityDecodeResult result = await _fitActivityDecoder.DecodeFileAsync(fitFilePath, cancellationToken);
        Activity = result.GetActivityOrThrowIfDecodingFailed();
        int activityFieldDisplayOrder = 0;
        int sessionFieldDisplayOrder = 0;
        int recordFieldDisplayOrder = 0;
        int lapFieldDisplayOrder = 0;

        foreach (FitField field in Activity.Fields)
        {
            var dataField = new DataField(field, activityFieldDisplayOrder++);
            if (IsFieldValid(dataField))
            {
                _ = _activityFields.Add(dataField);
            }
        }

        foreach (FitSession session in Activity.Sessions)
        {
            foreach (FitField field in session.Fields)
            {
                var dataField = new DataField(field, sessionFieldDisplayOrder++);
                if (IsFieldValid(dataField))
                {
                    _ = _sessionFields.Add(dataField);
                }
            }

            foreach (FitRecord record in session.Records)
            {
                foreach (FitField field in record.Fields)
                {
                    var dataField = new DataField(field, recordFieldDisplayOrder++);
                    if (IsFieldValid(dataField))
                    {
                        _ = _recordFields.Add(dataField);
                    }
                }
            }

            foreach (FitLap lap in session.Laps)
            {
                foreach (FitField field in lap.Fields)
                {
                    var dataField = new DataField(field, lapFieldDisplayOrder++);
                    if (IsFieldValid(dataField))
                    {
                        _ = _lapFields.Add(dataField);
                    }
                }
            }
        }
    }

    private static bool IsFieldValid([NotNullWhen(true)] DataField? field) => field is not null
        && !string.IsNullOrWhiteSpace(field.Name);

    private bool IsDataFieldEqual(DataField? field1, DataField? field2)
    {
        if (ReferenceEquals(field1, field2))
        {
            return true;
        }

        if (field1 is null || field2 is null)
        {
            return false;
        }

        return field1.Name.Equals(field2.Name, StringComparison.OrdinalIgnoreCase)
            && field1.Id == field2.Id;
    }

    private int GetHashCodeForDataField(DataField? field) => field is null
        ? 0
        : HashCode.Combine(field.Name, field.Id);

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
            // DateTime dataDate = FitFileAnalyzer.GetSessionDate(FitFilePath);
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
                foreach (ObservableFileDescriptor fileDescriptor in e.NewItems?.OfType<ObservableFileDescriptor>() ?? [])
                {
                    PropertyValidationResult validationResult = _filePathsValidator.Invoke(fileDescriptor.OriginalFullPath);
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

    /// <summary>
    /// Creates a CSV export request from the current UI-facing export state.
    /// </summary>
    /// <param name="outputDirectoryPath">The temporary output directory for generated CSV files.</param>
    /// <returns>The CSV export request that represents the current field selections.</returns>
    internal CsvExportRequest CreateCsvExportRequest(string outputDirectoryPath)
    {
        ArgumentExceptionAdvanced.ThrowIfNullOrWhiteSpace(outputDirectoryPath);

        if (Activity is null)
        {
            throw new InvalidOperationException("A decoded activity must be available before a CSV export request can be created.");
        }

        ImmutableArray<CsvExportColumnRequest>.Builder columnRequests = ImmutableArray.CreateBuilder<CsvExportColumnRequest>(
            _activityFields.Count + _sessionFields.Count + _lapFields.Count + _recordFields.Count);
        AddColumnRequests(_activityFields, columnRequests);
        AddColumnRequests(_sessionFields, columnRequests);
        AddColumnRequests(_lapFields, columnRequests);
        AddColumnRequests(_recordFields, columnRequests);

        return CsvExportRequestFactory.Create(
            Activity,
            FitFileNameWithoutExtension,
            outputDirectoryPath,
            columnRequests.ToImmutable(),
            options: new FitExportOptions(target: FitExportTarget.StructuredCsv),
            delimiter: DefaultCsvDelimiter);
    }

    /// <summary>
    /// Replaces the generated export artifacts that should be packaged into the ZIP archive.
    /// </summary>
    /// <param name="exportedArtifacts">The generated export artifacts from the latest export run.</param>
    internal void SetExportArtifacts(ImmutableArray<ExportedArtifact> exportedArtifacts)
        => _exportedArtifacts = exportedArtifacts.IsDefault ? ImmutableArray<ExportedArtifact>.Empty : exportedArtifacts;

    /// <summary>
    /// Enumerates the files that should be packaged for this export batch.
    /// </summary>
    /// <returns>The generated export artifacts followed by the user-selected extra files.</returns>
    internal IEnumerable<FileDescriptor> EnumerateArchiveFileDescriptors()
    {
        // Preserve exporter order so ancillary families and the manifest keep a stable bundle layout.
        foreach (ExportedArtifact exportedArtifact in _exportedArtifacts)
        {
            yield return new FileDescriptor(exportedArtifact.FilePath, isRenamingRequired: false, exportedArtifact.BundlePath);
        }

        foreach (ObservableFileDescriptor observableFileDescriptor in SelectedExtraFilePaths)
        {
            yield return observableFileDescriptor.ToFileDescriptor();
        }
    }

    private static void AddColumnRequests(
        IEnumerable<DataField> fields,
        ImmutableArray<CsvExportColumnRequest>.Builder columnRequests)
    {
        foreach (DataField field in fields)
        {
            columnRequests.Add(CreateColumnRequest(field));
        }
    }

    private static CsvExportColumnRequest CreateColumnRequest(DataField field)
        => new(
            field.FitField.Original.ExportColumnKey,
            field.FitField.Original.OriginalName,
            field.FitField.State.ColumnName,
            field.DisplayOrder,
            field.IsSelected);

    internal void SetAllActivityFieldsSelected(bool isSelected)
    {
        foreach (DataField field in _activityFields)
        {
            field.IsSelected = isSelected;
        }
    }

    internal void SetAllRecordFieldsSelected(bool isSelected)
    {
        foreach (DataField field in _recordFields)
        {
            field.IsSelected = isSelected;
        }
    }

    internal void SetAllSessionFieldsSelected(bool isSelected)
    {
        foreach (DataField field in _sessionFields)
        {
            field.IsSelected = isSelected;
        }
    }

    internal void SetAllLapFieldsSelected(bool isSelected)
    {
        foreach (DataField field in _lapFields)
        {
            field.IsSelected = isSelected;
        }
    }

    public void ClearExtraFilePaths() => _selectedExtraFilePaths.Clear();

    public void SetRenameAllExtraFiles(bool isRenamingEnabled)
    {
        foreach (ObservableFileDescriptor fileDescriptor in _selectedExtraFilePaths)
        {
            fileDescriptor.IsRenamingEnabled = isRenamingEnabled;
        }
    }

    public IRelayCommand<ObservableFileDescriptor> DeleteExtraFileCommand { get; }

    public string FitFilePath
    {
        get => _fitFilePath;
        private set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            _fitFilePath = value;
            if (FitFileDescriptor is not null)
            {
                _ = _selectedExtraFilePaths.Remove(FitFileDescriptor);
            }

            FitFileDescriptor = new ObservableFileDescriptor(FitFilePath, isRenamingRequired: false);
            if (IsIncludeFitFileEnabled)
            {
                _ = _selectedExtraFilePaths.Add(FitFileDescriptor);
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
        set
        {
            if (TrySetValue(value, ref _isAutoRenamingEnabled, _setValueOptions))
            {
                RenameAllFiles();
            }
        }
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
                    _ = _selectedExtraFilePaths.Add(FitFileDescriptor);
                }
                else
                {
                    _ = _selectedExtraFilePaths.Remove(FitFileDescriptor);
                }
            }
        }
    }

    internal FitActivity Activity { get; private set; } = null!;
    public ReadOnlyObservableHashSet<DataField> ActivityFields { get; }
    public ReadOnlyObservableHashSet<DataField> SessionFields { get; }
    public ReadOnlyObservableHashSet<DataField> RecordFields { get; }
    public ReadOnlyObservableHashSet<DataField> LapFields { get; }

    public ReadOnlyObservableHashSet<ObservableFileDescriptor> SelectedExtraFilePaths { get; }
    public string AutoRenameBatchName => _autoRenameBatchName ??= GetAutoRenameBatchName();
}
