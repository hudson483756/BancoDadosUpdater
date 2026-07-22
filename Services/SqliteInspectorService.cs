using System.Data;
using System.IO; // <- Adicionado aqui
using BancoDadosUpdater.Models;
using Microsoft.Data.Sqlite;

namespace BancoDadosUpdater.Services;

public class SqliteInspectorService
{
    public static DatabaseInfo InspectDatabase(string filePath, string label)
    {
        var dbInfo = new DatabaseInfo
        {
            FilePath = filePath,
            Label = label
        };

        if (!File.Exists(filePath))
            return dbInfo;

        string connectionString = $"Data Source={filePath}";
        using var conexao = new SqliteConnection(connectionString);
        conexao.Open();

        // 1. Busca todos os nomes das tabelas (ignorando tabelas do sistema SQLite)
        string queryTabelas = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';";
        using var cmdTabelas = new SqliteCommand(queryTabelas, conexao);
        using var readerTabelas = cmdTabelas.ExecuteReader();

        List<string> nomesTabelas = new();
        while (readerTabelas.Read())
        {
            nomesTabelas.Add(readerTabelas.GetString(0));
        }

        // 2. Para cada tabela, busca suas colunas via PRAGMA
        foreach (var tabela in nomesTabelas)
        {
            var tableSchema = new TableSchema { TableName = tabela };
            string queryColunas = $"PRAGMA table_info('{tabela}');";

            using var cmdColunas = new SqliteCommand(queryColunas, conexao);
            using var readerColunas = cmdColunas.ExecuteReader();

            while (readerColunas.Read())
            {
                tableSchema.Columns.Add(new ColumnModel
                {
                    Id = readerColunas.GetInt32(0),
                    Name = readerColunas.GetString(1),
                    Type = readerColunas.GetString(2),
                    IsNotNull = readerColunas.GetInt32(3) == 1,
                    DefaultValue = readerColunas.IsDBNull(4) ? "NULL" : readerColunas.GetString(4)
                });
            }

            dbInfo.Tables.Add(tableSchema);
        }

        return dbInfo;
    }
}