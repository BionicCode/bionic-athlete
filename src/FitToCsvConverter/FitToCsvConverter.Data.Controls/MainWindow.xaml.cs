namespace FitToCsvConverter.Controls;

using System.Windows;
using System.Windows.Controls;
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
        var listBox = sender as ListBox;
        var exportData = listBox!.DataContext as ExportData;
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