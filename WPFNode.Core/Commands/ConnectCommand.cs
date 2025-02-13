using WPFNode.Core.Models;
using WPFNode.Abstractions;

namespace WPFNode.Core.Commands;

public class ConnectCommand : ICommand
{
    private readonly NodeCanvas _canvas;
    private readonly IOutputPort _source;
    private readonly IInputPort _target;

    public string Description => "포트 연결";

    public ConnectCommand(NodeCanvas canvas, IOutputPort source, IInputPort target)
    {
        _canvas = canvas;
        _source = source;
        _target = target;
    }

    public void Execute()
    {
        _canvas.Connect(_source, _target);
    }

    public void Undo()
    {
        _source.Disconnect();
    }
}

public class ReplaceConnectionCommand : ICommand
{
    private readonly NodeCanvas _canvas;
    private readonly IOutputPort _newSource;
    private readonly IInputPort _target;
    private readonly IOutputPort? _oldSource;

    public string Description => "포트 연결 교체";

    public ReplaceConnectionCommand(NodeCanvas canvas, IOutputPort newSource, IInputPort target, IOutputPort? oldSource)
    {
        _canvas = canvas;
        _newSource = newSource;
        _target = target;
        _oldSource = oldSource;
    }

    public void Execute()
    {
        _oldSource?.Disconnect();
        _canvas.Connect(_newSource, _target);
    }

    public void Undo()
    {
        _newSource.Disconnect();
        if (_oldSource != null)
        {
            _canvas.Connect(_oldSource, _target);
        }
    }
}