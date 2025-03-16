using System.ComponentModel;
using System.Text.Json;
using System.Collections.Generic;
using WPFNode.Exceptions;
using WPFNode.Interfaces;
using WPFNode.Utilities;
using WPFNode.Models.Serialization;

namespace WPFNode.Models.Properties;

public class NodeProperty<T> : INodeProperty, IInputPort<T> {
    private          T?                _value;
    private readonly INode             _node;
    private readonly List<IConnection> _connections = new();
    private          bool              _canConnectToPort;
    private          bool              _isVisible;
    private          int               _portIndex;

    public event PropertyChangedEventHandler? PropertyChanged;

    public NodeProperty(
        string  name,
        string  displayName,
        INode   node,
        int     portIndex,
        string? format           = null,
        bool    canConnectToPort = false
    ) {
        Name         = name;
        DisplayName  = displayName;
        Format       = format;
        PropertyType = typeof(T);
        ElementType  = GetElementType(PropertyType);

        _node      = node;
        _portIndex = portIndex;

        // CanConnectToPort가 true일 때만 포트가 보이도록 설정
        _canConnectToPort = canConnectToPort;
        _isVisible        = canConnectToPort;
    }

    // INodeProperty 구현
    public string  Name        { get; }
    public string  DisplayName { get; }
    public string? Format      { get; }

    public bool CanConnectToPort {
        get => _canConnectToPort;
        set {
            if (_canConnectToPort != value) {
                // 포트 연결이 비활성화될 때 기존 연결 해제
                if (!value && IsConnected) {
                    Disconnect();
                }

                _canConnectToPort = value;
                _isVisible        = value; // IsVisible도 함께 업데이트

                OnPropertyChanged(nameof(CanConnectToPort));
                OnPropertyChanged(nameof(IsVisible));
                OnPropertyChanged(nameof(ConnectedPort));
                OnPropertyChanged(nameof(IsConnectedToPort));
            }
        }
    }

    public bool IsVisible {
        get => _isVisible;
        set {
            if (_isVisible != value) {
                if (!value && IsConnected) {
                    Disconnect();
                }

                _isVisible = value;
                OnPropertyChanged(nameof(IsVisible));
            }
        }
    }

    public Type                             PropertyType      { get; }
    public Type?                            ElementType       { get; }
    public IInputPort?                      ConnectedPort     => IsConnected ? this : null;
    public bool                             IsConnectedToPort => IsConnected;

    object? INodeProperty.Value {
        get => Value;
        set {
            if (value is T typedValue) {
                Value = typedValue;
            }
        }
    }

    public T? Value {
        get => GetValueOrDefault();
        set {
            if (!Equals(_value, value)) {
                _value = value;
                OnPropertyChanged(nameof(Value));
            }
        }
    }

    public T? GetValueOrDefault(T? defaultValue = default) {
        if (!CanConnectToPort)
            return _value ?? defaultValue;

        if (IsConnected) {
            var connection = Connections.FirstOrDefault();
            if (connection?.Source is { } outputPort) {
                var sourceValue = outputPort.Value;
                if (sourceValue == null) return defaultValue;

                try {
                    // 1. 직접 변환 시도
                    if (TryConvertValue(sourceValue, out T? convertedValue)) {
                        return convertedValue;
                    }

                    // 2. 일반적인 타입 변환 시도
                    if (sourceValue.TryConvertTo(out T? convertedValue2)) {
                        return convertedValue2;
                    }

                    // 3. 마지막으로 문자열 변환 시도 (대상 타입이 string이 아닌 경우에만)
                    if (PropertyType != typeof(string) && sourceValue.ToString() is string stringValue) {
                        if (stringValue.TryConvertTo(out T? stringConvertedValue)) {
                            return stringConvertedValue;
                        }
                    }
                }
                catch (Exception ex) {
                    // 변환 중 오류가 발생하더라도 기본값을 반환
                    System.Diagnostics.Debug.WriteLine($"NodeProperty 값 변환 중 오류 발생: {ex.Message}");
                }
            }
        }

        return _value ?? defaultValue;
    }

    private bool TryConvertValue(object? sourceValue, out T? result) {
        result = default;
        if (sourceValue == null) return false;

        try {
            // TypeConverter를 통해 변환 시도
            var converter = TypeDescriptor.GetConverter(PropertyType);
            if (converter.CanConvertFrom(sourceValue.GetType())) {
                result = (T?)converter.ConvertFrom(sourceValue);
                return true;
            }

            // 소스 타입의 TypeConverter로 변환 시도
            var sourceConverter = TypeDescriptor.GetConverter(sourceValue.GetType());
            if (sourceConverter.CanConvertTo(PropertyType)) {
                result = (T?)sourceConverter.ConvertTo(sourceValue, PropertyType);
                return true;
            }

            // 기본 TryConvertTo 메서드 호출
            return sourceValue.TryConvertTo(out result);
        }
        catch {
            return false;
        }
    }

