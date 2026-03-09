namespace FitToCsvConverter.ViewModel;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using BionicCode.Utilities.Net;

public class MainViewModel : ViewModel
{
    public const string DefaultDestinationFolder = @"C:\Temp\FitToCsvConverterOutput";
    private const string FitFileExtension = ".fit";
    private string _destinationFolder;
    private HashSet<string> _selectedFitFilePaths;
    private readonly PropertyValidationDelegate<HashSet<string>> _fitFilePathsValidator;
    private readonly PropertyValidationDelegate<ObservableCollection<string>> _filePathsValidator;
    private readonly PropertyValidationDelegate<string> _folderPathValidator;
    private readonly SetValueOptions _setValueOptions;

    public MainViewModel()
    {
        _fitFilePathsValidator = IsFitFilePathValid();
        _filePathsValidator = IsFilePathValid();
        _folderPathValidator = IsFolderPathValid();
        _setValueOptions = SetValueOptions.Default with { IsRejectInvalidValueEnabled = true, IsThrowExceptionOnValidationErrorEnabled = true, IsRejectEqualValuesEnabled = true };
        _destinationFolder = DefaultDestinationFolder;
        ExportData = [];
        _selectedFitFilePaths = [];
    }

    public void AddFitFilePaths(IEnumerable<string> fitFilePaths)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNullOrEmpty(fitFilePaths);

        SelectedFitFilePaths = [.. fitFilePaths.Distinct()];
    }

    public string DestinationFolder
    {
        get => _destinationFolder;
        set => _ = TrySetValue(value, _folderPathValidator, ref _destinationFolder, _setValueOptions);
    }

    public HashSet<string> SelectedFitFilePaths
    {
        get => _selectedFitFilePaths;
        private set
        {
            if (TrySetValue(value, _fitFilePathsValidator, ref _selectedFitFilePaths, _setValueOptions))
            {
                ExportData.Clear();
                foreach (string fitFilePath in SelectedFitFilePaths)
                {
                    var exportData = new ExportData(_filePathsValidator, _setValueOptions)
                    {
                        FitFilePath = fitFilePath
                    };

                    ExportData.Add(exportData);
                }
            }
        }
    }

    public ObservableCollection<ExportData> ExportData { get; private set; }

    private static PropertyValidationDelegate<HashSet<string>> IsFitFilePathValid() => fitFilePaths => new PropertyValidationResult(fitFilePaths != null
                                                                                                             && fitFilePaths.Count > 0
                                                                                                             && fitFilePaths.All(filePath => File.Exists(filePath) && Path.GetExtension(filePath).Equals(FitFileExtension, StringComparison.OrdinalIgnoreCase)),
                                                                                                             ["At least one fit file must be selected."]);

    private static PropertyValidationDelegate<ObservableCollection<string>> IsFilePathValid() => fitFilePaths => new PropertyValidationResult(fitFilePaths != null
                                                                                                             && fitFilePaths.Count > 0
                                                                                                             && fitFilePaths.All(filePath => File.Exists(filePath)),
                                                                                                             ["At least one fit file must be selected."]);

    private static PropertyValidationDelegate<string> IsFolderPathValid() => folderPath => new PropertyValidationResult(!string.IsNullOrWhiteSpace(folderPath) && Directory.Exists(folderPath),
                                                                                                             ["Destination folder cannot be empty or must exist."]);

    public event PropertyChangedEventHandler? PropertyChanged;
}

public class ExportData : ViewModel
{
    private readonly PropertyValidationDelegate<ObservableCollection<string>> _filePathsValidator;
    private readonly SetValueOptions _setValueOptions;
    private ObservableCollection<string> _selectedExtraFilePaths;

    public ExportData(PropertyValidationDelegate<ObservableCollection<string>> filePathsValidator, SetValueOptions setValueOptions)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(filePathsValidator);

        _filePathsValidator = filePathsValidator;
        _setValueOptions = setValueOptions;
    }

    public string FitFilePath { get; init; }

    public ObservableCollection<string> SelectedExtraFilePaths
    {
        get => _selectedExtraFilePaths;
        set => _ = TrySetValue(value, _filePathsValidator, ref _selectedExtraFilePaths, _setValueOptions);
    }
}