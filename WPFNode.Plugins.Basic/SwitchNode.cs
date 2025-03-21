using System;
using System.Threading;
using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Interfaces.Flow;
using WPFNode.Models;

namespace WPFNode.Plugins.Basic;

[NodeName("Switch 분기")]
[NodeCategory("흐름 제어")]
[NodeDescription("입력 값에 따라 다른 경로로 실행을 전파합니다.")]
public class SwitchNode : NodeBase
{
    [FlowInPort("In")]
    public IFlowInPort FlowIn { get; private set; }
    
    [FlowOutPort("Case 1")]
    public IFlowOutPort Case1 { get; private set; }
    
    [FlowOutPort("Case 2")]
    public IFlowOutPort Case2 { get; private set; }
    
    [FlowOutPort("Case 3")]
    public IFlowOutPort Case3 { get; private set; }
    
    [FlowOutPort("Default")]
    public IFlowOutPort Default { get; private set; }
    
    [NodeInput("Value")]
    public InputPort<int> ValueInput { get; private set; }
    
    [NodeInput("Case 1 Value")]
    public InputPort<int> Case1Input { get; private set; }
    
    [NodeInput("Case 2 Value")]
    public InputPort<int> Case2Input { get; private set; }
    
    [NodeInput("Case 3 Value")]
    public InputPort<int> Case3Input { get; private set; }
    
    public SwitchNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
    {
    }
    
    protected override async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        // 값 계산만 수행
        await Task.CompletedTask;
    }
    
    public override async Task PropagateFlowAsync(WPFNode.Models.Execution.ExecutionContext context, CancellationToken cancellationToken)
    {
        int value = ValueInput.GetValueOrDefault(0);
        
        if (value == Case1Input.GetValueOrDefault(1))
        {
            await Case1.PropagateFlowAsync(context, cancellationToken);
        }
        else if (value == Case2Input.GetValueOrDefault(2))
        {
            await Case2.PropagateFlowAsync(context, cancellationToken);
        }
        else if (value == Case3Input.GetValueOrDefault(3))
        {
            await Case3.PropagateFlowAsync(context, cancellationToken);
        }
        else
        {
            await Default.PropagateFlowAsync(context, cancellationToken);
        }
    }
}
