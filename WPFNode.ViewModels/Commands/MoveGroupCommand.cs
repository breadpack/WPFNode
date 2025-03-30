using WPFNode.Interfaces;
using WPFNode.Models;

namespace WPFNode.Commands;

public class MoveGroupCommand : ICommand
{
    private readonly NodeGroup _group;
    private readonly double _deltaX;
    private readonly double _deltaY;
    private readonly double _oldX;
    private readonly double _oldY;

    public string Description => "그룹 이동";

    public MoveGroupCommand(NodeGroup group, double deltaX, double deltaY)
    {
        _group = group;
        _deltaX = deltaX;
        _deltaY = deltaY;
        _oldX = group.X;
        _oldY = group.Y;
    }

    public void Execute()
    {
        _group.X = _oldX + _deltaX;
        _group.Y = _oldY + _deltaY;
    }

    public void Undo()
    {
        _group.X = _oldX;
        _group.Y = _oldY;
    }
} 
