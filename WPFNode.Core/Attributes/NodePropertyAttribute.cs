namespace WPFNode.Core.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class NodePropertyAttribute : Attribute
{
    public string DisplayName { get; }
    public NodePropertyControlType ControlType { get; }
    public string? Format { get; }

    public NodePropertyAttribute(string displayName, NodePropertyControlType controlType = NodePropertyControlType.Default, string? format = null)
    {
        DisplayName = displayName;
        ControlType = controlType;
        Format = format;
    }
}

public enum NodePropertyControlType
{
    Default,        // 속성 타입에 따라 자동 선택
    TextBox,        // 일반 텍스트 입력
    NumberBox,      // 숫자 입력 (스핀 버튼 포함)
    CheckBox,       // 체크박스
    ComboBox,       // 콤보박스
    ColorPicker,    // 색상 선택기
    FilePicker,     // 파일 선택기
    MultilineText   // 여러 줄 텍스트 입력
} 