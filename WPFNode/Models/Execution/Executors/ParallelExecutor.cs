using Microsoft.Extensions.Logging;

namespace WPFNode.Models.Execution.Executors;

public class ParallelExecutor : IExecutable
{
    private readonly IEnumerable<IExecutable> _executors;
    private readonly ILogger? _logger;

    public ParallelExecutor(IEnumerable<IExecutable> executors, ILogger? logger = null)
    {
        _executors = executors;
        _logger = logger;
    }

    public async Task ExecuteAsync(ExecutionContext context, CancellationToken cancellationToken = default)
    {
        var tasks = _executors.Select(e => e.ExecuteAsync(context, cancellationToken));
        await Task.WhenAll(tasks);
        
        await ProcessScheduledNodesAsync(context, cancellationToken);
    }
    
    private async Task ProcessScheduledNodesAsync(ExecutionContext context, CancellationToken cancellationToken)
    {
        if (!context.HasScheduledNodes)
            return;
            
        _logger?.LogDebug("ParallelExecutor: 예약된 노드 처리 시작");
        
        int count = 0;
        while (context.HasScheduledNodes)
        {
            var node = context.DequeueScheduledNode();
            if (node == null) break;
            
            count++;
            _logger?.LogDebug("ParallelExecutor: 예약된 노드 {NodeType} 실행", node.GetType().Name);
            
            await node.ExecuteAsync(cancellationToken);
            context.MarkNodeExecuted(node);
            
            context.CheckAndSchedulePendingNodes(node);
        }
        
        _logger?.LogDebug("ParallelExecutor: {Count}개의 예약된 노드 처리 완료", count);
    }
} 