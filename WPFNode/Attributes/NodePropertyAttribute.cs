using System;

namespace WPFNode.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class NodePropertyAttribute : Attribute
{
    public string? DisplayName { get; }
    public string? Format { get; set; }
    public bool CanConnectToPort { get; set; }
    public string? OnValueChanged { get; set; }

    public NodePropertyAttribute(string? displayName = null)
    {
        DisplayName = displayName;
    }
} 