using System;
using System.Threading;
using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Interfaces.Flow;
using WPFNode.Models;

namespace WPFNode.Plugins.Basic;

[NodeName("For 반복문")]
[NodeCategory("흐름 제어")]
[NodeDescription("지정된 범위만큼 반복합니다.")]
public class ForNode : NodeBase
{
    [FlowInPort("In")]
    public IFlowInPort FlowIn { get; private set; }
    
    [FlowOutPort("Loop Body")]
    public IFlowOutPort LoopBody { get; private set; }
    
    [FlowOutPort("Completed")]
    public IFlowOutPort Completed { get; private set; }
    
    [NodeInput("Start")]
    public InputPort<int> StartInput { get; private set; }
    
    [NodeInput("End")]
    public InputPort<int> EndInput { get; private set; }
    
    [NodeInput("Step")]
    public InputPort<int> StepInput { get; private set; }
    
    [NodeOutput("Current")]
    public OutputPort<int> CurrentOutput { get; private set; }
    
    private int _currentIndex;
    private int _endValue;
    private int _step;
    
    public ForNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
    {
    }
    
    protected override async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        _currentIndex = StartInput.GetValueOrDefault(0);
        _endValue = EndInput.GetValueOrDefault(10);
        _step = StepInput.GetValueOrDefault(1);
        
        // 기본값 검증
        if (_step == 0) _step = 1;
        
        CurrentOutput.Value = _currentIndex;
        
        await Task.CompletedTask;
    }
    
    public override async Task PropagateFlowAsync(WPFNode.Models.Execution.ExecutionContext context, CancellationToken cancellationToken)
    {
        bool continueLoop = (_step > 0) ? 
            _currentIndex < _endValue : 
            _currentIndex > _endValue;
            
        if (continueLoop)
        {
            // 루프 본문으로 흐름 전파
            await LoopBody.PropagateFlowAsync(context, cancellationToken);
            
            // 다음 반복 준비
            _currentIndex += _step;
            CurrentOutput.Value = _currentIndex;
            
            // 다시 자신을 실행
            await FlowIn.ReceiveFlowAsync(context, cancellationToken);
        }
        else
        {
            // 루프 완료 후 다음 단계로 전파
            await Completed.PropagateFlowAsync(context, cancellationToken);
        }
    }
}
