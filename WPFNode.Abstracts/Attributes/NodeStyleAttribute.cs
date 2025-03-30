using System;

namespace WPFNode.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class NodeStyleAttribute : Attribute
{
    public string StyleResourceKey { get; }
    public string ResourceFile { get; }
    public Type? CustomControlType { get; }
    
    public NodeStyleAttribute(string styleResourceKey, string resourceFile = "Themes/Generic.xaml", Type customControlType = null)
    {
        StyleResourceKey = styleResourceKey;
        ResourceFile = resourceFile;
        CustomControlType = customControlType;
    }
} 