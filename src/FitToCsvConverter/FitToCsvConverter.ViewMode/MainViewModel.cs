namespace FitToCsvConverter.ViewModel;

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using BionicCode.Utilities.Net;
using BionicCode.Utilities.Net.Common.Collections.Generic;

public class MainViewModel : ViewModel
{
    public const string DefaultDestinationFolder = @"C:\Temp\FitToCsvConverterOutput";
    private const string FitFileExtension = ".fit";
    private string _destinationFolder;
    private HashSet<string> _selectedFitFilePaths;
    private ExportData _selectedExportData;
    private readonly PropertyValidationDelegate<HashSet<string>> _fitFilePathsValidator;
    private readonly PropertyValidationDelegate<ObservableCollection<string>> _filePathsValidator;
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
    public ExportData SelectedExportData
    {
        get => _selectedExportData;
        set => TrySetValue(value, ref _selectedExportData);
    }

    private static PropertyValidationDelegate<HashSet<string>> IsFitFilePathsValid() => fitFilePaths =>
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

    private static PropertyValidationDelegate<ObservableCollection<string>> IsFilePathsValid() => filePaths =>
        {
            ArgumentNullExceptionAdvanced.ThrowIfNull(filePaths);

            if (filePaths.Count == 0)
            {
                return new PropertyValidationResult(false, ["At least one file must be selected."]);
            }

            foreach (string filePath in filePaths)
            {
                PropertyValidationResult result = IsFilePathValid(filePath);
                if (!result.IsValid)
                {
                    return result;
                }
            }

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

    private static PropertyValidationDelegate<string> IsFolderPathValid() => folderPath => new PropertyValidationResult(!string.IsNullOrWhiteSpace(folderPath) && Directory.Exists(folderPath),
                                                                                                             ["Destination folder cannot be empty and must exist."]);
    public void AddExtraFilePaths(ExportData exportData, string[] filePaths)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(exportData);
        ArgumentNullExceptionAdvanced.ThrowIfNull(filePaths);

        if (filePaths.Length == 0)
        {
            return;
        }

        SelectedExportData = exportData;
        foreach (string filePath in filePaths)
        {
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public class ExportData : ViewModel
{
    private readonly PropertyValidationDelegate<string> _filePathsValidator;

    public ExportData(PropertyValidationDelegate<string> filePathsValidator)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(filePathsValidator);

        _filePathsValidator = filePathsValidator;
        SelectedExtraFilePaths = new ObservableHashSet<string>(StringComparer.OrdinalIgnoreCase);
        SelectedExtraFilePaths.CollectionChanged += ValidateOnItemAdded;
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

    public ObservableHashSet<string> SelectedExtraFilePaths { get; }
}