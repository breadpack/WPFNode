using System;
using System.Runtime.Serialization;

namespace WPFNode.Exceptions;

public class NodeException : Exception
{
    public string? Category { get; }
    public string? Operation { get; }

    public NodeException() { }
    
    public NodeException(string message) : base(message) { }
    
    public NodeException(string message, Exception inner) : base(message, inner) { }
    
    public NodeException(string message, string category, string operation) 
        : base(message)
    {
        Category = category;
        Operation = operation;
    }
    
    public NodeException(string message, Exception inner, string category, string operation) 
        : base(message, inner)
    {
        Category = category;
        Operation = operation;
    }

    protected NodeException(SerializationInfo info, StreamingContext context) 
        : base(info, context)
    {
        Category = info.GetString(nameof(Category));
        Operation = info.GetString(nameof(Operation));
    }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        if (info == null) throw new ArgumentNullException(nameof(info));
        
        info.AddValue(nameof(Category), Category);
        info.AddValue(nameof(Operation), Operation);
        base.GetObjectData(info, context);
    }
} 