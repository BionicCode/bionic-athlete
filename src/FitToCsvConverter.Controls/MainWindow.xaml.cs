namespace FitToCsvConverter.Controls;

using System.Windows;
using System.Windows.Input;
using BionicCode.Utilities.Net;
using FitToCsvConverter.ViewModel;
using Microsoft.Win32;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, IDisposableAdvanced
{
    private readonly MainViewModel _viewModel;
    private readonly OpenFolderDialog _openFolderDialog;
    private readonly List<CancellationTokenSource> _addFitFilesCancellationTokenSources;
    private readonly object _cancellationTokenSourceQueueSyncLock;
    private bool? _isFitFileDropAllowed;
    private bool? _isExtraFileDropAllowed;

    public bool IsDisposed { get; private set; }

    public static readonly RoutedCommand SelectAllActivityFieldsCommand = new(nameof(SelectAllActivityFieldsCommand), typeof(MainWindow));
    public static readonly RoutedCommand UnselectAllActivityFieldsCommand = new(nameof(UnselectAllActivityFieldsCommand), typeof(MainWindow));
    public static readonly RoutedCommand SelectAllRecordFieldsCommand = new(nameof(SelectAllRecordFieldsCommand), typeof(MainWindow));
    public static readonly RoutedCommand UnselectAllRecordFieldsCommand = new(nameof(UnselectAllRecordFieldsCommand), typeof(MainWindow));
    public static readonly RoutedCommand SelectAllSessionFieldsCommand = new(nameof(SelectAllSessionFieldsCommand), typeof(MainWindow));
    public static readonly RoutedCommand UnselectAllSessionFieldsCommand = new(nameof(UnselectAllSessionFieldsCommand), typeof(MainWindow));
    public static readonly RoutedCommand SelectAllLapFieldsCommand = new(nameof(SelectAllLapFieldsCommand), typeof(MainWindow));
    public static readonly RoutedCommand UnselectAllLapFieldsCommand = new(nameof(UnselectAllLapFieldsCommand), typeof(MainWindow));
    public static readonly RoutedCommand RemoveAllExtraFilesCommand = new(nameof(RemoveAllExtraFilesCommand), typeof(MainWindow));
    public static readonly RoutedCommand RemoveExtraFileCommand = new(nameof(RemoveExtraFileCommand), typeof(MainWindow));
    public static readonly RoutedCommand SelectAllExtraFilesForRenameCommand = new(nameof(SelectAllExtraFilesForRenameCommand), typeof(MainWindow));
    public static readonly RoutedCommand UnselectAllExtraFilesForRenameCommand = new(nameof(UnselectAllExtraFilesForRenameCommand), typeof(MainWindow));
    public static readonly RoutedCommand AddFitFileCommand = new(nameof(AddFitFileCommand), typeof(MainWindow));
    public static readonly RoutedCommand RemoveFitFileCommand = new(nameof(RemoveFitFileCommand), typeof(MainWindow));

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = _viewModel;
        _openFolderDialog = new OpenFolderDialog
        {
            Title = "Select Destination Folder",
            Multiselect = false,
            AddToRecent = true
        };
        _addFitFilesCancellationTokenSources = [];
        _cancellationTokenSourceQueueSyncLock = new object();

        var selectAllActivitiesCommandBinding = new CommandBinding(
            SelectAllActivityFieldsCommand,
            executed: (s, e) => _viewModel.SetAllActivityFieldsSelected(true),
            canExecute: (s, e) => e.CanExecute = _viewModel.SelectedExportData?.ActivityFields.Any(field => !field.IsSelected) ?? false);
        _ = CommandBindings.Add(selectAllActivitiesCommandBinding);

        var unselectAllActivitiesCommandBinding = new CommandBinding(
            UnselectAllActivityFieldsCommand,
            executed: (s, e) => _viewModel.SetAllActivityFieldsSelected(false),
            canExecute: (s, e) => e.CanExecute = _viewModel.SelectedExportData?.ActivityFields.Any(field => field.IsSelected) ?? false);
        _ = CommandBindings.Add(unselectAllActivitiesCommandBinding);

        var selectAllRecordsCommandBinding = new CommandBinding(
            SelectAllRecordFieldsCommand,
            executed: (s, e) => _viewModel.SetAllRecordFieldsSelected(true),
            canExecute: (s, e) => e.CanExecute = _viewModel.SelectedExportData?.RecordFields.Any(field => !field.IsSelected) ?? false);
        _ = CommandBindings.Add(selectAllRecordsCommandBinding);

        var unselectAllRecordsCommandBinding = new CommandBinding(
            UnselectAllRecordFieldsCommand,
            executed: (s, e) => _viewModel.SetAllRecordFieldsSelected(false),
            canExecute: (s, e) => e.CanExecute = _viewModel.SelectedExportData?.RecordFields.Any(field => field.IsSelected) ?? false);
        _ = CommandBindings.Add(unselectAllRecordsCommandBinding);

        var selectAllSessionsCommandBinding = new CommandBinding(
            SelectAllSessionFieldsCommand,
            executed: (s, e) => _viewModel.SetAllSessionFieldsSelected(true),
            canExecute: (s, e) => e.CanExecute = _viewModel.SelectedExportData?.SessionFields.Any(field => !field.IsSelected) ?? false);
        _ = CommandBindings.Add(selectAllSessionsCommandBinding);

        var unselectAllSessionsCommandBinding = new CommandBinding(
            UnselectAllSessionFieldsCommand,
            executed: (s, e) => _viewModel.SetAllSessionFieldsSelected(false),
            canExecute: (s, e) => e.CanExecute = _viewModel.SelectedExportData?.SessionFields.Any(field => field.IsSelected) ?? false);
        _ = CommandBindings.Add(unselectAllSessionsCommandBinding);

        var selectAllLapsCommandBinding = new CommandBinding(
            SelectAllLapFieldsCommand,
            executed: (s, e) => _viewModel.SetAllLapFieldsSelected(true),
            canExecute: (s, e) => e.CanExecute = _viewModel.SelectedExportData?.LapFields.Any(field => !field.IsSelected) ?? false);
        _ = CommandBindings.Add(selectAllLapsCommandBinding);

        var unselectAllLapsCommandBinding = new CommandBinding(
            UnselectAllLapFieldsCommand,
            executed: (s, e) => _viewModel.SetAllLapFieldsSelected(false),
            canExecute: (s, e) => e.CanExecute = _viewModel.SelectedExportData?.LapFields.Any(field => field.IsSelected) ?? false);
        _ = CommandBindings.Add(unselectAllLapsCommandBinding);

        var removeAllExtraFilesCommandBinding = new CommandBinding(
            RemoveAllExtraFilesCommand,
            executed: (s, e) => _viewModel.SelectedExportData?.ClearExtraFilePaths(),
            canExecute: (s, e) => e.CanExecute = _viewModel.SelectedExportData?.SelectedExtraFilePaths?.Any() ?? false);
        _ = CommandBindings.Add(removeAllExtraFilesCommandBinding);

        var removeExtraFileCommandBinding = new CommandBinding(
            RemoveExtraFileCommand,
            executed: (s, e) =>
            {
                if (e.Parameter is ObservableFileDescriptor fileDescriptor)
                {
                    _viewModel.SelectedExportData?.RemoveExtraFilePath(fileDescriptor);
                }
            },
            canExecute: (s, e) => e.CanExecute = e.Parameter is ObservableFileDescriptor fileDescriptor && (_viewModel.SelectedExportData?.SelectedExtraFilePaths?.Contains(fileDescriptor) ?? false));
        _ = CommandBindings.Add(removeExtraFileCommandBinding);

        var selectAllExtraFilesForRenameCommandBinding = new CommandBinding(
            SelectAllExtraFilesForRenameCommand,
            executed: (s, e) => _viewModel.SelectedExportData?.SetRenameAllExtraFiles(true),
            canExecute: (s, e) => e.CanExecute = _viewModel.SelectedExportData?.SelectedExtraFilePaths?.Any(file => !file.IsRenamingEnabled) ?? false);
        _ = CommandBindings.Add(selectAllExtraFilesForRenameCommandBinding);

        var unselectAllExtraFilesForRenameCommandBinding = new CommandBinding(
            UnselectAllExtraFilesForRenameCommand,
            executed: (s, e) => _viewModel.SelectedExportData?.SetRenameAllExtraFiles(false),
            canExecute: (s, e) => e.CanExecute = _viewModel.SelectedExportData?.SelectedExtraFilePaths?.Any(file => file.IsRenamingEnabled) ?? false);
        _ = CommandBindings.Add(unselectAllExtraFilesForRenameCommandBinding);

        var removeFitFileCommandBinding = new CommandBinding(
            RemoveFitFileCommand,
            executed: (s, e) =>
            {
                if (e.Parameter is string filePath)
                {
                    _viewModel.RemoveFitFilePath(filePath);
                }
            },
            canExecute: (s, e) => e.CanExecute = e.Parameter is string filePath && (_viewModel.FitFilePaths?.Contains(filePath) ?? false));
        _ = CommandBindings.Add(removeFitFileCommandBinding);

        var addFitFileCommandBinding = new CommandBinding(
            AddFitFileCommand,
            executed: async (s, e) => await OnExecutedAddFitFileCommandAsync(s, e),
            canExecute: (s, e) => e.CanExecute = true);
        _ = CommandBindings.Add(addFitFileCommandBinding);
    }

    private async Task OnExecutedAddFitFileCommandAsync(object sender, ExecutedRoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "Select FIT Files",
            Filter = "FIT Files|*.fit|ZIP Files|*.zip|All Files|*.fit;*.zip",
            Multiselect = true,
            AddToRecent = true
        };
        bool? result = openFileDialog.ShowDialog();
        if (result == true)
        {
            await AddFitFilePathsAsync(openFileDialog.FileNames);
        }
    }

    private async void OnFitFilesDropped(object sender, DragEventArgs e)
    {
        string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop, false) ?? [];
        if (_isFitFileDropAllowed.GetValueOrDefault() && filePaths.Length > 0)
        {
            await AddFitFilePathsAsync(filePaths);
        }
    }

    private async Task AddFitFilePathsAsync(string[] filePaths)
    {
        CancellationTokenSource? cancellationTokenSource = null;
        try
        {
            lock (_cancellationTokenSourceQueueSyncLock)
            {
                // Add to the end and remove expired from the front.
                // The uncancelled global token source is always at the end of the list.
                // Therefore, we must always expose the last token source in the list to the view model for cancellation
                // and only add a new token source when the last token source is already cancelled.
                cancellationTokenSource = _addFitFilesCancellationTokenSources.LastOrDefault();
                if (cancellationTokenSource is null
                    || cancellationTokenSource.IsCancellationRequested)
                {
                    cancellationTokenSource = new CancellationTokenSource()!;
                    _addFitFilesCancellationTokenSources.Add(cancellationTokenSource);
                }
            }

            await _viewModel.AddFitFilePathsAsync(filePaths, cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            lock (_cancellationTokenSourceQueueSyncLock)
            {
                if (cancellationTokenSource is not null
                    && _addFitFilesCancellationTokenSources.Remove(cancellationTokenSource
                    ))
                {
                    cancellationTokenSource.Dispose();
                }
            }
        }
    }

    private void SelectDestinationFolderButton_Click(object sender, RoutedEventArgs e)
    {
        bool? result = _openFolderDialog.ShowDialog();
        if (result == true)
        {
            _viewModel.DestinationFolder = _openFolderDialog.FolderName;
        }
    }

    private void OnExtraZipFileContentDropped(object sender, DragEventArgs e)
    {
        ExportData? exportData = _viewModel.SelectedExportData;
        string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop, false) ?? [];
        _viewModel.AddExtraFilePaths(exportData!, filePaths);
    }

    private void ProvideFitFileDropTargetFeedBack(object sender, DragEventArgs e)
    {
        if (_isFitFileDropAllowed == false)
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }
    }

    private void ClearFitFileDropTargetFeedBack(object sender, DragEventArgs e) => _isFitFileDropAllowed = null;

    private void PrepareFitFileDropTargetFeedBack(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return;
        }

        string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop, false) ?? [];

        // Only disallow when all files are invalid.
        // Otherwise, allow the drop and let the view model handle the validation and error reporting for each file.
        _isFitFileDropAllowed = filePaths.Any(path => _viewModel.IsFitFilePathValid(path).IsValid);
    }

    private void ProvideExtraFileDropTargetFeedBack(object sender, DragEventArgs e)
    {
        if (_isExtraFileDropAllowed == false)
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }
    }

    private void ClearExtraFileDropTargetFeedBack(object sender, DragEventArgs e) => _isExtraFileDropAllowed = null;

    private void PrepareExtraFileDropTargetFeedBack(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return;
        }

        string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop, false) ?? [];

        // Only disallow when all files are invalid.
        // Otherwise, allow the drop and let the view model handle the validation and error reporting for each file.
        _isExtraFileDropAllowed = _viewModel.ExportData.Any() && filePaths.Any(path => _viewModel.IsFilePathValid(path).IsValid);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!IsDisposed)
        {
            if (disposing)
            {
                lock (_cancellationTokenSourceQueueSyncLock)
                {
                    _addFitFilesCancellationTokenSources.ForEach(cancellationTokenSource =>
                            {
                                cancellationTokenSource.Cancel();
                                cancellationTokenSource.Dispose();
                            });
                    _addFitFilesCancellationTokenSources.Clear();
                }
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
