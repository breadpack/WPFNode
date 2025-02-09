using WPFNode.Core.Models;
using WPFNode.Plugin.SDK;

namespace WPFNode.Core.Commands;

public class DeleteGroupCommand : ICommand
{
    private readonly NodeCanvas     _canvas;
    private readonly NodeGroup      _group;
    private readonly List<NodeBase> _nodes;

    public string Description => "그룹 삭제";

    public DeleteGroupCommand(NodeCanvas canvas, NodeGroup group)
    {
        _canvas = canvas;
        _group = group;
        _nodes = group.Nodes.ToList();
    }

    public void Execute()
    {
        _canvas.DeleteGroup(_group);
    }

    public void Undo()
    {
        var group = _canvas.CreateGroup(_nodes, _group.Name);
        group.X = _group.X;
        group.Y = _group.Y;
    }
} 
