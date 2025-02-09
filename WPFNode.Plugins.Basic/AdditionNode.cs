using System.Threading.Tasks;
using WPFNode.Plugin.SDK;

namespace WPFNode.Plugins.Basic;

[NodeName("덧셈")]
[NodeCategory("기본 연산")]
[NodeDescription("두 수를 더합니다.")]
public class AdditionNode : NodeBase
{
    private readonly InputPort<double> _inputA;
    private readonly InputPort<double> _inputB;
    private readonly OutputPort<double> _output;

    public AdditionNode()
    {
        _inputA = new InputPort<double>("A");
        _inputB = new InputPort<double>("B");
        _output = new OutputPort<double>("결과");
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
        _output.Value = a + b;
        await Task.CompletedTask;
    }
} 