using WPFNode.Core.Models;

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
        _canvas.AddNode(_node);
    }

    public void Undo()
    {
        _canvas.RemoveNode(_node);
    }
}