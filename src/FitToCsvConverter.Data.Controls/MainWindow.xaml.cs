namespace FitToCsvConverter.Controls;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BionicCode.Utilities.Net;
using FitToCsvConverter.ViewModel;
using Microsoft.Win32;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly OpenFolderDialog _openFolderDialog;

    public static readonly RoutedCommand SelectAllActivityFieldsCommand = new(nameof(SelectAllActivityFieldsCommand), typeof(MainWindow));
    public static readonly RoutedCommand UnselectAllActivityFieldsCommand = new(nameof(UnselectAllActivityFieldsCommand), typeof(MainWindow));
    public static readonly RoutedCommand SelectAllRecordFieldsCommand = new(nameof(SelectAllRecordFieldsCommand), typeof(MainWindow));
    public static readonly RoutedCommand UnselectAllRecordFieldsCommand = new(nameof(UnselectAllRecordFieldsCommand), typeof(MainWindow));
    public static readonly RoutedCommand SelectAllSessionFieldsCommand = new(nameof(SelectAllSessionFieldsCommand), typeof(MainWindow));
    public static readonly RoutedCommand UnselectAllSessionFieldsCommand = new(nameof(UnselectAllSessionFieldsCommand), typeof(MainWindow));
    public static readonly RoutedCommand SelectAllLapFieldsCommand = new(nameof(SelectAllLapFieldsCommand), typeof(MainWindow));
    public static readonly RoutedCommand UnselectAllLapFieldsCommand = new(nameof(UnselectAllLapFieldsCommand), typeof(MainWindow));

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

        var selectAllActivitiesBinding = new CommandBinding(
            SelectAllActivityFieldsCommand,
            executed: (s, e) => _viewModel.SetAllActivityFieldsSelected(true),
            canExecute: (s, e) => e.CanExecute = _viewModel.SelectedExportData?.ActivityFields.Any(field => !field.IsSelected) ?? false);
        var unselectAllActivitiesBinding = new CommandBinding(
            UnselectAllActivityFieldsCommand,
            executed: (s, e) => _viewModel.SetAllActivityFieldsSelected(false),
            canExecute: (s, e) => e.CanExecute = _viewModel.SelectedExportData?.ActivityFields.Any(field => field.IsSelected) ?? false);
        _ = CommandBindings.Add(selectAllActivitiesBinding);
        _ = CommandBindings.Add(unselectAllActivitiesBinding);

        var selectAllRecordsBinding = new CommandBinding(
            SelectAllRecordFieldsCommand,
            executed: (s, e) => _viewModel.SetAllRecordFieldsSelected(true),
            canExecute: (s, e) => e.CanExecute = _viewModel.SelectedExportData?.RecordFields.Any(field => !field.IsSelected) ?? false);
        var unselectAllRecordsBinding = new CommandBinding(
            UnselectAllRecordFieldsCommand,
            executed: (s, e) => _viewModel.SetAllRecordFieldsSelected(false),
            canExecute: (s, e) => e.CanExecute = _viewModel.SelectedExportData?.RecordFields.Any(field => field.IsSelected) ?? false);
        _ = CommandBindings.Add(selectAllRecordsBinding);
        _ = CommandBindings.Add(unselectAllRecordsBinding);

        var selectAllSessionsBinding = new CommandBinding(
            SelectAllSessionFieldsCommand,
            executed: (s, e) => _viewModel.SetAllSessionFieldsSelected(true),
            canExecute: (s, e) => e.CanExecute = _viewModel.SelectedExportData?.SessionFields.Any(field => !field.IsSelected) ?? false);
        var unselectAllSessionsBinding = new CommandBinding(
            UnselectAllSessionFieldsCommand,
            executed: (s, e) => _viewModel.SetAllSessionFieldsSelected(false),
            canExecute: (s, e) => e.CanExecute = _viewModel.SelectedExportData?.SessionFields.Any(field => field.IsSelected) ?? false);
        _ = CommandBindings.Add(selectAllSessionsBinding);
        _ = CommandBindings.Add(unselectAllSessionsBinding);

        var selectAllLapsBinding = new CommandBinding(
            SelectAllLapFieldsCommand,
            executed: (s, e) => _viewModel.SetAllLapFieldsSelected(true),
            canExecute: (s, e) => e.CanExecute = _viewModel.SelectedExportData?.LapFields.Any(field => !field.IsSelected) ?? false);
        var unselectAllLapsBinding = new CommandBinding(
            UnselectAllLapFieldsCommand,
            executed: (s, e) => _viewModel.SetAllLapFieldsSelected(false),
            canExecute: (s, e) => e.CanExecute = _viewModel.SelectedExportData?.LapFields.Any(field => field.IsSelected) ?? false);
        _ = CommandBindings.Add(selectAllLapsBinding);
        _ = CommandBindings.Add(unselectAllLapsBinding);
    }

    private async void OnFitFilesDropped(object sender, DragEventArgs e)
    {
        string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop, false) ?? [];
        if (_isFitFileDropAllowed.GetValueOrDefault() && filePaths.Length > 0)
        {
            _viewModel.ProgressChanged += OnAddFitFileProgressChanged;
            await _viewModel.AddFitFilePathsAsync(filePaths, CancellationToken.None);
        }
    }

    private Window? _progressDialog;
    private void OnAddFitFileProgressChanged(object sender, ProgressChangedEventArgs e)
    {
        _progressDialog ??= new Window()
        {
            Title = "Adding FIT Files",
            Content = new ProgressBar
            {
                IsIndeterminate = e.IsIndeterminate,
                Minimum = 0,
                Maximum = e.MaxValue,
                Width = 300,
                Height = 30
            },
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this
        };
        _ = (_progressDialog.Content as ProgressBar)!.SetBinding(ProgressBar.ValueProperty, new System.Windows.Data.Binding(nameof(MainViewModel.SelectedProgress)) { Source = _viewModel });
        _progressDialog.Show();
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

    private bool? _isFitFileDropAllowed;
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

    private void ProvideFitFileDropTargetFeedBack(object sender, GiveFeedbackEventArgs e)
    {
        //if (_isFitFileDropAllowed == false)
        //{
        //    Cursor = Cursors.No;
        //}
    }
}