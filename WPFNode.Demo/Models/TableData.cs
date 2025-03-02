namespace WPFNode.Demo.Models;

public class TableData
{
    public string TableName { get; set; } = "";
    public List<string> Headers { get; set; } = new();
    public List<ColumnDefinition> Columns { get; set; } = new();
    public List<RowData> Rows { get; set; } = new();

    public TableData Clone()
    {
        return new TableData
        {
            TableName = this.TableName,
            Headers = new List<string>(this.Headers),
            Columns = new List<ColumnDefinition>(this.Columns),
            Rows = new List<RowData>(this.Rows)
        };
    }

    public TableData CloneStructure()
    {
        return new TableData
        {
            TableName = this.TableName,
            Headers = new List<string>(this.Headers),
            Columns = new List<ColumnDefinition>(),
            Rows = new List<RowData>()
        };
    }
}

public class ColumnDefinition
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public bool IsNullable { get; set; }
}

public class RowData
{
    public List<string> Values { get; set; } = new();
} 