using System;
using System.Runtime.Serialization;
using WPFNode.Constants;

namespace WPFNode.Exceptions;

public class NodeCreationException : NodeException
{
    public Type? NodeType { get; }

    public NodeCreationException(string message) 
        : base(message, LoggerCategories.Node, "NodeCreation") { }
    
    public NodeCreationException(string message, Type nodeType) 
        : base(message, LoggerCategories.Node, "NodeCreation")
    {
        NodeType = nodeType;
    }
    
    public NodeCreationException(string message, Exception inner) 
        : base(message, inner, LoggerCategories.Node, "NodeCreation") { }
    
    public NodeCreationException(string message, Type nodeType, Exception inner) 
        : base(message, inner, LoggerCategories.Node, "NodeCreation")
    {
        NodeType = nodeType;
    }

    protected NodeCreationException(SerializationInfo info, StreamingContext context) 
        : base(info, context)
    {
        NodeType = (Type?)info.GetValue(nameof(NodeType), typeof(Type));
    }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        if (info == null) throw new ArgumentNullException(nameof(info));
        
        info.AddValue(nameof(NodeType), NodeType);
        base.GetObjectData(info, context);
    }
} 