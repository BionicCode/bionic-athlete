namespace FitToCsvConverter.Main;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FitToCsvConverter.ViewModel;
using Microsoft.Win32;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly OpenFolderDialog _openFolderDialog;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainViewModel();
        DataContext = _viewModel;
        _openFolderDialog = new OpenFolderDialog
        {
            Title = "Select Destination Folder",
            InitialDirectory = MainViewModel.DefaultDestinationFolder,
            Multiselect = false,
            AddToRecent = true
        };
    }

    private void OnFitFilesDropped(object sender, DragEventArgs e)
    {
        string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop, false) ?? [];
        if (filePaths.Length > 0)
        {
            _viewModel.AddFitFilePaths(filePaths);
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

    private void ClearFitFileDropTargetFeedBack(object sender, DragEventArgs e)
    {
        _isFitFileDropAllowed = null;
    }

    private void PrepareFitFileDropTargetFeedBack(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return;
        }

        string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop, false) ?? [];

        // Only disallow when all files are invalid.
        // Otherwise, allow the drop and let the view model handle the validation and error reporting for each file.
        _isFitFileDropAllowed = filePaths.Any(path => MainViewModel.IsFitFilePathValid(path).IsValid);
    }

    private void ProvideFitFileDropTargetFeedBack(object sender, GiveFeedbackEventArgs e)
    {
        //if (_isFitFileDropAllowed == false)
        //{
        //    Cursor = Cursors.No;
        //}
    }
}