using WPFNode.Interfaces;

namespace WPFNode.Services;

public class NodeCommandService : INodeCommandService
{
    private readonly Dictionary<Guid, INode> _nodes = new();
    private readonly INodePluginService _pluginService;

    public NodeCommandService(INodePluginService pluginService)
    {
        _pluginService = pluginService;
    }

    public void RegisterNode(INode node)
    {
        if (node == null) throw new ArgumentNullException(nameof(node));
        _nodes[node.Id] = node;
    }

    public void UnregisterNode(Guid nodeId)
    {
        _nodes.Remove(nodeId);
    }

    public bool ExecuteCommand(Guid nodeId, string commandName, object? parameter = null)
    {
        if (!_nodes.TryGetValue(nodeId, out var node))
            return false;

        if (!node.CanExecuteCommand(commandName, parameter))
            return false;

        try
        {
            node.ExecuteCommand(commandName, parameter);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool CanExecuteCommand(Guid nodeId, string commandName, object? parameter = null)
    {
        return _nodes.TryGetValue(nodeId, out var node) && 
               node.CanExecuteCommand(commandName, parameter);
    }
} 