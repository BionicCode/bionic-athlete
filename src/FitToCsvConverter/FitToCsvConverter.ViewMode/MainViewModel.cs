namespace FitToCsvConverter.ViewModel;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using BionicCode.Utilities.Net;

public class MainViewModel : ViewModel
{
    private const string DefaultDestinationFolder = @"C:\Temp\FitToCsvConverterOutput";
    private const string FitFileExtension = ".fit";
    private string _destinationFolder;
    private ObservableCollection<string> _selectedFitFilePaths;
    private ObservableCollection<string> _selectedExtraFilePaths;
    private readonly PropertyValidationDelegate<ObservableCollection<string>> _fitFilePathsValidator;
    private readonly PropertyValidationDelegate<ObservableCollection<string>> _filePathsValidator;
    private readonly SetValueOptions _setValueOptions = SetValueOptions.Default with { IsRejectInvalidValueEnabled = true, IsThrowExceptionOnValidationErrorEnabled = true, IsRejectEqualValuesEnabled = true };

    public MainViewModel()
    {
        _destinationFolder = DefaultDestinationFolder;
        _fitFilePathsValidator = IsFitFilePathValid();
        _filePathsValidator = IsFilePathValid();
    }

    public string DestinationFolder
    {
        get => _destinationFolder;
        set => _ = TrySetValue(value, value => (!string.IsNullOrWhiteSpace(value), ["Destination folder cannot be empty or whitespace."]), ref _destinationFolder);
    }

    public ObservableCollection<string> SelectedFitFilePaths
    {
        get => _selectedFitFilePaths;
        set => _ = TrySetValue(value, _fitFilePathsValidator, ref _selectedFitFilePaths, _setValueOptions);
    }

    public ObservableCollection<string> SelectedExtraFilePaths
    {
        get => _selectedExtraFilePaths;
        set => _ = TrySetValue(value, _filePathsValidator, ref _selectedExtraFilePaths, _setValueOptions);
    }

    private static PropertyValidationDelegate<ObservableCollection<string>> IsFitFilePathValid() => fitFilePaths => new PropertyValidationResult(fitFilePaths != null
                                                                                                             && fitFilePaths.Count > 0
                                                                                                             && fitFilePaths.All(filePath => File.Exists(filePath) && Path.GetExtension(filePath).Equals(FitFileExtension, StringComparison.OrdinalIgnoreCase)),
                                                                                                             ["At least one fit file must be selected."]);

    private static PropertyValidationDelegate<ObservableCollection<string>> IsFilePathValid() => fitFilePaths => new PropertyValidationResult(fitFilePaths != null
                                                                                                             && fitFilePaths.Count > 0
                                                                                                             && fitFilePaths.All(filePath => File.Exists(filePath)),
                                                                                                             ["At least one fit file must be selected."]);

    public event PropertyChangedEventHandler? PropertyChanged;
}