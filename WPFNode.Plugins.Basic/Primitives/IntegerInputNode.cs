using System.Threading.Tasks;
using WPFNode.Abstractions;
using WPFNode.Abstractions.Attributes;
using WPFNode.Abstractions.Constants;
using WPFNode.Core.Models;
using WPFNode.Plugins.Basic.Constants;
using WPFNode.Plugins.Basic.Primitives.Base;

namespace WPFNode.Plugins.Basic.Primitives;

[NodeName("Integer Input")]
[NodeCategory("Primitives")]
[NodeDescription("정수 값을 입력받는 노드입니다.")]
[NodeStyle(StyleKeys.Input.Integer)]
public class IntegerInputNode : NumberInputNodeBase<int>
{
    public IntegerInputNode(INodeCanvas canvas, Guid id) : base(canvas, id)
    {
        // 기본값 설정
        Value = 0;
    }

    [NodeProperty("Value", NodePropertyControlType.NumberBox)]
    public override int Value
    {
        get => base.Value;
        set => base.Value = value;
    }

    protected override void OnIncrement()
    {
        Value++;
    }

    protected override void OnDecrement()
    {
        Value--;
    }
} 