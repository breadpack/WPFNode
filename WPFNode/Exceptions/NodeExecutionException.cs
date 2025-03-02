using System.Runtime.Serialization;
using WPFNode.Constants;
using WPFNode.Models;
using WPFNode.Models.Execution;

namespace WPFNode.Exceptions;

[Serializable]
public class NodeExecutionException : NodeException
{
    public NodeBase Node { get; }
    public IReadOnlyList<NodeBase>? FailedNodes { get; }
    public NodeExecutionState ExecutionState { get; }
    public TimeSpan? ExecutionDuration { get; }

    public NodeExecutionException(string message, NodeBase node) 
        : base(message, LoggerCategories.Execution, "NodeExecution")
    {
        Node = node;
        ExecutionState = NodeExecutionState.Failed;
    }

    public NodeExecutionException(string message, Exception inner, NodeBase node) 
        : base(message, inner, LoggerCategories.Execution, "NodeExecution")
    {
        Node = node;
        ExecutionState = NodeExecutionState.Failed;
    }

    public NodeExecutionException(
        string message, 
        Exception inner, 
        NodeBase node, 
        NodeExecutionState state,
        TimeSpan? duration = null) 
        : base(message, inner, LoggerCategories.Execution, "NodeExecution")
    {
        Node = node;
        ExecutionState = state;
        ExecutionDuration = duration;
    }

    public NodeExecutionException(string message, Exception inner, IReadOnlyList<NodeBase> failedNodes) 
        : base(message, inner, LoggerCategories.Execution, "NodeExecution")
    {
        FailedNodes = failedNodes;
        Node = failedNodes[0];
        ExecutionState = NodeExecutionState.Failed;
    }

    protected NodeExecutionException(SerializationInfo info, StreamingContext context) 
        : base(info, context)
    {
        Node = (NodeBase)info.GetValue(nameof(Node), typeof(NodeBase))!;
        FailedNodes = (IReadOnlyList<NodeBase>?)info.GetValue(nameof(FailedNodes), typeof(IReadOnlyList<NodeBase>));
        ExecutionState = (NodeExecutionState)info.GetValue(nameof(ExecutionState), typeof(NodeExecutionState))!;
        ExecutionDuration = (TimeSpan?)info.GetValue(nameof(ExecutionDuration), typeof(TimeSpan?));
    }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        if (info == null) throw new ArgumentNullException(nameof(info));
        
        info.AddValue(nameof(Node), Node);
        info.AddValue(nameof(FailedNodes), FailedNodes);
        info.AddValue(nameof(ExecutionState), ExecutionState);
        info.AddValue(nameof(ExecutionDuration), ExecutionDuration);
        base.GetObjectData(info, context);
    }
}