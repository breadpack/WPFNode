using System;

namespace WPFNode.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class NodeInputAttribute : Attribute
{
    public string? DisplayName { get; }
    public string? Format { get; set; }
    public bool CanConnectToPort { get; set; } = true;

    public NodeInputAttribute(string? displayName = null)
    {
        DisplayName = displayName;
    }
} 