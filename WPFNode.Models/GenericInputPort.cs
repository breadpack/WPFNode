using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using WPFNode.Exceptions;
using WPFNode.Interfaces;
using WPFNode.Utilities; // For CanConvertTo, TryConvertTo

namespace WPFNode.Models;

/// <summary>
/// 연결된 OutputPort의 타입에 따라 자신의 타입을 동적으로 결정하는 InputPort입니다.
/// </summary>
public class GenericInputPort : IInputPort, INotifyPropertyChanged, IDisposable
{
    private readonly int _index;
    private bool _isVisible = true;
    private IConnection? _connection;
    private Type? _currentResolvedType; // 현재 결정된 실제 데이터 타입

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// GenericInputPort를 생성합니다.
    /// </summary>
    /// <param name="name">포트 이름</param>
    /// <param name="node">소속 노드</param>
    /// <param name="index">포트 인덱스 (NodeBase에서 관리)</param>
    public GenericInputPort(string name, INode node, int index)
    {
        Name = name;
        Node = node;
        _index = index;
        // 초기 타입은 object 또는 null
        _currentResolvedType = typeof(object);
    }

    public PortId Id => new(Node.Guid, true, Name);
    public string Name { get; set; }

    /// <summary>
    /// 현재 동적으로 결정된 데이터 타입입니다. 연결되지 않았거나 타입을 알 수 없으면 object를 반환합니다.
    /// </summary>
    public Type DataType => CurrentResolvedType ?? typeof(object);

    public bool IsInput => true;
    public bool IsConnected => _connection != null;

    /// <summary>
    /// 이 포트에 직접 연결된 OutputPort의 타입입니다. 연결되지 않은 경우 null입니다.
    /// </summary>
    [JsonIgnore]
    public Type? ConnectedType => _connection?.Source?.DataType;

