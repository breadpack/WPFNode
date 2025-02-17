using WPFNode.Interfaces;
using WPFNode.Models;

namespace WPFNode.Commands;

public class ResizeGroupCommand : ICommand
{
    private readonly NodeGroup _group;
    private readonly double _oldX;
    private readonly double _oldY;
    private readonly double _oldWidth;
    private readonly double _oldHeight;
    private readonly double _newX;
    private readonly double _newY;
    private readonly double _newWidth;
    private readonly double _newHeight;

    public string Description => "그룹 크기 조절";

    public ResizeGroupCommand(NodeGroup group, double newX, double newY, double newWidth, double newHeight)
    {
        _group = group;
        _oldX = group.X;
        _oldY = group.Y;
        _oldWidth = group.Width;
        _oldHeight = group.Height;
        _newX = newX;
        _newY = newY;
        _newWidth = newWidth;
        _newHeight = newHeight;
    }

    public void Execute()
    {
        _group.ResizeAndMoveNodes(_newX, _newY, _newWidth, _newHeight);
    }

    public void Undo()
    {
        _group.ResizeAndMoveNodes(_oldX, _oldY, _oldWidth, _oldHeight);
    }
} 
