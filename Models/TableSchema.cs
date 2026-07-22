namespace BancoDadosUpdater.Models;

public class ColumnModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsNotNull { get; set; }
    public string DefaultValue { get; set; } = string.Empty;
}

public class TableSchema
{
    public string TableName { get; set; } = string.Empty;
    public List<ColumnModel> Columns { get; set; } = new();
}