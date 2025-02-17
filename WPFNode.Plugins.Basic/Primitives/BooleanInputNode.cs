using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Constants;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Plugins.Basic.Constants;

namespace WPFNode.Plugins.Basic.Primitives;

[NodeName("Boolean Input")]
[NodeCategory("Primitives")]
[NodeDescription("부울 값을 입력받는 노드입니다.")]
[NodeStyle(StyleKeys.Input.Boolean)]
public class BooleanInputNode : InputNodeBase<bool>
{
    public BooleanInputNode(INodeCanvas canvas, Guid id) : base(canvas, id)
    {
        // 기본값 설정
        Value = false;
    }

    [NodeProperty("Value", NodePropertyControlType.CheckBox)]
    public override bool Value
    {
        get => base.Value;
        set => base.Value = value;
    }
} 