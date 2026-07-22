namespace BancoDadosUpdater.Models;

public class MigrationResult
{
    public bool Success { get; set; }
    public string OutputFilePath { get; set; } = string.Empty;
    public List<string> AddedTables { get; set; } = new();
    public List<string> AddedColumns { get; set; } = new();
    public string ErrorMessage { get; set; } = string.Empty;
}