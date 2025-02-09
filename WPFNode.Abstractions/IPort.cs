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
    object? Value { get; set; }
    IReadOnlyList<Connection> Connections { get; }
}

public interface IPort<T> : IPort
{
    new T? Value { get; set; }
} 