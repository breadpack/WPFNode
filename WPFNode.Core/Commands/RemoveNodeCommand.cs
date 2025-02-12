using WPFNode.Abstractions;
using WPFNode.Core.Models;

namespace WPFNode.Core.Commands;

public class RemoveNodeCommand : ICommand
{
    private readonly NodeCanvas       _canvas;
    private readonly NodeBase         _node;
    private readonly List<IConnection> _connections;

    public string Description => "노드 삭제";

    public RemoveNodeCommand(NodeCanvas canvas, NodeBase node)
    {
        _canvas = canvas;
        _node   = node;
        _connections = node.InputPorts.SelectMany(p => p.Connections)
                           .Concat(node.OutputPorts.SelectMany(p => p.Connections))
                           .Distinct()
                           .ToList();
    }

    public void Execute()
    {
        foreach (var connection in _connections)
        {
            _canvas.Disconnect(connection);
        }
        _canvas.RemoveNode(_node);
    }

    public void Undo()
    {
        _canvas.AddNode(_node);
        foreach (var connection in _connections)
        {
            _canvas.Connect(connection.Source, connection.Target);
        }
    }
}