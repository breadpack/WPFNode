using WPFNode.Interfaces;

namespace WPFNode.Commands;

public class NodeCommand : ICommand
{
    private readonly INode _node;
    private readonly string _commandName;
    private readonly object? _parameter;

    public string Description => $"{_node.GetType().Name}: {_commandName}";

    public NodeCommand(INode node, string commandName, object? parameter = null)
    {
        _node = node;
        _commandName = commandName;
        _parameter = parameter;
    }

    public void Execute()
    {
        _node.ExecuteCommand(_commandName, _parameter);
    }

    public void Undo()
    {
        // 특수 케이스: 명령이 자체적으로 Undo 메커니즘을 가진 경우
        if (_node.CanExecuteCommand($"Undo{_commandName}", _parameter))
        {
            _node.ExecuteCommand($"Undo{_commandName}", _parameter);
        }
        // 기본적인 Undo 동작은 노드에 따라 다를 수 있으므로 각 노드에서 구현해야 함
    }
}
