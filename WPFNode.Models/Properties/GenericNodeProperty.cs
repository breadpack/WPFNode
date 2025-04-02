using System;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Utilities;
using TypeExtensions = WPFNode.Utilities.TypeExtensions; // For TypeExtensions if needed

namespace WPFNode.Models.Properties;

/// <summary>
/// 연결된 OutputPort의 타입에 따라 자신의 타입을 동적으로 결정하면서,
/// 노드의 속성으로 작동하는 프로퍼티입니다.
/// </summary>
public class GenericNodeProperty : GenericInputPort, INodeProperty
{
    private object? _internalValue;
    private string _displayName = string.Empty; // Initialize with empty string
    private string? _format;
    private bool _canConnectToPort;

    // 생성자는 NodeBase에서 리플렉션으로 호출될 수 있도록 기본 매개변수를 가집니다.
    // 실제 초기화는 Initialize 메서드에서 수행됩니다.
    public GenericNodeProperty(string name, INode node, int index)
        : base(name, node, index)
    {
        // 기본 생성자에서는 최소한의 초기화만 수행
        _displayName = name; // 기본값으로 포트 이름 사용
    }

    #region INodeProperty Implementation

    public string DisplayName => _displayName;
    public string? Format => _format;

    // PropertyType은 GenericInputPort의 DataType을 그대로 사용 (동적 타입)
    public Type PropertyType => base.DataType;

    // ElementType은 필요 시 TypeExtensions를 사용하여 구현 가능
    public Type? ElementType => TypeExtensions.GetElementType(PropertyType);

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
                    Disconnect();
                }

                _canConnectToPort = value;
                IsVisible = value; // IsVisible도 함께 업데이트 (GenericInputPort의 속성)

                OnPropertyChanged(nameof(CanConnectToPort));
                // NodeProperty<T>에는 ConnectedPort, IsConnectedToPort 알림이 있었지만,
                // GenericInputPort 기반이므로 IsConnected, Connections 변경 알림으로 충분할 수 있음.
                // 필요 시 추가.
            }
        }
    }

    // IsConnectedToPort, ConnectedPort는 GenericInputPort의 IsConnected, Connections로 대체 가능
    // 명시적으로 구현해야 한다면 아래와 같이 추가
    // public bool IsConnectedToPort => base.IsConnected;
    // public IInputPort? ConnectedPort => base.IsConnected ? this : null;


    public new object? Value // 'new' 키워드로 GenericInputPort의 Value 숨김
    {
        get
        {
            // 포트로 사용되고 연결되어 있으면 연결된 소스 값 반환 (GenericInputPort 로직)
            if (CanConnectToPort && IsConnected)
            {
                return base.Value; // GenericInputPort의 Value getter 호출
            }
            // 그렇지 않으면 내부 값 반환
            return _internalValue;
        }
        set
        {
            // 포트로 사용되고 연결되어 있을 때는 값을 직접 설정할 수 없음 (소스에서 받아옴)
            if (CanConnectToPort && IsConnected)
            {
                // 필요하다면 경고 로깅 또는 예외 발생
                System.Diagnostics.Debug.WriteLine("경고: 연결된 GenericNodeProperty의 값은 직접 설정할 수 없습니다.");
                return;
            }

            // 내부 값 설정
            if (!Equals(_internalValue, value))
            {
                _internalValue = value;
                OnPropertyChanged(nameof(Value)); // INotifyPropertyChanged 이벤트 발생
            }
        }
    }

    // INode 인터페이스 멤버는 GenericInputPort에서 이미 구현됨 (Node 속성)
    // public INode Node => base.Node; (명시적으로 필요하면 추가)

    /// <summary>
    /// Attribute 및 노드 정보를 기반으로 프로퍼티를 초기화합니다.
    /// </summary>
    public virtual void Initialize(INode node, NodePropertyAttribute attribute, MemberInfo memberInfo)
    {
        _displayName = attribute.DisplayName ?? Name; // Attribute의 DisplayName 우선 사용
        _format = attribute.Format;
        CanConnectToPort = attribute.CanConnectToPort; // CanConnectToPort 설정 (setter 호출)

        // OnValueChanged 콜백 설정
        if (!string.IsNullOrEmpty(attribute.OnValueChanged))
        {
            var method = node.GetType().GetMethod(attribute.OnValueChanged,
                                                  BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (method != null && method.GetParameters().Length == 0)
            {
                PropertyChanged += (s, e) => {
                    if (e.PropertyName == nameof(Value))
                    {
                        try
                        {
                            method.Invoke(node, null);
                        }
                        catch (Exception ex)
                        {
                            // 로깅 필요
                            System.Diagnostics.Debug.WriteLine($"Error invoking OnValueChanged callback '{attribute.OnValueChanged}': {ex.Message}");
                        }
                    }
                };
            }
            else
            {
                 System.Diagnostics.Debug.WriteLine($"Warning: OnValueChanged callback method '{attribute.OnValueChanged}' not found or signature mismatch in node '{node.GetType().Name}'. Expected 'void MethodName()'.");
            }
        }

        // ConnectionStateChangedCallback 설정은 NodeBase.HandleInputPortRegistration에서 중앙 처리하므로 여기서는 제거.
        // if (CanConnectToPort && !string.IsNullOrEmpty(attribute.ConnectionStateChangedCallback))
        // {
        //     // ... (NodeBase에서 처리) ...
        // }
    }

    #endregion

    #region Overrides from GenericInputPort

    /// <summary>
    /// 포트 연결 가능 여부를 확인합니다. CanConnectToPort가 true일 때만 연결을 허용합니다.
    /// </summary>
    public override bool CanAcceptType(Type sourceType)
    {
        return CanConnectToPort && base.CanAcceptType(sourceType); // 항상 true 반환
    }

    // WriteJson/ReadJson은 GenericInputPort의 것을 사용하되,
    // 추가적인 상태(DisplayName, Format, CanConnectToPort)는 NodeBase에서 처리될 것으로 예상.
    // 만약 NodeBase의 WritePropertyValues/ReadPropertyValues에서 처리되지 않는다면 여기서 추가해야 함.
    // 현재 계획은 NodeBase에서 처리하는 것이므로 여기서는 base 호출만으로 충분할 수 있음.
    // (단, GenericInputPort의 WriteJson/ReadJson이 필요한 정보를 모두 저장/복원하는지 확인 필요)

    // 예시: 만약 GenericInputPort의 WriteJson이 DisplayName 등을 저장하지 않는다면...
    // public override void WriteJson(Utf8JsonWriter writer)
    // {
    //     base.WriteJson(writer); // 기본 포트 정보 저장
    //     // 추가 정보 저장 (NodeBase에서 처리 안 할 경우)
    //     // writer.WriteString("DisplayName", DisplayName);
    //     // writer.WriteString("Format", Format);
    //     // writer.WriteBoolean("CanConnectToPort", CanConnectToPort);
    // }
    // public override void ReadJson(JsonElement element, JsonSerializerOptions options)
    // {
    //     base.ReadJson(element, options); // 기본 포트 정보 복원
    //     // 추가 정보 복원 (NodeBase에서 처리 안 할 경우)
    //     // if (element.TryGetProperty("DisplayName", out var dn)) _displayName = dn.GetString() ?? Name;
    //     // if (element.TryGetProperty("Format", out var fmt)) _format = fmt.GetString();
    //     // if (element.TryGetProperty("CanConnectToPort", out var ccp)) CanConnectToPort = ccp.GetBoolean();
    // }

    #endregion
}
