using WPFNode.Core.Models;
using WPFNode.Plugin.SDK;

namespace WPFNode.Core.Commands;

public class MoveNodesCommand : ICommand
{
    private readonly List<(NodeBase Node, double OldX, double OldY)> _nodePositions;
    private readonly double                                          _deltaX;
    private readonly double                                          _deltaY;

    public string Description => "노드 이동";

    public MoveNodesCommand(IEnumerable<NodeBase> nodes, double deltaX, double deltaY)
    {
        _nodePositions = nodes.Select(n => (n, n.X, n.Y)).ToList();
        _deltaX = deltaX;
        _deltaY = deltaY;
    }

    public void Execute()
    {
        foreach (var (node, _, _) in _nodePositions)
        {
            node.X += _deltaX;
            node.Y += _deltaY;
        }
    }

    public void Undo()
    {
        foreach (var (node, oldX, oldY) in _nodePositions)
        {
            node.X = oldX;
            node.Y = oldY;
        }
    }
} 
