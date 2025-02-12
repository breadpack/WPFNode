using System.Threading.Tasks;
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

    public MultiplicationNode()
    {
        _inputA = new InputPort<double>("A", this);
        _inputB = new InputPort<double>("B", this);
        _output = new OutputPort<double>("결과", this);
    }

    protected override void InitializePorts()
    {
        RegisterInputPort(_inputA);
        RegisterInputPort(_inputB);
        RegisterOutputPort(_output);
    }

    public override async Task ProcessAsync()
    {
        var a = _inputA.GetValueOrDefault(0.0);
        var b = _inputB.GetValueOrDefault(0.0);
        _output.Value = a * b;
        await Task.CompletedTask;
    }
} 