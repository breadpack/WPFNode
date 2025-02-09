using WPFNode.Core.Models;
using WPFNode.Abstractions;

namespace WPFNode.Core.Commands;

public class ConnectCommand : ICommand
{
    private readonly NodeCanvas _canvas;
    private readonly IPort _source;
    private readonly IPort _target;
    private Connection? _connection;

    public string Description => "포트 연결";

    public ConnectCommand(NodeCanvas canvas, IPort source, IPort target)
    {
        _canvas = canvas;
        _source = source;
        _target = target;
    }

    public void Execute()
    {
        _connection = _canvas.Connect(_source, _target);
    }

    public void Undo()
    {
        if (_connection != null)
        {
            _canvas.Disconnect(_connection);
            _connection = null;
        }
    }
}