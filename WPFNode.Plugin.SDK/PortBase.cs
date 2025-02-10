using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using WPFNode.Abstractions;

namespace WPFNode.Plugin.SDK;

public abstract class PortBase : IPort
{
    private string _name;
    private object? _value;
    private bool _isConnected;
    private readonly List<Connection> _connections = new();

    public PortBase(string name, Type dataType, bool isInput)
    {
        Id = Guid.NewGuid();
        _name = name;
        DataType = dataType;
        IsInput = isInput;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public Guid Id { get; internal set; }
    
    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged();
            }
        }
    }

    public Type DataType { get; }
    public bool IsInput { get; }
    
    public bool IsConnected
    {
        get => _isConnected;
        protected set
        {
            if (_isConnected != value)
            {
                _isConnected = value;
                OnPropertyChanged();
            }
        }
    }

    public object? Value
    {
        get => _value;
        set
        {
            if (_value != value)
            {
                if (value != null && !DataType.IsAssignableFrom(value.GetType()))
                {
                    throw new ArgumentException($"값의 타입이 일치하지 않습니다. 예상: {DataType.Name}, 실제: {value.GetType().Name}");
                }
                _value = value;
                OnPropertyChanged();
            }
        }
    }

    public IReadOnlyList<Connection> Connections => _connections;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void AddConnection(Connection connection)
    {
        _connections.Add(connection);
        IsConnected = true;
        OnPropertyChanged(nameof(Connections));
    }

    public void RemoveConnection(Connection connection)
    {
        _connections.Remove(connection);
        IsConnected = _connections.Count > 0;
        OnPropertyChanged(nameof(Connections));
    }

    internal PortBase Clone()
    {
        var clone = (PortBase)MemberwiseClone();
        clone.Id = Guid.NewGuid();
        clone._connections.Clear();
        return clone;
    }
} 