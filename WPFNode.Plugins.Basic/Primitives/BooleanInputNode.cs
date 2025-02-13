using System.Threading.Tasks;
using WPFNode.Abstractions;
using WPFNode.Core.Attributes;
using WPFNode.Core.Models;
using WPFNode.Plugins.Basic.Constants;

namespace WPFNode.Plugins.Basic.Primitives;

[NodeName("Boolean Input")]
[NodeCategory("Primitives")]
[NodeDescription("불리언 값을 입력받는 노드입니다.")]
[NodeStyle(StyleKeys.Input.Boolean)]
public class BooleanInputNode : InputNodeBase<bool>
{
    public BooleanInputNode(INodeCanvas canvas) : base(canvas) { }

    [NodeProperty("Value", NodePropertyControlType.CheckBox)]
    public override bool Value
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
} 