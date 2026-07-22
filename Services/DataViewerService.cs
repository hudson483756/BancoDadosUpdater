using System.Data;
using Microsoft.Data.Sqlite;

namespace BancoDadosUpdater.Services;

public class DataViewerService
{
    public static DataTable ObterDadosTabela(string filePath, string tableName)
    {
        var dataTable = new DataTable();
        using var conexao = new SqliteConnection($"Data Source={filePath}");
        conexao.Open();

        string query = $"SELECT * FROM \"{tableName}\";";
        using var cmd = new SqliteCommand(query, conexao);
        using var reader = cmd.ExecuteReader();

        dataTable.Load(reader);
        return dataTable;
    }
}