using System;

namespace WPFNode.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class NodeFlowInAttribute : Attribute
{
    public string? DisplayName { get; }

    public NodeFlowInAttribute(string? displayName = null)
    {
        DisplayName = displayName;
    }
}
