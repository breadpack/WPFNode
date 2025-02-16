using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media;
using WPFNode.Abstractions;
using WPFNode.Abstractions.Attributes;
using WPFNode.Abstractions.Constants;
using WPFNode.Core.Models;
using WPFNode.Core.Models.Properties;

namespace WPFNode.Demo;

[NodeName("테스트 노드")]
[NodeCategory("테스트")]
[NodeDescription("속성 그리드 테스트를 위한 노드입니다.")]
public class TestNode : NodeBase
{
    private string _textValue = string.Empty;
    private double _numberValue;
    private bool _boolValue;
    private string _multilineText = string.Empty;
    private Color _colorValue = Colors.White;
    
    public OutputPort<string> Output { get; }

    public TestNode(INodeCanvas canvas) : base(canvas) {
        Output = CreateOutputPort<string>("출력");

        Initialize();
    }

    public override void Initialize()
    {
        if (IsInitialized) return;
        
        base.Initialize();

        AddProperty(
            "TextValue",
            "텍스트",
            NodePropertyControlType.TextBox,
            () => _textValue,
            value => _textValue = value);

        AddProperty(
            "NumberValue",
            "숫자",
            NodePropertyControlType.NumberBox,
            () => _numberValue,
            value => _numberValue = value,
            "F2");

        AddProperty(
            "BoolValue",
            "참/거짓",
            NodePropertyControlType.CheckBox,
            () => _boolValue,
            value => _boolValue = value);

        AddProperty(
            "MultilineText",
            "여러 줄 텍스트",
            NodePropertyControlType.MultilineText,
            () => _multilineText,
            value => _multilineText = value);

        AddProperty(
            "ColorValue",
            "색상",
            NodePropertyControlType.ColorPicker,
            () => _colorValue,
            value => _colorValue = value);
    }

    public override Task ProcessAsync()
    {
        Output.Value = $"Text: {_textValue}, Number: {_numberValue}, Bool: {_boolValue}, Multiline: {_multilineText}, Color: {_colorValue}";
        return Task.CompletedTask;
    }
} 