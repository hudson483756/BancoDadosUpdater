using System.IO;
using System.Windows;
using System.Windows.Input;
using BancoDadosUpdater.Models;
using BancoDadosUpdater.Services;
using Microsoft.Win32;

namespace BancoDadosUpdater.ViewModels;

public class MainViewModel : ViewModelBase
{
    private DatabaseInfo _dbDrive = new();
    private DatabaseInfo _dbWindows = new();
    private DatabaseInfo? _dbCorrigido;
    private string _statusMessage = "Selecione as duas bases de dados para iniciar o processo.";
    private bool _isProcessing;

    public DatabaseInfo DbDrive
    {
        get => _dbDrive;
        set => SetProperty(ref _dbDrive, value);
    }

    public DatabaseInfo DbWindows
    {
        get => _dbWindows;
        set => SetProperty(ref _dbWindows, value);
    }

    public DatabaseInfo? DbCorrigido
    {
        get => _dbCorrigido;
        set => SetProperty(ref _dbCorrigido, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsProcessing
    {
        get => _isProcessing;
        set => SetProperty(ref _isProcessing, value);
    }

    public ICommand SelecionarDbDriveCommand { get; }
    public ICommand SelecionarDbWindowsCommand { get; }
    public ICommand ProcessarEAtualizarCommand { get; }

    public MainViewModel()
    {
        SelecionarDbDriveCommand = new RelayCommand(_ => SelecionarArquivo(isDrive: true));
        SelecionarDbWindowsCommand = new RelayCommand(_ => SelecionarArquivo(isDrive: false));
        ProcessarEAtualizarCommand = new RelayCommand(_ => ExecutarAtualizacao(), _ => PodeExecutar());
    }

    private void SelecionarArquivo(bool isDrive)
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Banco SQLite (*.db)|*.db|Todos os arquivos (*.*)|*.*",
            Title = isDrive
                ? "Selecione o memorias.db do Drive (Dados Reais)"
                : "Selecione o memorias.db do Windows (Estrutura Atualizada)"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            string caminho = openFileDialog.FileName;

            if (isDrive)
            {
                DbDrive = SqliteInspectorService.InspectDatabase(caminho, "memorias.db (Drive)");
            }
            else
            {
                DbWindows = SqliteInspectorService.InspectDatabase(caminho, "memorias.db (Windows)");
            }

            AtualizarStatus();
        }
    }

    private bool PodeExecutar()
    {
        return DbDrive.IsLoaded && DbWindows.IsLoaded && !IsProcessing;
    }

    private void AtualizarStatus()
    {
        if (!DbDrive.IsLoaded && !DbWindows.IsLoaded)
            StatusMessage = "Aguardando seleção das duas bases de dados.";
        else if (!DbDrive.IsLoaded)
            StatusMessage = "Falta selecionar o banco do Drive (Dados).";
        else if (!DbWindows.IsLoaded)
            StatusMessage = "Falta selecionar o banco do Windows (Modelo).";
        else
            StatusMessage = "⚡ Ambas as bases foram carregadas! Pronto para processar e atualizar.";
    }

    private void ExecutarAtualizacao()
    {
        IsProcessing = true;
        StatusMessage = "Processando e criando o banco corrigido...";

        try
        {
            var resultado = DatabaseMergerService.ProcessarEAtualizarBanco(DbDrive, DbWindows);

            if (resultado.Success)
            {
                DbCorrigido = SqliteInspectorService.InspectDatabase(resultado.OutputFilePath, "memorias.db (Corrigido)");

                int qtdTabelasNovas = resultado.AddedTables.Count;
                int qtdColunasNovas = resultado.AddedColumns.Count;

                StatusMessage = $"✨ Sucesso! Banco gerado com +{qtdTabelasNovas} tabela(s) e +{qtdColunasNovas} coluna(s) adicionada(s).";

                MessageBox.Show(
                    $"O banco de dados foi atualizado com sucesso sem perder registros!\n\n" +
                    $"Salvo em:\n{resultado.OutputFilePath}\n\n" +
                    $"• Tabelas criadas: {qtdTabelasNovas}\n" +
                    $"• Colunas adicionadas: {qtdColunasNovas}",
                    "Atualização Concluída", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                StatusMessage = "❌ Erro ao atualizar o banco de dados.";
                MessageBox.Show($"Falha no processo: {resultado.ErrorMessage}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = "❌ Erro inesperado ao atualizar.";
            MessageBox.Show($"Erro inesperado: {ex.Message}", "Erro Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsProcessing = false;
        }
    }
}