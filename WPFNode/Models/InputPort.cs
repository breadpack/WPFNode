using System.ComponentModel;
using System.Text.Json;
using WPFNode.Interfaces;
using WPFNode.Utilities;
using WPFNode.Exceptions;

namespace WPFNode.Models;

public class InputPort<T> : IInputPort<T>, INotifyPropertyChanged {
    private readonly Dictionary<Type, Func<object, T>> _converters = new();
    private readonly int                               _index;
    private          bool                              _isVisible = true;
    private          IConnection?                      _connection;

    public event PropertyChangedEventHandler? PropertyChanged;

    public InputPort(string name, INode node, int index) {
        Name   = name;
        Node   = node;
        _index = index;
    }

    public PortId Id          => new(Node.Guid, true, Name);
    public string Name        { get; set; }
    public Type   DataType    => typeof(T);
    public bool   IsInput     => true;
    public bool   IsConnected => _connection != null;

    public bool IsVisible {
        get => _isVisible;
        set {
            if (_isVisible != value) {
                if (!value && IsConnected) {
                    _connection?.Disconnect();
                }

                _isVisible = value;
                OnPropertyChanged(nameof(IsVisible));
            }
        }
    }

    public IReadOnlyList<IConnection> Connections    => _connection != null ? new[] { _connection } : Array.Empty<IConnection>();
    public INode                      Node           { get; private set; }
    public int                        GetPortIndex() => _index;

    public void RegisterConverter<TSource>(Func<TSource, T> converter) {
        if (converter == null)
            throw new ArgumentNullException(nameof(converter));

        _converters[typeof(TSource)] = obj => converter((TSource)obj);
    }

    public bool CanAcceptType(Type sourceType) {
        // 1. 컨버터가 등록되어 있으면 변환 가능
        if (_converters.ContainsKey(sourceType))
            return true;

        // 2. 공통 타입 변환 검사 메서드 호출
        return sourceType.CanConvertTo(typeof(T));
    }

    public T? GetValueOrDefault(T? defaultValue = default) {
        if (_connection?.Source is { } outputPort) {
            var sourceValue = outputPort.Value;
            if (sourceValue == null) return defaultValue;

            try {
                // 1. 등록된 커스텀 컨버터 시도
                if (_converters.TryGetValue(sourceValue.GetType(), out var converter)) {
                    return converter(sourceValue);
                }
                
                // 2. 일반적인 타입 변환 시도
                if (sourceValue.TryConvertTo(out T? convertedValue)) {
                    return convertedValue;
                }
                
                // 3. 마지막으로 문자열 변환 시도 (대상 타입이 string이 아닌 경우에만)
                if (typeof(T) != typeof(string) && sourceValue.ToString() is string stringValue) {
                    if (stringValue.TryConvertTo(out convertedValue)) {
                        return convertedValue;
                    }
                }
            }
            catch (Exception ex) {
                // 변환 중 오류가 발생하더라도 기본값을 반환
                System.Diagnostics.Debug.WriteLine($"InputPort 값 변환 중 오류 발생: {ex.Message}");
            }
        }
        return defaultValue;
    }

    public void AddConnection(IConnection connection) {
        if (connection == null)
            throw new ArgumentNullException(nameof(connection));

        // 기존 연결이 있으면 제거
        _connection?.Disconnect();

        _connection = connection;
        OnPropertyChanged(nameof(Connections));
        OnPropertyChanged(nameof(IsConnected));
    }

    public void RemoveConnection(IConnection connection) {
        if (connection == null)
            throw new NodeConnectionException("연결이 null입니다.");
        if (!connection.Target.Equals(this))
            throw new NodeConnectionException("연결의 타겟 포트가 일치하지 않습니다.", this, connection.Target);

        if (_connection == connection) {
            _connection = null;
            OnPropertyChanged(nameof(Connections));
            OnPropertyChanged(nameof(IsConnected));
        }
    }

    public IConnection Connect(IOutputPort source) {
        if (source == null)
            throw new NodeConnectionException("소스 포트가 null입니다.");

        if (!CanAcceptType(source.DataType))
            throw new NodeConnectionException("타입이 호환되지 않습니다.", source, this);

        if (source.Node == Node)
            throw new NodeConnectionException("같은 노드의 포트와는 연결할 수 없습니다.", source, this);

        // 기존 연결이 있으면 삭제
        _connection?.Disconnect();

        // Canvas를 통해 새로운 연결 생성
        var canvas = ((NodeBase)Node!).Canvas;
        return canvas.Connect(source, this);
    }

    public IConnection Connect(IPort otherPort) {
        if (otherPort == null)
            throw new NodeConnectionException("대상 포트가 null입니다.");
            
        if (otherPort is IOutputPort outputPort) {
            return Connect(outputPort);
        }
        else {
            throw new NodeConnectionException("입력 포트는 다른 입력 포트와 연결할 수 없습니다.");
        }
    }

    public void Disconnect() {
        _connection?.Disconnect();
    }

    protected virtual void OnPropertyChanged(string propertyName) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void WriteJson(Utf8JsonWriter writer) {
        writer.WriteStartObject();
        writer.WriteString("Name", Name);
        writer.WriteString("Type", DataType.AssemblyQualifiedName);
        writer.WriteNumber("Index", GetPortIndex());
        writer.WriteBoolean("IsVisible", IsVisible);
        writer.WriteEndObject();
    }

    public void ReadJson(JsonElement element, JsonSerializerOptions options) {
        if (element.TryGetProperty("Name", out var nameElement))
            Name = nameElement.GetString()!;
        if (element.TryGetProperty("IsVisible", out var visibleElement))
            IsVisible = visibleElement.GetBoolean();
    }
}
