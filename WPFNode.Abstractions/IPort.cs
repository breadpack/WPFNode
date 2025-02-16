using System;
using System.ComponentModel;
using System.Collections.Generic;

namespace WPFNode.Abstractions;

public interface IPort : INotifyPropertyChanged
{
    Guid Id { get; }
    string Name { get; set; }
    Type DataType { get; }
    bool IsInput { get; }
    bool IsConnected { get; }
    IReadOnlyList<IConnection> Connections { get; }
    INode? Node { get; }
    object? Value { get; set; }
    void AddConnection(IConnection connection);
    void RemoveConnection(IConnection connection);
}

public interface IInputPort : IPort
{
    bool CanAcceptType(Type type);
    object? Value { get; }
}

public interface IOutputPort : IPort {
    bool CanConnectTo(IInputPort targetPort);
    object? Value { get; set; }
    IConnection Connect(IInputPort target);
    void Disconnect();
}