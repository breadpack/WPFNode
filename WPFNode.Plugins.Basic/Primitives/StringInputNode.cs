using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Constants;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Plugins.Basic.Constants;

namespace WPFNode.Plugins.Basic.Primitives;

[NodeName("String Input")]
[NodeCategory("Primitives")]
[NodeDescription("문자열을 입력받는 노드입니다.")]
[NodeStyle(StyleKeys.Input.String)]
public class StringInputNode : InputNodeBase<string>
{
    public StringInputNode(INodeCanvas canvas, Guid id) : base(canvas, id)
    {
        // 기본값 설정
        Value = string.Empty;
    }

    [NodeProperty("Value", NodePropertyControlType.TextBox)]
    public override string Value
    {
        get => base.Value;
        set => base.Value = value;
    }
} 