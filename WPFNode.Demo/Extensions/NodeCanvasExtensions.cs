using WPFNode.Demo.Models;
using WPFNode.Models;

namespace WPFNode.Demo.Extensions;

public static class NodeCanvasExtensions
{
    public static async Task SetTableDataAsync(this NodeCanvas canvas, TableData tableData, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(tableData);

        // TableInputNode 찾기
        var tableInputNode = canvas.Nodes
            .FirstOrDefault(n => n.GetType().Name == "TableInputNode") as NodeBase;

        // 없으면 새로 생성
        if (tableInputNode == null)
        {
            var tableInputType = canvas.GetType().Assembly
                .GetTypes()
                .FirstOrDefault(t => t.Name == "TableInputNode");

            if (tableInputType == null)
                throw new InvalidOperationException("TableInputNode 타입을 찾을 수 없습니다.");

            tableInputNode = (NodeBase)canvas.CreateNode(tableInputType);
        }

        // TableData 설정
        await tableInputNode.SetParameterAsync(tableData);

        // 실행
        await canvas.ExecuteAsync(cancellationToken);
    }
} 