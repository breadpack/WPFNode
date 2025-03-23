using Microsoft.Extensions.Logging;
using WPFNode.Interfaces;
using WPFNode.Exceptions;

namespace WPFNode.Models.Execution;

/// <summary>
/// 노드 실행 계획을 관리하는 클래스
/// </summary>
/// <remarks>
/// 주요 기능:
/// - 노드 간의 의존성 관리
/// - 계층적 실행 구조 (SubExecutionPlan)
/// - 병렬 실행 지원
/// - 루프 노드 처리
/// - 실행 상태 추적
/// - 백프레셔 패턴 지원
/// </remarks>
public class ExecutionPlan {
    private readonly ILogger?                          _logger;
    private readonly IExecutable                       _rootExecutor;

    public ExecutionPlan(
        IEnumerable<NodeBase>    nodes,
        IEnumerable<IConnection> connections,
        bool                     parallelExecution = true,
        ILogger?                 logger            = null
    ) {
        _logger = logger;
        var builder = new ExecutionPlanBuilder(_logger);
        _rootExecutor = builder.BuildExecutionPlanWithEntryPoints(nodes, connections, parallelExecution);
        _logger?.LogDebug("ExecutionPlan initialized with parallelExecution: {Parallel}", parallelExecution);
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default) {
        var context = new ExecutionContext();
        
        // 루트 실행기 실행
        await _rootExecutor.ExecuteAsync(context, cancellationToken);
        
        // 백프레셔 패턴: 예약된 노드가 있으면 실행
        await ProcessScheduledNodesAsync(context, cancellationToken);
        
        _logger?.LogDebug("ExecutionPlan execution completed");
    }
    
    /// <summary>
    /// 백프레셔 패턴: 예약된 노드들을 처리합니다.
    /// </summary>
    private async Task ProcessScheduledNodesAsync(ExecutionContext context, CancellationToken cancellationToken)
    {
        int iterationCount = 0;
        const int maxIterations = 1000; // 무한 루프 방지
        
        while (context.HasScheduledNodes && iterationCount < maxIterations)
        {
            iterationCount++;
            
            // 다음 예약된 노드 가져오기
            var node = context.DequeueScheduledNode();
            if (node == null) break;
            
            _logger?.LogDebug("백프레셔: 예약된 노드 {NodeType} 실행 (반복 {Count})", 
                node.GetType().Name, iterationCount);
            
            try
            {
                // 노드 실행
                await node.ExecuteAsync(cancellationToken);
                context.MarkNodeExecuted(node);
                
                // 이 노드에 의존하는 다른 노드들 확인
                context.CheckAndSchedulePendingNodes(node);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "백프레셔: 예약된 노드 {NodeType} 실행 중 오류 발생", 
                    node.GetType().Name);
                
                if (node is NodeBase nodeBase)
                {
                    context.SetNodeState(nodeBase, NodeExecutionState.Failed);
                }
                
                throw new NodeExecutionException(
                    $"예약된 노드 {node.GetType().Name} 실행 중 오류 발생", ex, node as NodeBase ?? throw new InvalidOperationException());
            }
        }
        
        if (iterationCount >= maxIterations)
        {
            _logger?.LogWarning("백프레셔: 최대 반복 횟수 ({MaxIterations})에 도달했습니다. 순환 의존성이 있을 수 있습니다.", 
                maxIterations);
        }
    }
}