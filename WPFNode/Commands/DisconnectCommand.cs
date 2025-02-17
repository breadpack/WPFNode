using WPFNode.Interfaces;
using WPFNode.Models;

namespace WPFNode.Commands;

public class DisconnectCommand : ICommand
{
    private readonly NodeCanvas  _canvas;
    private readonly IConnection _connection;
    private readonly IPort       _source;
    private readonly IPort       _target;

    public string Description => "포트 연결 해제";

    public DisconnectCommand(NodeCanvas canvas, IConnection connection)
    {
        _canvas = canvas;
        _connection = connection;
        _source = connection.Source;
        _target = connection.Target;
    }

    public void Execute()
    {
        _canvas.Disconnect(_connection);
    }

    public void Undo()
    {
        _canvas.Connect(_source, _target);
    }
} 