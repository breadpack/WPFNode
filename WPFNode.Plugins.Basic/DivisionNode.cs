using System.Threading.Tasks;
using WPFNode.Abstractions;
using WPFNode.Abstractions.Attributes;
using WPFNode.Core.Models;

namespace WPFNode.Plugins.Basic;

[NodeName("나눗셈")]
[NodeCategory("기본 연산")]
[NodeDescription("첫 번째 수를 두 번째 수로 나눕니다.")]
public class DivisionNode : NodeBase
{
    private readonly InputPort<double> _inputA;
    private readonly InputPort<double> _inputB;
    private readonly OutputPort<double> _output;
    
    public InputPort<double> InputA => _inputA;
    public InputPort<double> InputB => _inputB;
    public OutputPort<double> Result => _output;

    public DivisionNode(INodeCanvas canvas, Guid id) : base(canvas, id) {
        _inputA = CreateInputPort<double>("A");
        _inputB = CreateInputPort<double>("B");
        _output = CreateOutputPort<double>("결과");
    }

    public override async Task ProcessAsync()
    {
        var a = _inputA.GetValueOrDefault(0.0);
        var b = _inputB.GetValueOrDefault(1.0); // 0으로 나누는 것을 방지하기 위해 기본값을 1.0으로 설정
        
        if (b == 0.0)
        {
            _output.Value = double.NaN; // 0으로 나누려고 할 경우 NaN을 반환
        }
        else
        {
            _output.Value = a / b;
        }
        
        await Task.CompletedTask;
    }
} 