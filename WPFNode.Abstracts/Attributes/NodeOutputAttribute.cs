using System;

namespace WPFNode.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class NodeOutputAttribute : Attribute
{
    public string? DisplayName { get; }

    public NodeOutputAttribute(string? displayName = null)
    {
        DisplayName = displayName;
    }
} 