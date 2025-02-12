namespace WPFNode.Core.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class NodeStyleAttribute : Attribute
{
    public string StyleResourceKey { get; }
    public Type? CustomControlType { get; }
    
    public NodeStyleAttribute(string styleResourceKey = null, Type customControlType = null)
    {
        StyleResourceKey = styleResourceKey;
        CustomControlType = customControlType;
    }
} 