namespace WPFNode.Models;

public class NodeMetadata
{
    public NodeMetadata(Type nodeType, string name, string category, string description, bool isOutputNode)
    {
        NodeType = nodeType ?? throw new ArgumentNullException(nameof(nodeType));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Category = category ?? throw new ArgumentNullException(nameof(category));
        Description = description ?? string.Empty;
        IsOutputNode = isOutputNode;
    }

    public Type NodeType { get; }
    public string Name { get; }
    public string Category { get; }
    public string Description { get; }
    public bool IsOutputNode { get; }
} 