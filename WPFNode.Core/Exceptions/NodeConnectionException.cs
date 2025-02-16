using System;
using System.Runtime.Serialization;
using WPFNode.Abstractions;
using WPFNode.Core.Constants;

namespace WPFNode.Core.Exceptions;

[Serializable]
public class NodeConnectionException : NodeException
{
    public IPort? SourcePort { get; }
    public IPort? TargetPort { get; }
    public string? ConnectionId { get; }

    public NodeConnectionException(string message) 
        : base(message, LoggerCategories.Connection, "Connection") { }

    public NodeConnectionException(string message, IPort source, IPort target) 
        : base(message, LoggerCategories.Connection, "Connection")
    {
        SourcePort = source;
        TargetPort = target;
    }

    public NodeConnectionException(string message, string connectionId) 
        : base(message, LoggerCategories.Connection, "Connection")
    {
        ConnectionId = connectionId;
    }

    public NodeConnectionException(string message, Exception inner) 
        : base(message, inner, LoggerCategories.Connection, "Connection") { }

    public NodeConnectionException(string message, IPort source, IPort target, Exception inner) 
        : base(message, inner, LoggerCategories.Connection, "Connection")
    {
        SourcePort = source;
        TargetPort = target;
    }

    protected NodeConnectionException(SerializationInfo info, StreamingContext context) 
        : base(info, context)
    {
        SourcePort = (IPort?)info.GetValue(nameof(SourcePort), typeof(IPort));
        TargetPort = (IPort?)info.GetValue(nameof(TargetPort), typeof(IPort));
        ConnectionId = info.GetString(nameof(ConnectionId));
    }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        if (info == null) throw new ArgumentNullException(nameof(info));
        
        info.AddValue(nameof(SourcePort), SourcePort);
        info.AddValue(nameof(TargetPort), TargetPort);
        info.AddValue(nameof(ConnectionId), ConnectionId);
        base.GetObjectData(info, context);
    }
} 