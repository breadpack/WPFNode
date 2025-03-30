using System;

namespace WPFNode.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class NodeFlowOutAttribute : Attribute
{
    public string? DisplayName { get; }

    public NodeFlowOutAttribute(string displayName = "Out")
    {
        DisplayName = displayName;
    }
}
