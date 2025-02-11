using WPFNode.Core.Models;
using WPFNode.Abstractions;

namespace WPFNode.Core.Commands;

public class ConnectCommand : ICommand
{
    private readonly NodeCanvas   _canvas;
    private readonly IPort        _source;
    private readonly IPort        _target;
    private          IConnection? _connection;

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

public class ReplaceConnectionCommand : ICommand
{
    private readonly NodeCanvas   _canvas;
    private readonly IPort        _source;
    private readonly IPort        _target;
    private readonly IConnection? _oldConnection;
    private          Connection?  _newConnection;

    public string Description => "포트 연결 교체";

    public ReplaceConnectionCommand(NodeCanvas canvas, IPort source, IPort target, IConnection? oldConnection)
    {
        _canvas = canvas;
        _source = source;
        _target = target;
        _oldConnection = oldConnection;
    }

    public void Execute()
    {
        if (_oldConnection != null)
        {
            _canvas.Disconnect(_oldConnection);
        }
        _newConnection = _canvas.Connect(_source, _target);
    }

    public void Undo()
    {
        if (_newConnection != null)
        {
            _canvas.Disconnect(_newConnection);
        }
        if (_oldConnection != null)
        {
            _canvas.Connect(_oldConnection.Source, _oldConnection.Target);
        }
    }
}