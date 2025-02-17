namespace WPFNode.Interfaces;

public interface INodeCommandService
{
    void RegisterNode(INode node);
    void UnregisterNode(Guid nodeId);
    bool ExecuteCommand(Guid nodeId, string commandName, object? parameter = null);
    bool CanExecuteCommand(Guid nodeId, string commandName, object? parameter = null);
} 