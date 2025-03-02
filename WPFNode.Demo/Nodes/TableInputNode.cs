using System.Text.Json.Serialization;
using WPFNode.Attributes;
using WPFNode.Demo.Models;
using WPFNode.Interfaces;
using WPFNode.Models;

namespace WPFNode.Demo.Nodes;

[NodeCategory("Table")]
public class TableInputNode : NodeBase
{
    private TableData? _tableData;

    [JsonConstructor]
    public TableInputNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
    {
        Name = "Table Input";
        Description = "테이블 데이터를 입력받는 노드입니다.";

        // 출력 포트 생성
        CreateOutputPort("Table", typeof(TableData));
    }

    public TableData? TableData
    {
        get => _tableData;
        set
        {
            _tableData = value;
            if (_tableData != null)
            {
                Name = $"Table Input ({_tableData.TableName})";
            }
            ProcessAsync().Wait();
        }
    }

    protected override async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        if (_tableData != null)
        {
            OutputPorts[0].Value = _tableData;
        }
        await Task.CompletedTask;
    }

    public override async Task SetParameterAsync(object parameter)
    {
        if (parameter is TableData tableData)
        {
            TableData = tableData;
        }
        await base.SetParameterAsync(parameter);
    }
} 