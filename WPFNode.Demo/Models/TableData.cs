using System.Text.Json.Serialization;

namespace WPFNode.Demo.Models;

public class TableData
{
    public string                        TableName { get; set; } = "";
    public List<ColumnDefinition>        Columns   { get; set; } = new();
    public List<RowData>                 Rows      { get; set; } = new();

    public TableData Clone()
    {
        return new TableData
        {
            TableName = this.TableName,
            Columns = new List<ColumnDefinition>(this.Columns),
            Rows = new List<RowData>(this.Rows)
        };
    }

    public TableData CloneStructure()
    {
        return new TableData
        {
            TableName = this.TableName,
            Columns = new List<ColumnDefinition>(),
            Rows = new List<RowData>()
        };
    }
}

public class ColumnDefinition
{
    public string Name { get; set; } = "";
    public string TypeName { get; set; } = "";
    public bool IsNullable { get; set; }
    
    [JsonIgnore]
    public Type Type => Type.GetType(TypeName)!;
}

public class RowData
{
    public List<string> Values { get; set; } = new();
} 