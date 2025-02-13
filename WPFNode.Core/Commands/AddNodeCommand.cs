using WPFNode.Abstractions;
using WPFNode.Core.Models;

namespace WPFNode.Core.Commands;

public class AddNodeCommand : ICommand
{
    private readonly NodeCanvas _canvas;
    private readonly Type _nodeType;
    private readonly double _x;
    private readonly double _y;
    private INode? _createdNode;

    public string Description => "노드 추가";

    public AddNodeCommand(NodeCanvas canvas, Type nodeType, double x = 0, double y = 0)
    {
        _canvas = canvas;
        _nodeType = nodeType;
        _x = x;
        _y = y;
    }

    public void Execute()
    {
        _createdNode = (INode)_canvas.CreateNode(_nodeType, _x, _y);
    }

    public void Undo()
    {
        if (_createdNode != null)
        {
            _canvas.RemoveNode(_createdNode);
        }
    }
}