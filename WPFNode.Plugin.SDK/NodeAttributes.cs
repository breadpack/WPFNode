using System;

namespace WPFNode.Plugin.SDK;

[AttributeUsage(AttributeTargets.Property)]
public class PortLabelAttribute : Attribute
{
    public string Label { get; }

    public PortLabelAttribute(string label)
    {
        Label = label;
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class NodeNameAttribute : Attribute
{
    public NodeNameAttribute(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public string Name { get; }
}

[AttributeUsage(AttributeTargets.Class)]
public class NodeCategoryAttribute : Attribute
{
    public NodeCategoryAttribute(string category = "Basic")
    {
        Category = category ?? "Basic";
    }

    public string Category { get; }
}

[AttributeUsage(AttributeTargets.Class)]
public class NodeDescriptionAttribute : Attribute
{
    public NodeDescriptionAttribute(string description)
    {
        Description = description ?? string.Empty;
    }

    public string Description { get; }
} 