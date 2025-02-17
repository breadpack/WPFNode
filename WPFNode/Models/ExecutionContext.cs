namespace WPFNode.Models;

public class ExecutionContext
{
    private readonly Dictionary<Guid, NodeExecutionState> _nodeStates = new();
    private readonly CancellationTokenSource _cancellationTokenSource;
    
    public ExecutionContext()
    {
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public bool IsCancelled => _cancellationTokenSource.Token.IsCancellationRequested;
    
    public CancellationToken CancellationToken => _cancellationTokenSource.Token;

    public void Cancel()
    {
        _cancellationTokenSource.Cancel();
    }

    public NodeExecutionState GetNodeState(NodeBase node)
    {
        return _nodeStates.TryGetValue(node.Id, out var state) ? state : NodeExecutionState.NotStarted;
    }

    public void SetNodeState(NodeBase node, NodeExecutionState state)
    {
        _nodeStates[node.Id] = state;
    }
}

public enum NodeExecutionState
{
    NotStarted,
    Running,
    Completed,
    Failed
} 