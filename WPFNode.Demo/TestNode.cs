using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media;
using WPFNode.Attributes;
using WPFNode.Constants;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Properties;

namespace WPFNode.Demo;

[NodeName("테스트 노드")]
[NodeCategory("테스트")]
[NodeDescription("속성 그리드 테스트를 위한 노드입니다.")]
public class TestNode : NodeBase {
    public OutputPort<string> Output { get; }

    public TestNode(INodeCanvas canvas, Guid id) : base(canvas, id) {
        Output = CreateOutputPort<string>("출력");

        TextProperty = CreateProperty<string>(
            "TextValue",
            "텍스트");

        NumberProperty = CreateProperty<double>(
            "NumberValue",
            "숫자",
            "F2");

        BooleanProperty = CreateProperty<bool>(
            "BoolValue",
            "참/거짓");

        MultilineTextProperty = CreateProperty<string>(
            "MultilineText",
            "여러 줄 텍스트");

        ColorProperty = CreateProperty<Color>(
            "ColorValue",
            "색상");
    }

    public NodeProperty<Color> ColorProperty { get; }

    public NodeProperty<string> MultilineTextProperty { get; }

    public NodeProperty<bool> BooleanProperty { get; }

    public NodeProperty<double> NumberProperty { get; }

    public NodeProperty<string> TextProperty { get; }

    public override Task ProcessAsync() {
        Output.Value = $"Text: {TextProperty.Value}, Number: {NumberProperty.Value}, Bool: {BooleanProperty.Value}, Multiline: {MultilineTextProperty.Value}, Color: {ColorProperty.Value}";
        return Task.CompletedTask;
    }
}