    // IInputPort 구현
    public PortId Id {
        get {
            if (_node == null)
                throw new InvalidOperationException("노드가 설정되지 않았습니다.");

            return new PortId(_node.Guid, true, _portIndex);
        }
    }

    public int GetPortIndex() => _portIndex;

    public Type                       DataType    => PropertyType;
    public bool                       IsInput     => true;
    public bool                       IsConnected => Connections.Count > 0;
    public IReadOnlyList<IConnection> Connections => _connections;
    public INode                      Node        => _node;

    public void AddConnection(IConnection connection) {
        if (connection == null)
            throw new NodeConnectionException("연결이 null입니다.");
        if (!connection.Target.Equals(this))
            throw new NodeConnectionException("연결의 타겟 포트가 일치하지 않습니다.", connection.Target, this);
        if (!CanConnectToPort)
            throw new NodeConnectionException("이 프로퍼티는 포트 연결을 허용하지 않습니다.");

        _connections.Add(connection);
        OnPropertyChanged(nameof(Connections));
        OnPropertyChanged(nameof(IsConnected));
        OnPropertyChanged(nameof(Value)); // 연결 시 값 업데이트 알림
    }

    public void RemoveConnection(IConnection connection) {
        if (connection == null)
            throw new NodeConnectionException("연결이 null입니다.");
        if (!connection.Target.Equals(this))
            throw new NodeConnectionException("연결의 타겟 포트가 일치하지 않습니다.", connection.Target, this);

        _connections.Remove(connection);
        OnPropertyChanged(nameof(Connections));
        OnPropertyChanged(nameof(IsConnected));
        OnPropertyChanged(nameof(Value)); // 연결 해제 시 값 업데이트 알림
    }

    public bool CanAcceptType(Type type) {
        // 포트로 사용되지 않는 경우 연결 불가
        if (!CanConnectToPort)
            return false;

        if (type == null)
            throw new NodeConnectionException("타입이 null입니다.");

        // 공통 타입 변환 검사 메서드 호출
        return type.CanConvertTo(PropertyType);
    }

    private static Type? GetElementType(Type type) {
        return type.GetElementType();
    }

    protected virtual void OnPropertyChanged(string propertyName) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void WriteJson(Utf8JsonWriter writer) {
        writer.WriteStartObject();
        writer.WriteString("Name", Name);
        writer.WriteString("DisplayName", DisplayName);
        writer.WriteString("Type", PropertyType.AssemblyQualifiedName);
        writer.WriteString("Format", Format);
        writer.WriteBoolean("CanConnectToPort", CanConnectToPort);
        writer.WriteBoolean("IsVisible", IsVisible);

        // 값이 있는 경우에만 직렬화
        if (_value != null || typeof(T).IsValueType) {
            writer.WritePropertyName("Value");
            JsonSerializer.Serialize(writer, _value, typeof(T), NodeCanvasJsonConverter.SerializerOptions);
        }

        writer.WriteEndObject();
    }

    public void ReadJson(JsonElement element, JsonSerializerOptions options) {
        if (element.TryGetProperty("Value", out var valueElement)) {
            Value = JsonSerializer.Deserialize<T>(valueElement.GetRawText(), NodeCanvasJsonConverter.SerializerOptions);
        }

        if (element.TryGetProperty("IsVisible", out var isVisibleElement)) {
            IsVisible = isVisibleElement.GetBoolean();
        }

        if (element.TryGetProperty("CanConnectToPort", out var canConnectElement)) {
            CanConnectToPort = canConnectElement.GetBoolean();
        }
    }

    public void ConnectToPort(IInputPort port) {
        // 이미 자신이 InputPort이므로 구현 불필요
    }

    public void Disconnect() {
        // 연결된 모든 연결 해제
        foreach (var connection in Connections.ToArray()) {
            connection.Disconnect();
        }

        // 값을 로컬 값으로 복원
        if (_connections.Count > 0) {
            Value = _value; // 로컬 값 사용
        }
    }

    public IConnection Connect(IOutputPort source) {
        if (!CanConnectToPort)
            throw new NodeConnectionException("이 프로퍼티는 포트 연결을 허용하지 않습니다.");

        if (source == null)
            throw new NodeConnectionException("소스 포트가 null입니다.");

        if (!CanAcceptType(source.DataType))
            throw new NodeConnectionException("타입이 호환되지 않습니다.", source, this);

        if (source.Node == Node)
            throw new NodeConnectionException("같은 노드의 포트와는 연결할 수 없습니다.", source, this);

        // Canvas를 통해 연결 생성
        var canvas = ((NodeBase)Node!).Canvas;
        return canvas.Connect(source, this);
    }
}