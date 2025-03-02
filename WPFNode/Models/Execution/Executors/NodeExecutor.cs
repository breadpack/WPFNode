using Microsoft.Extensions.Logging;
using WPFNode.Interfaces;

namespace WPFNode.Models.Execution.Executors;

public class NodeExecutor : IExecutable
{
    private readonly INode _node;
    private readonly ILogger? _logger;

    public NodeExecutor(INode node, ILogger? logger = null)
    {
        _node = node;
        _logger = logger;
    }

    public async Task ExecuteAsync(ExecutionContext context, CancellationToken cancellationToken = default)
    {
        // 이미 실행된 노드는 건너뜀
        if (context.IsNodeExecuted(_node))
        {
            _logger?.LogDebug("노드 {NodeType}는 이미 실행되었으므로 건너뜁니다", _node.GetType().Name);
            return;
        }
        
        // 노드를 현재 실행 사이클에 등록
        context.RegisterNodeInCurrentCycle(_node);
        
        // 노드 실행 전에 의존성 확인
        // 모든 의존성 노드가 실행되었는지 확인
        var allDependenciesExecuted = true;
        var dependenciesToCheck = new List<INode>();
        
        // 노드가 NodeBase인 경우 입력 포트를 통해 의존성 확인
        if (_node is NodeBase nodeBase)
        {
            // 노드 상태를 Running으로 설정
            context.SetNodeState(nodeBase, NodeExecutionState.Running);
            
            foreach (var inputPort in nodeBase.InputPorts)
            {
                // 연결된 출력 포트가 있는 경우
                if (inputPort.IsConnected && inputPort.Connections.Count > 0)
                {
                    foreach (var connection in inputPort.Connections)
                    {
                        var sourceNode = connection.Source.Node;
                        if (!context.IsNodeExecuted(sourceNode))
                        {
                            _logger?.LogDebug("노드 {NodeType}의 의존성 {SourceNodeType}가 아직 실행되지 않았습니다", 
                                _node.GetType().Name, sourceNode.GetType().Name);
                            allDependenciesExecuted = false;
                            dependenciesToCheck.Add(sourceNode);
                        }
                    }
                }
            }
        }
        
        // 백프레셔 패턴 적용: 의존성 노드가 실행되지 않았다면 현재 노드 실행을 연기
        if (!allDependenciesExecuted)
        {
            _logger?.LogDebug("노드 {NodeType}의 의존성이 아직 실행되지 않아 실행을 연기합니다", _node.GetType().Name);
            
            // 의존성 노드 목록을 컨텍스트에 저장
            if (_node is NodeBase nb)
            {
                context.AddPendingNode(nb, dependenciesToCheck);
                
                // 노드 상태를 NotStarted로 되돌림 (다시 시도할 수 있도록)
                context.SetNodeState(nb, NodeExecutionState.NotStarted);
            }
            
            return; // 실행을 중단하고 반환
        }
        
        // 현재 노드 실행
        _logger?.LogDebug("노드 {NodeType} 실행 (사이클: {Cycle})", 
            _node.GetType().Name, context.GetCurrentCycle());
        await _node.ExecuteAsync(cancellationToken);
        context.MarkNodeExecuted(_node);
        
        // 이 노드가 실행된 후 대기 중인 노드들을 확인하고 필요한 경우 실행 예약
        if (_node is NodeBase executedNode)
        {
            context.CheckAndSchedulePendingNodes(executedNode);
        }
        
        // 현재 사이클의 모든 노드가 실행 완료되었는지 확인
        int currentCycle = context.GetCurrentCycle();
        if (context.IsCycleCompleted(currentCycle))
        {
            _logger?.LogDebug("사이클 {Cycle}의 모든 노드가 실행 완료되었습니다. 다음 사이클로 진행합니다.", currentCycle);
            context.AdvanceCycle();
        }
    }
} 