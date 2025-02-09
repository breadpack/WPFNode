using WPFNode.Core.Models;
using WPFNode.Plugin.SDK;

namespace WPFNode.Core.Commands;

public class CreateGroupCommand : ICommand
{
    private readonly NodeCanvas            _canvas;
    private readonly IEnumerable<NodeBase> _nodes;
    private readonly string                _name;
    private          NodeGroup?            _createdGroup;

    public string Description => "그룹 생성";

    public CreateGroupCommand(NodeCanvas canvas, IEnumerable<NodeBase> nodes, string name)
    {
        _canvas = canvas;
        _nodes = nodes.ToList();
        _name = name;
    }

    public void Execute()
    {
        _createdGroup = _canvas.CreateGroup(_nodes, _name);
    }

    public void Undo()
    {
        if (_createdGroup != null)
        {
            _canvas.DeleteGroup(_createdGroup);
            _createdGroup = null;
        }
    }
} 
