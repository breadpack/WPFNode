using System;
using System.Threading;
using System.Threading.Tasks;
using WPFNode.Interfaces;
using WPFNode.Interfaces.Flow;

namespace WPFNode.Models.Flow;

/// <summary>
/// 흐름 출력 포트의 구현 클래스입니다.
/// </summary>
public class FlowOutPort : FlowPort, IFlowOutPort
{
    /// <summary>
    /// 흐름 출력 포트 생성자
    /// </summary>
    /// <param name="node">포트가 소속된 노드</param>
    /// <param name="name">포트 이름</param>
    public FlowOutPort(INode node, string name) : base(node, name)
    {
    }

    /// <inheritdoc />
    public override FlowPortType PortType => FlowPortType.FlowOut;
    
    /// <inheritdoc />
    public override void AddConnection(IFlowConnection connection)
    {
        if (connection == null)
            throw new ArgumentNullException(nameof(connection));

        // 기존 연결이 있는 경우, 새 연결을 추가하기 전에 제거
        if (Connections.Count > 0)
        {
            // 첫 번째 연결 가져오기
            var existingConnection = Connections.FirstOrDefault();
            if (existingConnection != null)
            {
                // NodeCanvas를 통해 연결 제거
                if (Node is NodeBase nodeBase && nodeBase.Canvas is NodeCanvas canvas)
                {
                    canvas.DisconnectFlow(existingConnection);
                }
                else
                {
                    // Canvas가 없는 경우 수동으로 제거
                    RemoveConnection(existingConnection);
                    if (existingConnection.Target is IFlowPort targetPort)
                    {
                        targetPort.RemoveConnection(existingConnection);
                    }
                }
            }
        }

        // 새 연결 추가
        base.AddConnection(connection);
    }

    /// <inheritdoc />
    public virtual async Task PropagateFlowAsync(WPFNode.Models.Execution.ExecutionContext context, CancellationToken cancellationToken)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        // 모든 연결된 입력 포트로 흐름 전파
        foreach (var connection in Connections)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            await connection.Target.ReceiveFlowAsync(context, cancellationToken);
        }
    }
}
