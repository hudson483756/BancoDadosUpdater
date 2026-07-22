using System.Windows;
using System.Windows.Controls;
using BancoDadosUpdater.Models;
using BancoDadosUpdater.Services;

namespace BancoDadosUpdater.Views;

public partial class DataViewerWindow : Window
{
    private readonly string _dbPath;

    public DataViewerWindow(DatabaseInfo dbInfo)
    {
        InitializeComponent();
        _dbPath = dbInfo.FilePath;
        Title = $"Visualizando: {dbInfo.Label}";

        // Preenche o ComboBox com as tabelas do banco
        CmbTabelas.ItemsSource = dbInfo.Tables;

        // Seleciona a primeira tabela por padrão (se houver)
        if (dbInfo.Tables.Count > 0)
            CmbTabelas.SelectedIndex = 0;
    }

    private void CmbTabelas_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbTabelas.SelectedItem is TableSchema tabela)
        {
            // Carrega os dados e joga direto para o DataGrid
            var dataTable = DataViewerService.ObterDadosTabela(_dbPath, tabela.TableName);
            GridDados.ItemsSource = dataTable.DefaultView;
        }
    }
}