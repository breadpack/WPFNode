using System.ComponentModel;
using System.Text.Json;
using WPFNode.Exceptions;
using WPFNode.Interfaces;

namespace WPFNode.Models;

public class FlowInPort : IFlowInPort, INotifyPropertyChanged
{
    private readonly int              _index;
    private          bool             _isVisible = true;
    private          List<IConnection> _connections = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public FlowInPort(string name, INode node, int index)
    {
        Name = name;
        Node = node;
        _index = index;
    }

    public PortId Id => new(Node.Guid, true, Name);
    public string Name { get; set; }
    public Type DataType => typeof(void); // Flow 포트는 타입이 없음을 void로 표현
    /// <summary>
    /// Flow 포트는 특정 데이터 타입을 전달하지 않으므로 항상 null을 반환합니다.
    /// </summary>
    public Type? ConnectedType => null;
    public bool IsInput => true;
    public bool IsConnected => _connections.Count > 0;
    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible != value)
            {
                if (!value && IsConnected)
                {
                    foreach (var connection in _connections)
                        connection.Disconnect();
                    
                    _connections.Clear();
                }

                _isVisible = value;
                OnPropertyChanged(nameof(IsVisible));
            }
        }
    }

    public IReadOnlyList<IConnection> Connections    => _connections;
    public INode                      Node           { get; private set; }
    public int                        GetPortIndex() => _index;

    public bool CanAcceptType(Type sourceType)
    {
        // Flow In 포트는 Flow Out 포트의 연결만 허용
        // 타입 검사는 실제로 필요 없음
        return sourceType == typeof(void);
    }

    public void AddConnection(IConnection connection)
    {
        if (connection == null)
            throw new ArgumentNullException(nameof(connection));

        // 기존 연결이 있으면 제거
        _connections.Add(connection);
        OnPropertyChanged(nameof(Connections));
        OnPropertyChanged(nameof(IsConnected));
        OnPropertyChanged(nameof(_connections));
    }

    public void RemoveConnection(IConnection connection)
    {
        if (connection == null)
            throw new NodeConnectionException("연결이 null입니다.");
        if (!connection.Target.Equals(this))
            throw new NodeConnectionException("연결의 타겟 포트가 일치하지 않습니다.", this, connection.Target);

        _connections.Remove(connection);
        OnPropertyChanged(nameof(Connections));
        OnPropertyChanged(nameof(IsConnected));
        OnPropertyChanged(nameof(_connections));
    }

    public IConnection Connect(IFlowOutPort source)
    {
        if (source == null)
            throw new NodeConnectionException("소스 포트가 null입니다.");

        if (!(source is IFlowOutPort))
            throw new NodeConnectionException("Flow In 포트는 Flow Out 포트만 연결할 수 있습니다.", source, this);

        if (source.Node == Node)
            throw new NodeConnectionException("같은 노드의 포트와는 연결할 수 없습니다.", source, this);

        // Canvas를 통해 새로운 연결 생성
        var canvas = ((NodeBase)Node!).Canvas;
        return canvas.Connect(source, this);
    }

    public IConnection Connect(IPort otherPort)
    {
        if (otherPort == null)
            throw new NodeConnectionException("대상 포트가 null입니다.");
            
        if (otherPort is IFlowOutPort flowOutPort)
        {
            return Connect(flowOutPort);
        }
        else
        {
            throw new NodeConnectionException("Flow In 포트는 Flow Out 포트만 연결할 수 있습니다.");
        }
    }

    public void Disconnect()
    {
        foreach (var connection in _connections)
            connection.Disconnect();
                    
        _connections.Clear();
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// 포트 초기화 로직. FlowInPort는 기본적으로 할 일이 없습니다.
    /// </summary>
    public virtual void Initialize() {
        // 기본 구현은 비어 있음
    }

    public void WriteJson(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteString("Name", Name);
        writer.WriteString("Type", "Flow");
        writer.WriteNumber("Index", GetPortIndex());
        writer.WriteBoolean("IsVisible", IsVisible);
        writer.WriteEndObject();
    }

    public void ReadJson(JsonElement element, JsonSerializerOptions options)
    {
        if (element.TryGetProperty("Name", out var nameElement))
            Name = nameElement.GetString()!;
        if (element.TryGetProperty("IsVisible", out var visibleElement))
            IsVisible = visibleElement.GetBoolean();
    }
}
