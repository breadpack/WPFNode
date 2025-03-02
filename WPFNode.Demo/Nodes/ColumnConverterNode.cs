using System.Text.Json.Serialization;
using WPFNode.Attributes;
using WPFNode.Demo.Models;
using WPFNode.Interfaces;
using WPFNode.Models;

namespace WPFNode.Demo.Nodes;

[NodeCategory("Table")]
public class ColumnConverterNode : NodeBase
{
    private string _columnName = "";
    private string _targetType = "";

    [JsonConstructor]
    public ColumnConverterNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
    {
        Name = "Column Converter";
        Description = "테이블의 특정 컬럼 타입을 변환하는 노드입니다.";

        InputPort = CreateInputPort<TableData>("Input");
        OutputPort = CreateOutputPort<TableData>("Output");
    }

    public OutputPort<TableData> OutputPort { get; set; }

    public InputPort<TableData> InputPort { get; set; }

    public string ColumnName
    {
        get => _columnName;
        set
        {
            _columnName = value;
            UpdateName();
            ProcessAsync().Wait();
        }
    }

    public string TargetType
    {
        get => _targetType;
        set
        {
            _targetType = value;
            UpdateName();
            ProcessAsync().Wait();
        }
    }

    private void UpdateName()
    {
        if (!string.IsNullOrEmpty(_columnName) && !string.IsNullOrEmpty(_targetType))
        {
            Name = $"Convert {_columnName} to {_targetType}";
        }
    }

    protected override async Task ProcessAsync(CancellationToken cancellationToken = default) {
        var inputTable = InputPort.GetValueOrDefault();
        if (inputTable != null)
        {
            var outputTable = inputTable.Clone();
            var columnIndex = outputTable.Headers.IndexOf(_columnName);
            
            if (columnIndex >= 0 && columnIndex < outputTable.Columns.Count)
            {
                outputTable.Columns[columnIndex].Type = _targetType;
                // TODO: 여기서 실제 데이터 변환 로직을 구현해야 합니다.
            }

            OutputPort.Value = outputTable;
        }
        await Task.CompletedTask;
    }

    public override async Task SetParameterAsync(object parameter)
    {
        if (parameter is (string columnName, string targetType))
        {
            ColumnName = columnName;
            TargetType = targetType;
        }
        await base.SetParameterAsync(parameter);
    }
} 