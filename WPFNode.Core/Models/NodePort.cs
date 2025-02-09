using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPFNode.Core.Models;

public class NodePort : INotifyPropertyChanged
{
    private string _name;
    private readonly List<Connection> _connections;

    public NodePort(string id, string name, Type dataType, bool isInput, Node parent)
    {
        Id = id;
        _name = name;
        DataType = dataType;
        IsInput = isInput;
        Parent = parent;
        _connections = new List<Connection>();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public string Id { get; }

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
    public Node Parent { get; }
    public IReadOnlyList<Connection> Connections => _connections;

    internal void AddConnection(Connection connection)
    {
        if (!_connections.Contains(connection))
        {
            _connections.Add(connection);
            OnPropertyChanged(nameof(Connections));
        }
    }

    internal void RemoveConnection(Connection connection)
    {
        if (_connections.Remove(connection))
        {
            OnPropertyChanged(nameof(Connections));
        }
    }

    public bool CanConnectTo(NodePort other)
    {
        if (IsInput == other.IsInput) return false;
        if (Parent == other.Parent) return false;
        return DataType.IsAssignableFrom(other.DataType);
    }
} 
