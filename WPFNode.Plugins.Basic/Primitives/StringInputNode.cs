using System.Threading.Tasks;
using WPFNode.Abstractions;
using WPFNode.Abstractions.Attributes;
using WPFNode.Abstractions.Constants;
using WPFNode.Core.Models;
using WPFNode.Plugins.Basic.Constants;

namespace WPFNode.Plugins.Basic.Primitives;

[NodeName("String Input")]
[NodeCategory("Primitives")]
[NodeDescription("문자열을 입력받는 노드입니다.")]
[NodeStyle(StyleKeys.Input.String)]
public class StringInputNode : InputNodeBase<string>
{
    public StringInputNode(INodeCanvas canvas) : base(canvas) => _value = string.Empty;

    [NodeProperty("Value", NodePropertyControlType.TextBox)]
    public override string Value
    {
        get => base.Value;
        set => base.Value = value;
    }

    public override Task ProcessAsync()
    {
        _output.Value = _value;
        return Task.CompletedTask;
    }
} 