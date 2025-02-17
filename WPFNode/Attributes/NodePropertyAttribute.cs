using WPFNode.Constants;

namespace WPFNode.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class NodePropertyAttribute : Attribute
{
    public string DisplayName { get; }
    public NodePropertyControlType ControlType { get; }
    public string? Format { get; }
    public bool CanConnectToPort { get; }
    public string? CustomControlResourceKey { get; }

    public NodePropertyAttribute(
        string displayName, 
        NodePropertyControlType controlType = NodePropertyControlType.TextBox,
        string? format = null,
        bool canConnectToPort = false,
        string? customControlResourceKey = null)
    {
        DisplayName = displayName;
        ControlType = controlType;
        Format = format;
        CanConnectToPort = canConnectToPort;
        CustomControlResourceKey = customControlResourceKey;
    }
}