    /// <summary>
    /// 현재 연결된 소스로부터 결정된 실제 데이터 타입입니다.
    /// 연결되지 않았거나 타입을 결정할 수 없으면 null일 수 있습니다. (내부 업데이트용)
    /// </summary>
    [JsonIgnore]
    public Type? CurrentResolvedType
    {
        get => _currentResolvedType;
        private set
        {
            if (_currentResolvedType != value)
            {
                _currentResolvedType = value;
                OnPropertyChanged(nameof(CurrentResolvedType));
                OnPropertyChanged(nameof(DataType)); // DataType도 변경됨을 알림
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
                    _connection?.Disconnect();
                }
                _isVisible = value;
                OnPropertyChanged(nameof(IsVisible));
            }
        }
    }

    public IReadOnlyList<IConnection> Connections => _connection != null ? new[] { _connection } : Array.Empty<IConnection>();
    public INode Node { get; private set; }
    public int GetPortIndex() => _index;

    /// <summary>
    /// 연결된 소스 타입에 기반하여 현재 타입을 업데이트합니다.
    /// </summary>
    private void UpdateResolvedType()
    {
        CurrentResolvedType = ConnectedType; // 직접 연결된 타입을 사용
    }

    /// <summary>
    /// 포트 초기화 로직. GenericInputPort는 기본적으로 할 일이 없습니다.
    /// </summary>
    public virtual void Initialize()
    {
        // 기본 구현은 비어 있음
    }

    /// <summary>
    /// 주어진 소스 타입의 연결을 받을 수 있는지 확인합니다.
    /// GenericInputPort는 일단 모든 연결을 허용하고, 값 처리 시점에서 타입을 확인합니다.
    /// </summary>
    public virtual bool CanAcceptType(Type sourceType)
    {
        return true; // 모든 타입 일단 허용
    }

    public object? Value {
        get {
            if(!IsConnected)
                return null;
            
            if (_connection?.Source is IOutputPort outputPort)
                return outputPort.Value;
            
            return null;
        }
    }

    /// <summary>
    /// 연결된 OutputPort에서 값을 가져와 요청된 타입 T로 변환을 시도합니다.
    /// </summary>
    public T? GetValueOrDefault<T>(T? defaultValue = default)
    {
        if (_connection?.Source is not IOutputPort outputPort || outputPort.Value == null)
            return defaultValue;

        var sourceValue = outputPort.Value;

        // 1. 직접 캐스팅 가능하면 바로 반환
        if (sourceValue is T typedValue)
        {
            return typedValue;
        }

        // 2. 일반적인 타입 변환 시도 (WPFNode.Utilities 확장 메서드 사용)
        if (sourceValue.TryConvertTo<T>(out var convertedValue))
        {
            return convertedValue;
        }

        // 3. 변환 실패 시 기본값 반환
        // TODO: 컬렉션 변환 로직 추가 필요 시 InputPort<T>의 로직 참고
        System.Diagnostics.Debug.WriteLine($"GenericInputPort: 값 변환 실패 ({sourceValue.GetType().Name} -> {typeof(T).Name})");
        return defaultValue;
    }
    
    public object? GetValueOrDefault(Type targetType, object? defaultValue = null)
    {
        if (_connection?.Source is not IOutputPort outputPort || outputPort.Value == null)
            return defaultValue;

        var sourceValue = outputPort.Value;

        // 1. 직접 캐스팅 가능하면 바로 반환
        if (sourceValue.GetType() == targetType)
        {
            return sourceValue;
        }

        // 2. 일반적인 타입 변환 시도 (WPFNode.Utilities 확장 메서드 사용)
        return sourceValue.TryConvertTo(targetType);
    }

    public void AddConnection(IConnection connection)
    {
        if (connection == null)
            throw new ArgumentNullException(nameof(connection));
        if (!connection.Target.Equals(this))
             throw new NodeConnectionException("연결의 타겟 포트가 일치하지 않습니다.", this, connection.Target);

        // 기존 연결이 있으면 제거
        _connection?.Disconnect();

        _connection = connection;
        UpdateResolvedType(); // 연결 시 타입 업데이트
        OnPropertyChanged(nameof(Connections));
        OnPropertyChanged(nameof(IsConnected));
        OnPropertyChanged(nameof(ConnectedType));
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
            UpdateResolvedType(); // 연결 해제 시 타입 업데이트 (null이 될 수 있음)
            OnPropertyChanged(nameof(Connections));
            OnPropertyChanged(nameof(IsConnected));
            OnPropertyChanged(nameof(ConnectedType));
        }
    }

    public IConnection Connect(IOutputPort source)
    {
        if (source == null)
            throw new NodeConnectionException("소스 포트가 null입니다.");

        // CanAcceptType은 항상 true를 반환하므로 별도 검사 불필요
        // if (!CanAcceptType(source.DataType))
        //     throw new NodeConnectionException("타입이 호환되지 않습니다.", source, this);

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

        if (otherPort is IOutputPort outputPort)
        {
            return Connect(outputPort);
        }
        else
        {
            throw new NodeConnectionException("입력 포트는 출력 포트와만 연결할 수 있습니다.");
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

    /// <summary>
    /// 리소스를 해제합니다. (현재는 특별히 해제할 리소스 없음)
    /// </summary>
    public void Dispose()
    {
        // 이벤트 구독 해지 등 필요한 리소스 정리 로직 추가 가능
        GC.SuppressFinalize(this);
    }

    // --- IJsonSerializable 구현 ---
    // GenericInputPort 자체는 Attribute로 정의되므로,
    // 포트 자체의 정의보다는 연결 상태나 내부 상태를 저장할 필요는 적음.
    // NodeBase에서 동적 포트로 관리되지 않으므로 Write/ReadJson은 간단하게.

    public void WriteJson(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteString("Name", Name);
        // Generic 포트임을 명시하거나, DataType (object)을 쓸 수 있음
        writer.WriteString("PortType", nameof(GenericInputPort));
        writer.WriteString("Type", DataType.AssemblyQualifiedName); // 현재 해석된 타입 또는 object
        writer.WriteNumber("Index", GetPortIndex());
        writer.WriteBoolean("IsVisible", IsVisible);
        writer.WriteEndObject();
    }

    public void ReadJson(JsonElement element, JsonSerializerOptions options)
    {
        // Name, IsVisible 등은 NodeBase의 Attribute 처리에서 설정될 수 있음
        // 또는 여기서 명시적으로 읽을 수도 있음
        if (element.TryGetProperty("Name", out var nameElement))
            Name = nameElement.GetString()!;
        if (element.TryGetProperty("IsVisible", out var visibleElement))
            IsVisible = visibleElement.GetBoolean();

        // CurrentResolvedType은 로드 후 연결 복원 시 자동으로 업데이트되므로 여기서 읽지 않음
    }
}
