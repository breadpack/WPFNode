using System.ComponentModel;
using WPFNode.Models;

namespace WPFNode.Interfaces;

public interface IPort : INotifyPropertyChanged
{
    PortId Id { get; }
    string Name { get; set; }
    Type DataType { get; }
    bool IsInput { get; }
    bool IsConnected { get; }
    bool IsVisible { get; set; }
    IReadOnlyList<IConnection> Connections { get; }
    INode? Node { get; }
    void AddConnection(IConnection connection);
    void RemoveConnection(IConnection connection);
    int GetPortIndex();
}

public interface IInputPort : IPort
{
    bool CanAcceptType(Type type);
    object? Value { get; }
}

public interface IOutputPort : IPort
{
    bool CanConnectTo(IInputPort targetPort);
    object? Value { get; set; }
    IConnection Connect(IInputPort target);
    void Disconnect();
}