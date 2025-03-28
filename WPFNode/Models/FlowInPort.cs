using System.ComponentModel;
using System.Text.Json;
using WPFNode.Exceptions;
using WPFNode.Interfaces;

namespace WPFNode.Models;

public class FlowInPort : IFlowInPort, IFlowNodePort, INotifyPropertyChanged
{
    private readonly int _index;
    private bool _isVisible = true;
    private IConnection? _connection;

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
    public bool IsInput => true;
    public bool IsConnected => _connection != null;
    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible != value)
            {
                if (!value && IsConnected)
                {
                    _connection?.Disconnect();
                }

                _isVisible = value;
                OnPropertyChanged(nameof(IsVisible));
            }
        }
    }

    public IReadOnlyList<IConnection> Connections => _connection != null ? new[] { _connection } : Array.Empty<IConnection>();
    public IConnection? Connection => _connection;
    public INode Node { get; private set; }
    public int GetPortIndex() => _index;

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
        _connection?.Disconnect();

        _connection = connection;
        OnPropertyChanged(nameof(Connections));
        OnPropertyChanged(nameof(IsConnected));
        OnPropertyChanged(nameof(Connection));
    }

    public void RemoveConnection(IConnection connection)
    {
        if (connection == null)
            throw new NodeConnectionException("연결이 null입니다.");
        if (!connection.Target.Equals(this))
            throw new NodeConnectionException("연결의 타겟 포트가 일치하지 않습니다.", this, connection.Target);

        if (_connection == connection)
        {
            _connection = null;
            OnPropertyChanged(nameof(Connections));
            OnPropertyChanged(nameof(IsConnected));
            OnPropertyChanged(nameof(Connection));
        }
    }

    public IConnection Connect(IOutputPort source)
    {
        if (source == null)
            throw new NodeConnectionException("소스 포트가 null입니다.");

        if (!(source is IFlowOutPort))
            throw new NodeConnectionException("Flow In 포트는 Flow Out 포트만 연결할 수 있습니다.", source, this);

        if (source.Node == Node)
            throw new NodeConnectionException("같은 노드의 포트와는 연결할 수 없습니다.", source, this);

        // 기존 연결이 있으면 삭제
        _connection?.Disconnect();

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
        _connection?.Disconnect();
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
    
    /// <summary>
    /// 이 Flow 입력 포트를 실행합니다. 이 메서드가 호출되면 포트가 속한 노드가 실행됩니다.
    /// </summary>
    public void Execute()
    {
        // 연결된 노드 실행
        if (Node is NodeBase nodeBase)
        {
            // 노드의 실행 로직을 호출합니다.
            // 비동기 메서드를 동기적으로 호출하므로 주의해야 합니다.
            try
            {
                // ExecuteAsync를 동기적으로 호출하기 위해 Wait 사용
                // 실제 프로덕션 환경에서는 안전한 비동기 호출 패턴을 사용하는 것이 좋습니다.
                nodeBase.ExecuteAsync(CancellationToken.None).Wait();
            }
            catch (AggregateException ae)
            {
                // 원본 예외 전파
                if (ae.InnerException != null)
                    throw ae.InnerException;
                throw;
            }
        }
    }
}
