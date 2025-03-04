using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Constants;
using WPFNode.Interfaces;
using WPFNode.Plugins.Basic.Constants;
using WPFNode.Plugins.Basic.Primitives.Base;

namespace WPFNode.Plugins.Basic.Primitives;

[NodeName("Double Input")]
[NodeCategory("Primitives")]
[NodeDescription("실수 값을 입력받는 노드입니다.")]
[NodeStyle(StyleKeys.Input.Double)]
public class DoubleInputNode : NumberInputNodeBase<double>
{
    public DoubleInputNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
    {
        // 기본값 설정
        Value = 0.0;
    }

    protected override void OnIncrement()
    {
        Value += 1.0;
    }

    protected override void OnDecrement()
    {
        Value -= 1.0;
    }
} 