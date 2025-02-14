using System.Threading.Tasks;
using WPFNode.Abstractions;
using WPFNode.Core.Attributes;
using WPFNode.Core.Models;

namespace WPFNode.Plugins.Basic;

[NodeName("곱셈")]
[NodeCategory("기본 연산")]
[NodeDescription("두 수를 곱합니다.")]
public class MultiplicationNode : NodeBase
{
    private readonly InputPort<double> _inputA;
    private readonly InputPort<double> _inputB;
    private readonly OutputPort<double> _output;

    public MultiplicationNode(INodeCanvas canvas) : base(canvas) {
        _inputA = CreateInputPort<double>("A");
        _inputB = CreateInputPort<double>("B");
        _output = CreateOutputPort<double>("결과");
    }

    public override async Task ProcessAsync()
    {
        var a = _inputA.GetValueOrDefault(0.0);
        var b = _inputB.GetValueOrDefault(0.0);
        _output.Value = a * b;
        await Task.CompletedTask;
    }
} 