using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Execution;

namespace WPFNode.Plugins.Basic;

[NodeName("뺄셈")]
[NodeCategory("기본 연산")]
[NodeDescription("첫 번째 수에서 두 번째 수를 뺍니다.")]
public class SubtractionNode : NodeBase
{
    [NodeFlowIn]
    public IFlowInPort FlowIn { get; set; }
    
    [NodeFlowOut]
    public IFlowOutPort FlowOut { get; set; }
    
    [NodeInput("A")]
    public InputPort<double>  InputA { get; set; }
    
    [NodeInput("B")]
    public InputPort<double>  InputB { get; set; }
    
    [NodeOutput("Output")]
    public OutputPort<double> Result { get; set; }

    public SubtractionNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) {
    }

    protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(FlowExecutionContext? context, CancellationToken cancellationToken) {
        var a = InputA.GetValueOrDefault(0.0);
        var b = InputB.GetValueOrDefault(0.0);
        Result.Value = a - b;
        yield return FlowOut;
    }
} 