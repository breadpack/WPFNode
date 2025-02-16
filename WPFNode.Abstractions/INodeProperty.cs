using WPFNode.Abstractions.Constants;

namespace WPFNode.Abstractions;

public interface INodeProperty
{
    string DisplayName { get; }
    NodePropertyControlType ControlType { get; }
    string? Format { get; }
    bool CanConnectToPort { get; }
    
    object? GetValue();
    void SetValue(object? value);
}