using WPFNode.Core.Models;

namespace WPFNode.Core.Commands;

public class AddGroupCommand : ICommand
{
    private readonly NodeCanvas _canvas;
    private readonly NodeGroup _group;
    private readonly IEnumerable<NodeBase> _nodes;

    public string Description => "그룹 생성";

    public AddGroupCommand(NodeCanvas canvas, string name, IEnumerable<NodeBase> nodes)
    {
        _canvas = canvas;
        _nodes = nodes;
        _group = new NodeGroup(Guid.NewGuid(), name);
    }

    public void Execute()
    {
        foreach (var node in _nodes)
        {
            _group.Nodes.Add(node);
        }
        _canvas.AddGroup(_group);
    }

    public void Undo()
    {
        _canvas.RemoveGroup(_group);
    }
}

public class RemoveGroupCommand : ICommand
{
    private readonly NodeCanvas _canvas;
    private readonly NodeGroup _group;
    private readonly List<NodeBase> _nodes;

    public string Description => "그룹 삭제";

    public RemoveGroupCommand(NodeCanvas canvas, NodeGroup group)
    {
        _canvas = canvas;
        _group = group;
        _nodes = group.Nodes.ToList();
    }

    public void Execute()
    {
        _canvas.RemoveGroup(_group);
    }

    public void Undo()
    {
        foreach (var node in _nodes)
        {
            _group.Nodes.Add(node);
        }
        _canvas.AddGroup(_group);
    }
} 