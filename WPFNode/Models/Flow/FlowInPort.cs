using System;
using System.Threading;
using System.Threading.Tasks;
using WPFNode.Interfaces;
using WPFNode.Interfaces.Flow;
using WPFNode.Models.Execution;

namespace WPFNode.Models.Flow;

/// <summary>
/// 흐름 입력 포트의 구현 클래스입니다.
/// </summary>
public class FlowInPort : FlowPort, IFlowInPort
{
    /// <summary>
    /// 흐름 입력 포트 생성자
    /// </summary>
    /// <param name="node">포트가 소속된 노드</param>
    /// <param name="name">포트 이름</param>
    public FlowInPort(INode node, string name) : base(node, name)
    {
    }

    /// <inheritdoc />
    public override FlowPortType PortType => FlowPortType.FlowIn;

    /// <inheritdoc />
    public virtual async Task ReceiveFlowAsync(WPFNode.Models.Execution.ExecutionContext context, CancellationToken cancellationToken)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));
            
        // 노드가 FlowNodeBase인 경우에만 실행
        if (Node is FlowNodeBase flowNode)
        {
            // 노드 실행
            await flowNode.ExecuteAsync(cancellationToken);
            
            // 노드 상태 업데이트
            if (Node is NodeBase nodeBase)
            {
                context.SetNodeState(nodeBase, NodeExecutionState.Completed);
            }
            
            // 다음 흐름 전파
            await flowNode.PropagateFlowAsync(context, cancellationToken);
        }
    }
}
