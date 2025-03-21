using System;
using System.Threading;
using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Interfaces.Flow;
using WPFNode.Models.Execution;

namespace WPFNode.Models.Flow;

/// <summary>
/// 흐름 실행의 시작점 역할을 하는 노드입니다.
/// </summary>
[NodeCategory("Flow Control")]
[NodeDescription("흐름 실행의 시작점입니다.")]
public class StartNode : NodeBase, IFlowEntryPoint
{
    /// <summary>
    /// 실행 흐름 출력 포트
    /// </summary>
    [FlowOutPort("Start")]
    public IFlowOutPort StartOutput { get; private set; }

    /// <summary>
    /// StartNode 생성자
    /// </summary>
    /// <param name="canvas">노드 캔버스</param>
    /// <param name="id">노드 ID</param>
    public StartNode(INodeCanvas canvas, Guid id) : base(canvas, id)
    {
        // 초기화는 NodeBase에서 자동으로 수행됨
    }
    
    /// <summary>
    /// 노드의 처리 로직 구현
    /// </summary>
    protected override async Task ProcessAsync(CancellationToken cancellationToken)
    {
        // 시작 노드에서 필요한 초기화 작업 수행
        await Task.CompletedTask;
    }
    
    /// <summary>
    /// 다음 노드로 흐름 전파
    /// </summary>
    public override async Task PropagateFlowAsync(WPFNode.Models.Execution.ExecutionContext context, CancellationToken cancellationToken)
    {
        // 시작 노드에서 다음 노드로 실행 흐름 전파
        await StartOutput.PropagateFlowAsync(context, cancellationToken);
    }
}
