using WPFNode.Core.Models;
using WPFNode.Plugin.SDK;

namespace WPFNode.Core.Commands;

public class AddNodeCommand : ICommand
{
    private readonly NodeCanvas _canvas;
    private readonly NodeBase   _node;

    public string Description => "노드 추가";

    public AddNodeCommand(NodeCanvas canvas, NodeBase node)
    {
        _canvas = canvas;
        _node   = node;
    }

    public void Execute()
    {
        _canvas.Nodes.Add(_node);
    }

    public void Undo()
    {
        _canvas.Nodes.Remove(_node);
    }
}