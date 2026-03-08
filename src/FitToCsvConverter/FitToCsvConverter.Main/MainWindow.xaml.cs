namespace FitToCsvConverter.Main;

using System.Collections.ObjectModel;
using System.Windows;
using FitToCsvConverter.ViewModel;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainViewModel();
        DataContext = _viewModel;
    }

    private void Border_Drop(object sender, DragEventArgs e)
    {
        string[] filePath = (string[])e.Data.GetData(DataFormats.FileDrop, false) ?? [];
        if (filePath.Length > 0)
        {
            _viewModel.SelectedFitFilePaths = new ObservableCollection<string>(filePath);
        }
    }

    private void SelectDestinationFolderButton_Click(object sender, RoutedEventArgs e)
    {

    }
}