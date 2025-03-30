using WPFNode.Interfaces;

namespace WPFNode.Commands;

public class RemoveNodeCommand : ICommand
{
    private readonly INodeCanvas _canvas;
    private readonly INode _node;
    private readonly List<(string SourcePortName, string TargetPortName, bool IsSourceNode)> _connectionInfo;
    private readonly Type _nodeType;
    private readonly double _x;
    private readonly double _y;

    public string Description => "노드 삭제";

    public RemoveNodeCommand(INodeCanvas canvas, INode node)
    {
        _canvas = canvas;
        _node = node;
        _nodeType = node.GetType();
        _x = node.X;
        _y = node.Y;
        
        // 연결 정보를 포트 이름과 연결 방향으로 저장
        _connectionInfo = node.InputPorts.SelectMany(p => p.Connections
                            .Select(c => (c.Source.Name, p.Name, false)))
                            .Concat(node.OutputPorts.SelectMany(p => p.Connections
                            .Select(c => (p.Name, c.Target.Name, true))))
                            .ToList();
    }

    public void Execute()
    {
        _canvas.RemoveNode(_node);
    }

    public void Undo()
    {
        var restoredNode = _canvas.CreateNode(_nodeType, _x, _y);

        // 연결 복원
        foreach (var info in _connectionInfo)
        {
            if (info.IsSourceNode)
            {
                var sourcePort = restoredNode.OutputPorts.First(p => p.Name == info.SourcePortName);
                var targetPort = _canvas.Nodes
                    .SelectMany(n => n.InputPorts)
                    .First(p => p.Name == info.TargetPortName);
                _canvas.Connect(sourcePort, targetPort);
            }
            else
            {
                var sourcePort = _canvas.Nodes
                    .SelectMany(n => n.OutputPorts)
                    .First(p => p.Name == info.SourcePortName);
                var targetPort = restoredNode.InputPorts.First(p => p.Name == info.TargetPortName);
                _canvas.Connect(sourcePort, targetPort);
            }
        }
    }
}