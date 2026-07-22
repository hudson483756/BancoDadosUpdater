using BancoDadosUpdater.Models;
using Microsoft.Data.Sqlite;
using System.IO;

namespace BancoDadosUpdater.Services;

public class DatabaseMergerService
{
    public static MigrationResult ProcessarEAtualizarBanco(DatabaseInfo dbDrive, DatabaseInfo dbWindows)
    {
        var resultado = new MigrationResult();

        try
        {
            // 1. Configura e garante a existência da pasta de destino em Documentos
            string pastaDocumentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string pastaDestino = Path.Combine(pastaDocumentos, "MemoriasAtelie", "BancoCorrigido");

            if (!Directory.Exists(pastaDestino))
            {
                Directory.CreateDirectory(pastaDestino);
            }

            string caminhoBancoCorrigido = Path.Combine(pastaDestino, "memorias.db");

            // 2. Faz a cópia do banco do Drive para a pasta BancoCorrigido (Substitui se já existir uma versão anterior)
            File.Copy(dbDrive.FilePath, caminhoBancoCorrigido, overwrite: true);
            resultado.OutputFilePath = caminhoBancoCorrigido;

            // 3. Conecta no banco copiado (Drive) para aplicar as melhorias/estruturas do modelo (Windows)
            string connectionStringCorrigido = $"Data Source={caminhoBancoCorrigido}";
            using var conexaoCorrigida = new SqliteConnection(connectionStringCorrigido);
            conexaoCorrigida.Open();

            // 4. Compara e adiciona novas TABELAS que existem no Windows mas faltam no Drive
            foreach (var tabelaModelo in dbWindows.Tables)
            {
                var tabelaDrive = dbDrive.Tables.FirstOrDefault(t => t.TableName.Equals(tabelaModelo.TableName, StringComparison.OrdinalIgnoreCase));

                if (tabelaDrive == null)
                {
                    // A tabela inteira não existe no Drive -> Recria a tabela no Drive usando a DDL do Windows
                    CriarNovaTabela(conexaoCorrigida, dbWindows.FilePath, tabelaModelo.TableName);
                    resultado.AddedTables.Add(tabelaModelo.TableName);
                }
                else
                {
                    // A tabela já existe -> Compara se há COLUNAS novas no modelo Windows que faltam no Drive
                    foreach (var colunaModelo in tabelaModelo.Columns)
                    {
                        bool colunaExisteNoDrive = tabelaDrive.Columns.Any(c => c.Name.Equals(colunaModelo.Name, StringComparison.OrdinalIgnoreCase));

                        if (!colunaExisteNoDrive)
                        {
                            AdicionarColuna(conexaoCorrigida, tabelaModelo.TableName, colunaModelo);
                            resultado.AddedColumns.Add($"{tabelaModelo.TableName}.{colunaModelo.Name}");
                        }
                    }
                }
            }

            resultado.Success = true;
        }
        catch (Exception ex)
        {
            resultado.Success = false;
            resultado.ErrorMessage = ex.Message;
        }

        return resultado;
    }

    private static void CriarNovaTabela(SqliteConnection conexaoDestino, string caminhoBancoOrigem, string nomeTabela)
    {
        // Obtém o DDL (CREATE TABLE ...) exato que foi usado no banco do Windows
        string connectionStringOrigem = $"Data Source={caminhoBancoOrigem}";
        using var conexaoOrigem = new SqliteConnection(connectionStringOrigem);
        conexaoOrigem.Open();

        string querySqlSchema = "SELECT sql FROM sqlite_master WHERE type='table' AND name=@nomeTabela;";
        using var cmdSchema = new SqliteCommand(querySqlSchema, conexaoOrigem);
        cmdSchema.Parameters.AddWithValue("@nomeTabela", nomeTabela);

        var sqlCreate = cmdSchema.ExecuteScalar()?.ToString();

        if (!string.IsNullOrEmpty(sqlCreate))
        {
            using var cmdCriar = new SqliteCommand(sqlCreate, conexaoDestino);
            cmdCriar.ExecuteNonQuery();
        }
    }

    private static void AdicionarColuna(SqliteConnection conexaoDestino, string nomeTabela, ColumnModel coluna)
    {
        string defaultValueClause = string.Empty;

        // SQLite proíbe CURRENT_TIMESTAMP em ALTER TABLE ADD COLUMN.
        // Usamos um valor constante fixo para colunas do tipo data.
        if (coluna.Name.Equals("UltimaAtualizacao", StringComparison.OrdinalIgnoreCase))
        {
            string dataAtualConstante = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            defaultValueClause = $"DEFAULT '{dataAtualConstante}'";
        }
        else if (coluna.Name.Equals("DispositivoOrigem", StringComparison.OrdinalIgnoreCase))
        {
            defaultValueClause = "DEFAULT 'Desconhecido'";
        }
        else if (!string.IsNullOrEmpty(coluna.DefaultValue) &&
                 !coluna.DefaultValue.Equals("NULL", StringComparison.OrdinalIgnoreCase) &&
                 !coluna.DefaultValue.Contains("CURRENT_TIMESTAMP", StringComparison.OrdinalIgnoreCase))
        {
            defaultValueClause = $"DEFAULT {coluna.DefaultValue}";
        }

        // Executa o ALTER TABLE com valor constante
        string queryAlter = $"ALTER TABLE \"{nomeTabela}\" ADD COLUMN \"{coluna.Name}\" {coluna.Type} {defaultValueClause};";

        using var cmdAlter = new SqliteCommand(queryAlter, conexaoDestino);
        cmdAlter.ExecuteNonQuery();
    }


}