using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Constants;
using WPFNode.Interfaces;
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

    protected override void OnIncrement()
    {
        Value++;
    }

    protected override void OnDecrement()
    {
        Value--;
    }
} 