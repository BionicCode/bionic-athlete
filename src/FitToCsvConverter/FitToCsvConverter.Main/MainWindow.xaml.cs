namespace FitToCsvConverter.Main;

using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
            _viewModel.SelectedFilePaths = filePath;
        }
    }
}