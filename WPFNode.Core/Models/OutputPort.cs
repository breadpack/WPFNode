using System;
using System.Collections.Generic;
using WPFNode.Abstractions;

namespace WPFNode.Core.Models;

public class OutputPort<T> : PortBase, IOutputPort
{
    private T? _value;
    private INodeCanvas Canvas => ((NodeBase)Node).Canvas;

    internal OutputPort(string name, INode node) : base(name, typeof(T), false, node)
    {
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

    public object? Value
    {
        get => _value;
        set
        {
            if (value != null && !typeof(T).IsAssignableFrom(value.GetType()))
            {
                throw new ArgumentException($"[{Name}] 타입이 일치하지 않습니다. 예상: {typeof(T).Name}, 실제: {value.GetType().Name}");
            }

            var typedValue = (T?)value;
            if (!EqualityComparer<T?>.Default.Equals(_value, typedValue))
            {
                _value = typedValue;
                OnPropertyChanged();
            }
        }
    }
} 