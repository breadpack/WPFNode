using Microsoft.Extensions.Logging;

namespace WPFNode.Models.Execution.Executors;

public class SequentialExecutor : IExecutable
{
    private readonly List<IExecutable> _executors = new();
    private readonly ILogger? _logger;

    public SequentialExecutor(ILogger? logger = null)
    {
        _logger = logger;
    }

    public void AddExecutor(IExecutable executor)
    {
        _executors.Add(executor);
    }

    public async Task ExecuteAsync(ExecutionContext context, CancellationToken cancellationToken = default)
    {
        // 각 실행기를 순차적으로 실행
        foreach (var executor in _executors)
        {
            await executor.ExecuteAsync(context, cancellationToken);
            
            // 백프레셔 패턴: 예약된 노드가 있으면 즉시 처리
            await ProcessScheduledNodesAsync(context, cancellationToken);
        }
    }
    
    /// <summary>
    /// 백프레셔 패턴: 예약된 노드들을 처리합니다.
    /// </summary>
    private async Task ProcessScheduledNodesAsync(ExecutionContext context, CancellationToken cancellationToken)
    {
        // 예약된 노드가 있는 경우에만 처리
        if (!context.HasScheduledNodes)
            return;
            
        _logger?.LogDebug("SequentialExecutor: 예약된 노드 처리 시작");
        
        // 현재 예약된 노드들만 처리 (처리 중에 새로 예약된 노드는 다음 반복에서 처리)
        int count = 0;
        while (context.HasScheduledNodes)
        {
            var node = context.DequeueScheduledNode();
            if (node == null) break;
            
            count++;
            _logger?.LogDebug("SequentialExecutor: 예약된 노드 {NodeType} 실행", node.GetType().Name);
            
            // 노드 실행
            await node.ExecuteAsync(cancellationToken);
            context.MarkNodeExecuted(node);
            
            // 이 노드에 의존하는 다른 노드들 확인
            context.CheckAndSchedulePendingNodes(node);
        }
        
        _logger?.LogDebug("SequentialExecutor: {Count}개의 예약된 노드 처리 완료", count);
    }
} 