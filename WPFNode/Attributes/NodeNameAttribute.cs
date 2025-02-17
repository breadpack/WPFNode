namespace WPFNode.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class NodeNameAttribute : Attribute
{
    public NodeNameAttribute(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public string Name { get; }
}