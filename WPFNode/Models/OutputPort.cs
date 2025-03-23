using System.ComponentModel;
using System.Text.Json;
using WPFNode.Exceptions;
using WPFNode.Interfaces;

namespace WPFNode.Models;

public class OutputPort<T> : IOutputPort, INotifyPropertyChanged {
    private readonly List<IConnection> _connections = new();
    private          T?                _value;
    private readonly int               _index;
    private          INodeCanvas       Canvas => ((NodeBase)Node).Canvas;
    private bool _isVisible = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    public OutputPort(string name, INode node, int index) {
        Name   = name;
        Node   = node;
        _index = index;
    }

    public PortId                     Id             => new(Node.Guid, false, Name);
    public string                     Name           { get; set; }
    public Type                       DataType       => typeof(T);
    public bool                       IsInput        => false;
    public bool                       IsConnected    => _connections.Count > 0;
    public IReadOnlyList<IConnection> Connections    => _connections;
    public INode                      Node           { get; private set; }
    public int                        GetPortIndex() => _index;

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible != value)
            {
                if (!value && IsConnected)
                {
                    // IsVisible이 false로 설정되고 연결이 있는 경우에만 연결 해제
                    Disconnect();
                }
                _isVisible = value;
                OnPropertyChanged(nameof(IsVisible));
            }
        }
    }

    public object? Value {
        get => _value;
        set {
            if (value is T typedValue && !Equals(_value, typedValue)) {
                _value = typedValue;
                OnPropertyChanged(nameof(Value));
            }
        }
    }

    public bool CanConnectTo(IInputPort targetPort) {
        // 같은 노드의 포트와는 연결 불가
        if (targetPort.Node == Node) return false;

        if (IsInput == targetPort.IsInput) return false;

        return targetPort.CanAcceptType(DataType);
    }

    public IConnection Connect(IInputPort target) {
        var canvas = ((NodeBase)Node).Canvas;
        return canvas.Connect(this, target);
    }

    public IConnection Connect(IPort otherPort) {
        if (otherPort == null)
            throw new NodeConnectionException("대상 포트가 null입니다.");
            
        if (otherPort is IInputPort inputPort) {
            return Connect(inputPort);
        }
        else {
            throw new NodeConnectionException("출력 포트는 다른 출력 포트와 연결할 수 없습니다.");
        }
    }

    public void Disconnect() {
        var connections = Connections.ToList();
        foreach (var connection in connections) {
            connection.Disconnect();
        }
    }

    public void Disconnect(IInputPort target) {
        Connections.FirstOrDefault(c => c.Target == target)
                   ?.Disconnect();
    }

    public void AddConnection(IConnection connection)
    {
        if (connection == null)
            throw new NodeConnectionException("연결이 null입니다.");
        if (!connection.Source.Equals(this))
            throw new NodeConnectionException("연결의 소스 포트가 일치하지 않습니다.", this, connection.Source);
            
        _connections.Add(connection);
        OnPropertyChanged(nameof(Connections));
        OnPropertyChanged(nameof(IsConnected));
    }

    public void RemoveConnection(IConnection connection)
    {
        if (connection == null)
            throw new NodeConnectionException("연결이 null입니다.");
        if (!connection.Source.Equals(this))
            throw new NodeConnectionException("연결의 소스 포트가 일치하지 않습니다.", this, connection.Source);
            
        _connections.Remove(connection);
        OnPropertyChanged(nameof(Connections));
        OnPropertyChanged(nameof(IsConnected));
    }

    protected virtual void OnPropertyChanged(string propertyName) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void WriteJson(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteString("Name", Name);
        writer.WriteString("Type", DataType.AssemblyQualifiedName);
        writer.WriteNumber("Index", GetPortIndex());
        writer.WriteBoolean("IsVisible", IsVisible);
        if (_value != null)
        {
            writer.WritePropertyName("Value");
            JsonSerializer.Serialize(writer, _value);
        }
        writer.WriteEndObject();
    }

    public void ReadJson(JsonElement element, JsonSerializerOptions options)
    {
        if (element.TryGetProperty("Name", out var nameElement))
            Name = nameElement.GetString()!;
        if (element.TryGetProperty("IsVisible", out var visibleElement))
            IsVisible = visibleElement.GetBoolean();
        if (element.TryGetProperty("Value", out var valueElement))
        {
            Value = JsonSerializer.Deserialize<T>(valueElement.GetRawText());
        }
    }
}
