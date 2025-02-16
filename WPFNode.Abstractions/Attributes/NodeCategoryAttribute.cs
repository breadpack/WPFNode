namespace WPFNode.Abstractions.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class NodeCategoryAttribute : Attribute
{
    public NodeCategoryAttribute(string category = "Basic")
    {
        Category = category ?? "Basic";
    }

    public string Category { get; }
}