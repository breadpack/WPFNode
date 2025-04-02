using System.ComponentModel;
using System.Text.Json;
using WPFNode.Exceptions;
using WPFNode.Interfaces;

namespace WPFNode.Models;

public class FlowOutPort : IFlowOutPort, INotifyPropertyChanged
{
    private readonly List<IConnection> _connections = new();
    private readonly int _index;
    private bool _isVisible = true;
    private INodeCanvas Canvas => ((NodeBase)Node).Canvas;

    public event PropertyChangedEventHandler? PropertyChanged;

    public FlowOutPort(string name, INode node, int index)
    {
        Name = name;
        Node = node;
        _index = index;
    }

    public PortId Id => new(Node.Guid, false, Name);
    public string Name { get; set; }
    public Type DataType => typeof(void); // Flow 포트는 타입이 없음을 void로 표현
    public bool IsInput => false;
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
                    // IsVisible이 false로 설정되고 연결이 있는 경우에만 연결 해제
                    Disconnect();
                }
                _isVisible = value;
                OnPropertyChanged(nameof(IsVisible));
            }
        }
    }

    public IReadOnlyList<IConnection> Connections => _connections;
    public INode Node { get; private set; }
    public object? Value { get; set; } // Flow 포트는 값이 없지만, 인터페이스 구현을 위해 포함
    public int GetPortIndex() => _index;

    public IEnumerable<IFlowInPort> ConnectedFlowPorts
    {
        get
        {
            return _connections
                .Select(c => c.Target)
                .OfType<IFlowInPort>();
        }
    }

    public bool CanConnectTo(IInputPort targetPort)
    {
        // 같은 노드의 포트와는 연결 불가
        if (targetPort.Node == Node) return false;

        // 입력 포트여야 하고, Flow In 포트여야 함
        if (!targetPort.IsInput || !(targetPort is IFlowInPort)) return false;

        return true;
    }

    public IConnection Connect(IFlowInPort target)
    {
        if (target == null)
            throw new NodeConnectionException("타겟 포트가 null입니다.");

        if (target.Node == Node)
            throw new NodeConnectionException("같은 노드의 포트와는 연결할 수 없습니다.", this, target);

        // Canvas를 통해 새로운 연결 생성
        return Canvas.Connect(this, target);
    }

    public IConnection Connect(IPort otherPort)
    {
        if (otherPort == null)
            throw new NodeConnectionException("대상 포트가 null입니다.");
            
        if (otherPort is IFlowInPort flowInPort)
        {
            return Connect(flowInPort);
        }
        else
        {
            throw new NodeConnectionException("Flow Out 포트는 Flow In 포트만 연결할 수 있습니다.");
        }
    }

    public void Disconnect()
    {
        var connections = Connections.ToList();
        foreach (var connection in connections)
        {
            connection.Disconnect();
        }
    }

    public void Disconnect(IInputPort target)
    {
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
        OnPropertyChanged(nameof(ConnectedFlowPorts));
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
        OnPropertyChanged(nameof(ConnectedFlowPorts));
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// 포트 초기화 로직. FlowOutPort는 기본적으로 할 일이 없습니다.
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
