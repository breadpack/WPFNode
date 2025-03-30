using System;

namespace WPFNode.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class NodeCategoryAttribute : Attribute
{
    public string Category { get; }

    public NodeCategoryAttribute(string category)
    {
        Category = category;
    }
}