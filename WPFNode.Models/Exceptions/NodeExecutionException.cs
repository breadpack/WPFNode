using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using WPFNode.Constants;
using WPFNode.Interfaces;
using WPFNode.Models.Execution;

namespace WPFNode.Exceptions;

public class NodeExecutionException : NodeException
{
    public INode                 Node              { get; }
    public IReadOnlyList<INode>? FailedNodes       { get; }
    public INodeExecutionState    ExecutionState    { get; }
    public TimeSpan?             ExecutionDuration { get; }

    public NodeExecutionException(string message, INode node) 
        : base(message, LoggerCategories.Execution, "NodeExecution")
    {
        Node = node;
        ExecutionState = NodeExecutionState.Failed;
    }

    public NodeExecutionException(string message, Exception inner, INode node) 
        : base(message, inner, LoggerCategories.Execution, "NodeExecution")
    {
        Node = node;
        ExecutionState = NodeExecutionState.Failed;
    }

    public NodeExecutionException(
        string             message, 
        Exception          inner, 
        INode              node, 
        NodeExecutionState state,
        TimeSpan?          duration = null) 
        : base(message, inner, LoggerCategories.Execution, "NodeExecution")
    {
        Node = node;
        ExecutionState = state;
        ExecutionDuration = duration;
    }

    public NodeExecutionException(string message, Exception inner, IReadOnlyList<INode> failedNodes) 
        : base(message, inner, LoggerCategories.Execution, "NodeExecution")
    {
        FailedNodes = failedNodes;
        Node = failedNodes[0];
        ExecutionState = NodeExecutionState.Failed;
    }

    protected NodeExecutionException(SerializationInfo info, StreamingContext context) 
        : base(info, context)
    {
        Node              = (INode)info.GetValue(nameof(Node), typeof(INode))!;
        FailedNodes       = (IReadOnlyList<INode>?)info.GetValue(nameof(FailedNodes), typeof(IReadOnlyList<INode>));
        ExecutionState    = (NodeExecutionState)info.GetValue(nameof(ExecutionState), typeof(NodeExecutionState))!;
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