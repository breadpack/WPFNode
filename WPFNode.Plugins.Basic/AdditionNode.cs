using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;

namespace WPFNode.Plugins.Basic;

[NodeName("덧셈")]
[NodeCategory("기본 연산")]
[NodeDescription("두 수를 더합니다.")]
public class AdditionNode : NodeBase
{
    private readonly InputPort<double> _inputA;
    private readonly InputPort<double> _inputB;
    private readonly OutputPort<double> _output;
    
    public InputPort<double> InputA => _inputA;
    public InputPort<double> InputB => _inputB;
    public OutputPort<double> Result => _output;

    public AdditionNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) {
        _inputA = CreateInputPort<double>("A");
        _inputB = CreateInputPort<double>("B");
        _output = CreateOutputPort<double>("결과");
    }

    protected override async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        var a = _inputA.GetValueOrDefault(0.0);
        var b = _inputB.GetValueOrDefault(0.0);
        _output.Value = a + b;
        await Task.CompletedTask;
    }
} 