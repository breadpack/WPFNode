using WPFNode.Abstractions.Constants;

namespace WPFNode.Abstractions.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class NodePropertyAttribute : Attribute
{
    public string DisplayName { get; }
    public NodePropertyControlType ControlType { get; }
    public string? Format { get; }
    public bool CanConnectToPort { get; }

    public NodePropertyAttribute(
        string displayName, 
        NodePropertyControlType controlType = NodePropertyControlType.TextBox,
        string? format = null,
        bool canConnectToPort = false)
    {
        DisplayName = displayName;
        ControlType = controlType;
        Format = format;
        CanConnectToPort = canConnectToPort;
    }
}