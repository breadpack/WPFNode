namespace WPFNode.Plugin.SDK.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class PortLabelAttribute : Attribute
{
    public string Label { get; }

    public PortLabelAttribute(string label)
    {
        Label = label;
    }
}