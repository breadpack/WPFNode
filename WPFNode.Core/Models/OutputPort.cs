using System;
using System.Collections.Generic;
using System.ComponentModel;
using WPFNode.Abstractions;

namespace WPFNode.Core.Models;

public class OutputPort<T> : IOutputPort, INotifyPropertyChanged
{
    private readonly List<IConnection> _connections = new();
    private T? _value;
    private INodeCanvas Canvas => ((NodeBase)Node).Canvas;

    public event PropertyChangedEventHandler? PropertyChanged;

    public OutputPort(string name, INode node)
    {
        Id = Guid.NewGuid();
        Name = name;
        Node = node;
    }

    public Guid Id { get; }
    public string Name { get; set; }
    public Type DataType => typeof(T);
    public bool IsInput => false;
    public bool IsConnected => _connections.Count > 0;
    public IReadOnlyList<IConnection> Connections => _connections;
    public INode? Node { get; private set; }

    public object? Value
    {
        get => _value;
        set
        {
            if (value is T typedValue && !Equals(_value, typedValue))
            {
                _value = typedValue;
                OnPropertyChanged(nameof(Value));
            }
        }
    }

    public bool CanConnectTo(IInputPort targetPort)
    {
        // 같은 노드의 포트와는 연결 불가
        if (targetPort.Node == Node) return false;

        if (IsInput == targetPort.IsInput) return false;

        return targetPort.CanAcceptType(DataType);
    }

    public IConnection Connect(IInputPort target)
    {
        Canvas.Connect(this, target);
        var connection = Canvas.Connections.First(c => c.Source == this && c.Target == target);
        return connection;
    }

    public void Disconnect()
    {
        var connections = Connections.ToList();
        foreach (var connection in connections)
        {
            Canvas.Disconnect(connection);
        }
    }

    public void AddConnection(IConnection connection)
    {
        if (connection == null)
            throw new ArgumentNullException(nameof(connection));
        _connections.Add(connection);
        OnPropertyChanged(nameof(Connections));
        OnPropertyChanged(nameof(IsConnected));
    }

    public void RemoveConnection(IConnection connection)
    {
        if (connection == null)
            throw new ArgumentNullException(nameof(connection));
        _connections.Remove(connection);
        OnPropertyChanged(nameof(Connections));
        OnPropertyChanged(nameof(IsConnected));
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
} 