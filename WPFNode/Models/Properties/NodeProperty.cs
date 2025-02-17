using System.ComponentModel;
using WPFNode.Constants;
using WPFNode.Exceptions;
using WPFNode.Interfaces;
using WPFNode.Utilities;

namespace WPFNode.Models.Properties;

public class NodeProperty<T> : INodeProperty, IInputPort
{
    private T? _value;
    private readonly INode? _node;
    private readonly List<IConnection> _connections = new();
    private bool _canConnectToPort;
    private bool _isVisible;
    private int _portIndex;

    public event PropertyChangedEventHandler? PropertyChanged;

    public NodeProperty(
        string displayName,
        NodePropertyControlType controlType,
        INode node,
        int portIndex,
        string? format = null,
        bool canConnectToPort = false)
    {
        DisplayName = displayName;
        Name = displayName;
        ControlType = controlType;
        Format = format;
        PropertyType = typeof(T);
        ElementType = GetElementType(PropertyType);
        
        _node = node;
        _portIndex = portIndex;

        // CanConnectToPort가 true일 때만 포트가 보이도록 설정
        _canConnectToPort = canConnectToPort;
        _isVisible = canConnectToPort;
    }

    // INodeProperty 구현
    public string DisplayName { get; }
    public NodePropertyControlType ControlType { get; }
    public string? Format { get; }
    public bool CanConnectToPort 
    { 
        get => _canConnectToPort;
        set
        {
            if (_canConnectToPort != value)
            {
                // 포트 연결이 비활성화될 때 기존 연결 해제
                if (!value && IsConnected)
                {
                    DisconnectFromPort();
                }

                _canConnectToPort = value;
                _isVisible = value;  // IsVisible도 함께 업데이트
                
                OnPropertyChanged(nameof(CanConnectToPort));
                OnPropertyChanged(nameof(IsVisible));
                OnPropertyChanged(nameof(ConnectedPort));
                OnPropertyChanged(nameof(IsConnectedToPort));
            }
        }
    }
    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible != value)
            {
                if (!value && IsConnected)
                {
                    DisconnectFromPort();
                }
                _isVisible = value;
                OnPropertyChanged(nameof(IsVisible));
            }
        }
    }
    public Type PropertyType { get; }
    public Type? ElementType { get; }
    public IInputPort? ConnectedPort => IsConnected ? this : null;
    public bool IsConnectedToPort => IsConnected;

    object? INodeProperty.Value
    {
        get => Value;
        set
        {
            if (value is T typedValue)
            {
                Value = typedValue;
            }
        }
    }

    public T? Value
    {
        get
        {
            // 포트로 연결된 경우 연결된 포트의 값을 우선 사용
            if (CanConnectToPort && IsConnected)
            {
                var connectedValue = ((IInputPort)this).Value;
                if (connectedValue != null && connectedValue is T typedValue)
                {
                    return typedValue;
                }
            }
            return _value;
        }
        set
        {
            if (!Equals(_value, value))
            {
                _value = value;
                OnPropertyChanged(nameof(Value));
            }
        }
    }

    object? IInputPort.Value
    {
        get
        {
            if (IsConnected && _connections[0].Source is IOutputPort outputPort)
            {
                var sourceValue = outputPort.Value;
                if (sourceValue == null) return default(T);

                if (TryConvertValue(sourceValue, out T? convertedValue))
                {
                    return convertedValue;
                }
            }
            return _value;
        }
    }

    private bool TryConvertValue(object? sourceValue, out T? result)
    {
        return sourceValue.TryConvertTo(out result);
    }

    // IInputPort 구현
    public PortId Id
    {
        get
        {
            if (_node == null)
                throw new InvalidOperationException("노드가 설정되지 않았습니다.");
            
            return new PortId(_node.Id, true, _portIndex);
        }
    }

    public int GetPortIndex() => _portIndex;

    public string Name { get; set; }
    public Type DataType => PropertyType;
    public bool IsInput => true;
    public bool IsConnected => Connections.Count > 0;
    public IReadOnlyList<IConnection> Connections => _connections;
    public INode? Node => _node;

    public void AddConnection(IConnection connection)
    {
        if (connection == null)
            throw new NodeConnectionException("연결이 null입니다.");
        if (!connection.Target.Equals(this))
            throw new NodeConnectionException("연결의 타겟 포트가 일치하지 않습니다.", connection.Target, this);
        if (!CanConnectToPort)
            throw new NodeConnectionException("이 프로퍼티는 포트 연결을 허용하지 않습니다.");
            
        _connections.Add(connection);
        OnPropertyChanged(nameof(Connections));
        OnPropertyChanged(nameof(IsConnected));
        OnPropertyChanged(nameof(Value));  // 연결 시 값 업데이트 알림
    }

    public void RemoveConnection(IConnection connection)
    {
        if (connection == null)
            throw new NodeConnectionException("연결이 null입니다.");
        if (!connection.Target.Equals(this))
            throw new NodeConnectionException("연결의 타겟 포트가 일치하지 않습니다.", connection.Target, this);
            
        _connections.Remove(connection);
        OnPropertyChanged(nameof(Connections));
        OnPropertyChanged(nameof(IsConnected));
        OnPropertyChanged(nameof(Value));  // 연결 해제 시 값 업데이트 알림
    }

    public bool CanAcceptType(Type type)
    {
        // 포트로 사용되지 않는 경우 연결 불가
        if (!CanConnectToPort)
            return false;

        if (type == null)
            throw new NodeConnectionException("타입이 null입니다.");

        return type.CanImplicitlyConvertTo(PropertyType);
    }

    private static Type? GetElementType(Type type)
    {
        return type.GetElementType();
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void ConnectToPort(IInputPort port)
    {
        // 이미 자신이 InputPort이므로 구현 불필요
    }

    public void DisconnectFromPort()
    {
        // 연결된 모든 연결 해제
        foreach (var connection in Connections.ToArray())
        {
            connection.Disconnect();
        }
        
        // 값을 로컬 값으로 복원
        if (_connections.Count > 0)
        {
            Value = _value;  // 로컬 값 사용
        }
    }
} 