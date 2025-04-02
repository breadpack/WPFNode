using System;

namespace WPFNode.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class NodeInputAttribute : Attribute
{
    public string? DisplayName { get; }
    public string? Format { get; set; }
    public bool CanConnectToPort { get; set; } = true;
    /// <summary>
    /// 이 InputPort의 연결 상태가 변경될 때 호출될 노드 메서드의 이름입니다.
    /// 메서드 시그니처는 'void MethodName(IInputPort port)' 이어야 합니다.
    /// </summary>
    public string? ConnectionStateChangedCallback { get; set; }

    // SourcePortNameForType 속성 제거됨

    public NodeInputAttribute(string? displayName = null)
    {
        DisplayName = displayName;
    }
}
