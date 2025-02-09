using System;

namespace WPFNode.Plugin.SDK;

public class NodeMetadata
{
    public NodeMetadata(Type nodeType, string name, string category, string description)
    {
        NodeType = nodeType ?? throw new ArgumentNullException(nameof(nodeType));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Category = category ?? throw new ArgumentNullException(nameof(category));
        Description = description ?? string.Empty;
    }

    public Type NodeType { get; }
    public string Name { get; }
    public string Category { get; }
    public string Description { get; }
} 