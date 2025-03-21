using System;
using System.Threading;
using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Interfaces.Flow;
using WPFNode.Models.Execution;

namespace WPFNode.Models.Flow;

/// <summary>
/// 조건에 따라 흐름을 두 방향으로 분기하는 노드입니다.
/// </summary>
public class IfNode : NodeBase
{
    /// <summary>
    /// 조건 입력 포트 - Boolean 값을 입력받습니다.
    /// </summary>
    [NodeInput("Condition")]
    public InputPort<bool> Condition { get; private set; }
    
    /// <summary>
    /// 흐름 입력 포트
    /// </summary>
    [FlowInPort("Execute")]
    public IFlowInPort Execute { get; private set; }
    
    /// <summary>
    /// 조건이 참일 때 실행되는 흐름 출력 포트
    /// </summary>
    [FlowOutPort("True")]
    public IFlowOutPort TrueFlow { get; private set; }
    
    /// <summary>
    /// 조건이 거짓일 때 실행되는 흐름 출력 포트
    /// </summary>
    [FlowOutPort("False")]
    public IFlowOutPort FalseFlow { get; private set; }
    
    /// <summary>
    /// IfNode 생성자
    /// </summary>
    /// <param name="canvas">노드 캔버스</param>
    /// <param name="id">노드 ID</param>
    public IfNode(INodeCanvas canvas, Guid id) : base(canvas, id)
    {
        // 초기화는 NodeBase에서 자동으로 수행됨
    }
    
    /// <summary>
    /// 노드의 처리 로직 구현
    /// </summary>
    protected override async Task ProcessAsync(CancellationToken cancellationToken)
    {
        // 여기서는 조건 값을 계산하거나 필요한 전처리를 수행할 수 있음
        // 실제 분기 처리는 PropagateFlowAsync에서 수행
        await Task.CompletedTask;
    }
    
    /// <summary>
    /// 조건에 따라 흐름 전파
    /// </summary>
    public override async Task PropagateFlowAsync(WPFNode.Models.Execution.ExecutionContext context, CancellationToken cancellationToken)
    {
        // 조건 값 가져오기
        bool condition = Condition.GetValueOrDefault(false);
        
        // 조건에 따라 적절한 포트로 흐름 전파
        if (condition)
        {
            await TrueFlow.PropagateFlowAsync(context, cancellationToken);
        }
        else
        {
            await FalseFlow.PropagateFlowAsync(context, cancellationToken);
        }
    }
}
