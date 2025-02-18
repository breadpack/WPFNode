using System.ComponentModel;
using WPFNode.Constants;

namespace WPFNode.Interfaces;

public interface INodeProperty : INotifyPropertyChanged, IJsonSerializable
{
    string DisplayName { get; }
    NodePropertyControlType ControlType { get; }
    string? Format { get; }
    bool CanConnectToPort { get; set; }
    Type PropertyType { get; }
    Type? ElementType { get; }
    bool IsVisible { get; }
    
    // 값 관련
    object? Value { get; set; }
    
    // 포트 연결 관련
    IInputPort? ConnectedPort { get; }
    bool IsConnectedToPort { get; }
    void ConnectToPort(IInputPort port);
    void DisconnectFromPort();
}