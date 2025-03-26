using System.ComponentModel;
using System.Text.Json;
using WPFNode.Exceptions;
using WPFNode.Interfaces;
using WPFNode.Utilities;
using WPFNode.Models.Serialization;

namespace WPFNode.Models.Properties;

public class NodeProperty<T> : InputPort<T>, INodeProperty {
    private T?      _value;
    private string  _displayName;
    private string? _format;
    private bool    _canConnectToPort;

    public NodeProperty(
        string  name,
        string  displayName,
        INode   node,
        int     portIndex,
        string? format           = null,
        bool    canConnectToPort = false
    ) : base(name, node, portIndex) {
        _displayName     = displayName;
        _format          = format;
        _canConnectToPort = canConnectToPort;
        _isVisible       = canConnectToPort; // 기본적으로 포트 연결이 가능할 때만 보이도록 설정
    }

    // INodeProperty 구현
    public string  DisplayName { get => _displayName; }
    public string? Format      { get => _format; }
    public Type    PropertyType => DataType;
    public Type?   ElementType  => TypeExtensions.GetElementType(PropertyType);
    
    public IInputPort? ConnectedPort     => IsConnected ? this : null;
    public bool        IsConnectedToPort => IsConnected;

    public bool CanConnectToPort {
        get => _canConnectToPort;
        set {
            if (_canConnectToPort != value) {
                // 포트 연결이 비활성화될 때 기존 연결 해제
                if (!value && IsConnected) {
                    Disconnect();
                }

                _canConnectToPort = value;
                IsVisible = value; // IsVisible도 함께 업데이트

                OnPropertyChanged(nameof(CanConnectToPort));
                OnPropertyChanged(nameof(ConnectedPort));
                OnPropertyChanged(nameof(IsConnectedToPort));
            }
        }
    }

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

    // GetValueOrDefault 재정의 - 연결 여부에 따라 값 가져오기
    public override T? GetValueOrDefault(T? defaultValue = default) {
        // 포트로 사용되지 않거나 연결이 없으면 로컬 값 반환
        if (!CanConnectToPort || !IsConnected) {
            return _value ?? defaultValue;
        }

        // 포트로 사용되고 연결이 있으면 부모 메서드(InputPort) 호출
        return base.GetValueOrDefault(defaultValue);
    }

    // CanAcceptType 재정의 - 포트 연결 가능 여부 체크 추가
    public override bool CanAcceptType(Type sourceType) {
        // 포트로 사용되지 않는 경우 연결 불가
        if (!CanConnectToPort)
            return false;

        // 부모 클래스의 CanAcceptType 호출 (InputPort의 로직 활용)
        return base.CanAcceptType(sourceType);
    }

    // JSON 직렬화/역직렬화 구현
    public new void WriteJson(Utf8JsonWriter writer) {
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

    public new void ReadJson(JsonElement element, JsonSerializerOptions options) {
        if (element.TryGetProperty("Value", out var valueElement)) {
            try {
                Value = JsonSerializer.Deserialize<T>(valueElement.GetRawText(), NodeCanvasJsonConverter.SerializerOptions);
            }
            catch {
                Value = default;
            }
        }

        if (element.TryGetProperty("IsVisible", out var isVisibleElement)) {
            IsVisible = isVisibleElement.GetBoolean();
        }

        if (element.TryGetProperty("CanConnectToPort", out var canConnectElement)) {
            CanConnectToPort = canConnectElement.GetBoolean();
        }
        
        // 부모 클래스의 ReadJson 호출
        base.ReadJson(element, options);
    }

    // INodeProperty 추가 인터페이스 메서드
    public void ConnectToPort(IInputPort port) {
        // 이미 자신이 InputPort이므로 구현 불필요
    }
}
