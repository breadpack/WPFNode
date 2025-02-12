namespace WPFNode.Core.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class NodeDescriptionAttribute : Attribute
{
    public NodeDescriptionAttribute(string description)
    {
        Description = description ?? string.Empty;
    }

    public string Description { get; }
} 