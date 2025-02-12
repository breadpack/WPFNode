using System.Threading.Tasks;
using WPFNode.Core.Attributes;
using WPFNode.Plugins.Basic.Constants;
using WPFNode.Plugins.Basic.Primitives.Base;

namespace WPFNode.Plugins.Basic.Primitives;

[NodeName("Integer Input")]
[NodeCategory("Primitives")]
[NodeDescription("정수 값을 입력받는 노드입니다.")]
[NodeStyle(StyleKeys.Input.Integer)]
public class IntegerInputNode : NumberInputNodeBase<int>
{
    [NodeProperty("Value", NodePropertyControlType.NumberBox)]
    public override int Value
    {
        get => base.Value;
        set
        {
            if (base.Value != value)
            {
                base.Value = value;
                OnPropertyChanged();
            }
        }
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