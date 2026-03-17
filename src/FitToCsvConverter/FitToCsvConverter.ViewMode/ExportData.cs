namespace FitToCsvConverter.ViewModel;

using System.Collections.Specialized;
using BionicCode.Utilities.Net;

public class ExportData : ViewModel
{
    private readonly PropertyValidationDelegate<string> _filePathsValidator;
    private string _fileName;
    private bool _isRenamingEnabled;
    private string _fitFilePath;

    public ExportData(PropertyValidationDelegate<string> filePathsValidator)
    {
        ArgumentNullExceptionAdvanced.ThrowIfNull(filePathsValidator);

        _filePathsValidator = filePathsValidator;
        SelectedExtraFilePaths = [];
        SelectedExtraFilePaths.CollectionChanged += ValidateOnItemAdded;
        _fileName = string.Empty;
        ExportedFilePath = string.Empty;
        _fitFilePath = string.Empty;
        _isRenamingEnabled = true;
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

    public string FitFilePath
    {
        get => _fitFilePath;
        set => TrySetValue(value, ref _fitFilePath);
    }

    public bool IsRenamingEnabled
    {
        get => _isRenamingEnabled;
        set => TrySetValue(value, ref _isRenamingEnabled);
    }

    public string FileName
    {
        get => _fileName;
        set => TrySetValue(value, ref _fileName);
    }

    internal string ExportedFilePath { get; set; }
    public ObservableFileSystemPathHashSet SelectedExtraFilePaths { get; }
}