using System.IO; // <- Adicionado aqui

namespace BancoDadosUpdater.Models;

public class DatabaseInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty; // Ex: "Drive (Dados)" ou "Windows (Modelo)"
    public List<TableSchema> Tables { get; set; } = new();
    public bool IsLoaded => !string.IsNullOrEmpty(FilePath) && File.Exists(FilePath);
}