using System.Windows;
using BancoDadosUpdater.ViewModels;

namespace BancoDadosUpdater.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void BtnVisualizarDrive_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            if (vm.DbDrive != null && vm.DbDrive.IsLoaded)
            {
                var window = new DataViewerWindow(vm.DbDrive);
                window.Show();
            }
            else
            {
                MessageBox.Show("Por favor, selecione o banco do Drive primeiro.",
                                "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private void BtnVisualizarWindows_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            if (vm.DbWindows != null && vm.DbWindows.IsLoaded)
            {
                var window = new DataViewerWindow(vm.DbWindows);
                window.Show();
            }
            else
            {
                MessageBox.Show("Por favor, selecione o banco do Windows primeiro.",
                                "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}