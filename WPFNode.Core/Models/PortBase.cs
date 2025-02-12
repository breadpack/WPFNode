using System.ComponentModel;
using System.Runtime.CompilerServices;
using WPFNode.Abstractions;

namespace WPFNode.Core.Models;

public abstract class PortBase : IPort
{
    private          string            _name;
    private          object?           _value;
    private          bool              _isConnected;
    private readonly List<IConnection> _connections = new();
    private readonly INode             _node;

    protected PortBase(string name, Type dataType, bool isInput, INode node)
    {
        Id = Guid.NewGuid();
        _name = name;
        DataType = dataType;
        IsInput = isInput;
        _node = node ?? throw new ArgumentNullException(nameof(node));
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

    public IReadOnlyList<IConnection> Connections => _connections;

    public INode Node => _node;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void AddConnection(IConnection connection)
    {
        _connections.Add(connection);
        IsConnected = true;
        OnPropertyChanged(nameof(Connections));
    }

    public void RemoveConnection(IConnection connection)
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