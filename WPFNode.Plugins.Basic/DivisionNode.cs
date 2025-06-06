using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Execution;

namespace WPFNode.Plugins.Basic;

[NodeName("나눗셈")]
[NodeCategory("기본 연산")]
[NodeDescription("첫 번째 수를 두 번째 수로 나눕니다.")]
public class DivisionNode : NodeBase
{
    [NodeFlowIn]
    public FlowInPort FlowIn { get; set; }
    
    [NodeFlowOut]
    public FlowOutPort FlowOut { get; set; }
    
    [NodeInput("A")]
    public InputPort<double>  InputA { get; set; }
    
    [NodeInput("B")]
    public InputPort<double>  InputB { get; set; }
    
    [NodeOutput("결과")]
    public OutputPort<double> Result { get; set; }

    public DivisionNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) {
    }

    public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(IExecutionContext? context, CancellationToken cancellationToken) {
        var a = InputA.GetValueOrDefault(0.0);
        var b = InputB.GetValueOrDefault(1.0); // 0으로 나누는 것을 방지하기 위해 기본값을 1.0으로 설정
        
        if (b == 0.0)
        {
            Result.Value = double.NaN; // 0으로 나누려고 할 경우 NaN을 반환
        }
        else
        {
            Result.Value = a / b;
        }

        yield return FlowOut;
    }
} 