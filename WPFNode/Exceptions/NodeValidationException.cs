using System.Runtime.Serialization;
using WPFNode.Constants;
using WPFNode.Interfaces;

namespace WPFNode.Exceptions;

[Serializable]
public class NodeValidationException : NodeException
{
    public INode? Node { get; }
    public string? PropertyName { get; }
    public IDictionary<string, string>? ValidationErrors { get; }

    public NodeValidationException(string message) 
        : base(message, LoggerCategories.Validation, "Validation") { }
    
    public NodeValidationException(string message, INode node) 
        : base(message, LoggerCategories.Validation, "Validation")
    {
        Node = node;
    }
    
    public NodeValidationException(string message, INode node, string propertyName) 
        : base(message, LoggerCategories.Validation, "Validation")
    {
        Node = node;
        PropertyName = propertyName;
    }

    public NodeValidationException(string message, INode node, IDictionary<string, string> errors) 
        : base(message, LoggerCategories.Validation, "Validation")
    {
        Node = node;
        ValidationErrors = errors;
    }
    
    public NodeValidationException(string message, Exception inner) 
        : base(message, inner, LoggerCategories.Validation, "Validation") { }
    
    public NodeValidationException(string message, INode node, Exception inner) 
        : base(message, inner, LoggerCategories.Validation, "Validation")
    {
        Node = node;
    }

    protected NodeValidationException(SerializationInfo info, StreamingContext context) 
        : base(info, context)
    {
        Node = (INode?)info.GetValue(nameof(Node), typeof(INode));
        PropertyName = info.GetString(nameof(PropertyName));
        ValidationErrors = (IDictionary<string, string>?)info.GetValue(nameof(ValidationErrors), typeof(IDictionary<string, string>));
    }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        if (info == null) throw new ArgumentNullException(nameof(info));
        
        info.AddValue(nameof(Node), Node);
        info.AddValue(nameof(PropertyName), PropertyName);
        info.AddValue(nameof(ValidationErrors), ValidationErrors);
        base.GetObjectData(info, context);
    }
} 