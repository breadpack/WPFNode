using Microsoft.Extensions.Logging;
using WPFNode.Interfaces;

namespace WPFNode.Models.Execution.Executors;

public class LoopExecutor : IExecutable
{
    private readonly ILoopNode _loopNode;
    private readonly ILogger? _logger;
    private readonly List<IExecutable> _bodyExecutors = new();

    public LoopExecutor(ILoopNode loopNode, ILogger? logger = null)
    {
        _loopNode = loopNode;
        _logger = logger;
    }

    public void AddBodyExecutor(IExecutable executor)
    {
        _bodyExecutors.Add(executor);
    }

    public async Task ExecuteAsync(ExecutionContext context, CancellationToken cancellationToken = default)
    {
        // 이미 실행된 노드는 건너뜀
        if (context.IsNodeExecuted(_loopNode))
        {
            _logger?.LogDebug("루프 노드 {NodeType}는 이미 실행되었으므로 건너뜁니다", _loopNode.GetType().Name);
            return;
        }

        _logger?.LogDebug("루프 노드 {NodeType} 실행 시작", _loopNode.GetType().Name);

        // 리셋 가능한 노드는 초기화
        if (_loopNode is IResettable resettable)
        {
            _logger?.LogDebug("루프 노드 {NodeType} 초기화", _loopNode.GetType().Name);
            resettable.Reset();
        }

        // 루프 실행을 위한 누적 컨텍스트 생성
        var accumulatedContext = new ExecutionContext();
        int iterationCount = 0;

        // 루프 실행
        while (await _loopNode.ShouldContinueAsync(cancellationToken))
        {
            iterationCount++;
            _logger?.LogDebug("루프 노드 {NodeType} 반복 {Count} 시작 (사이클: {Cycle})", 
                _loopNode.GetType().Name, iterationCount, context.GetCurrentCycle());
            
            cancellationToken.ThrowIfCancellationRequested();

            // 루프 노드 실행
            await _loopNode.ExecuteAsync(cancellationToken);

            // 현재 반복을 위한 컨텍스트 생성
            var iterationContext = new ExecutionContext();
            iterationContext.MarkNodeExecuted(_loopNode);
            
            // 루프 바디 실행
            foreach (var executor in _bodyExecutors)
            {
                await executor.ExecuteAsync(iterationContext, cancellationToken);
                
                // 백프레셔 패턴: 각 실행기 실행 후 예약된 노드가 있으면 처리
                await ProcessScheduledNodesAsync(iterationContext, cancellationToken);
            }

            // 현재 반복의 실행 상태와 데이터 상태를 누적 컨텍스트에 병합
            accumulatedContext.MergeExecutionState(iterationContext);
            
            // 루프 노드의 상태 확인
            if (_loopNode.IsLoopCompleted)
            {
                _logger?.LogDebug("루프 노드 {NodeType}가 완료됨 (IsLoopCompleted = true)", _loopNode.GetType().Name);
                break;
            }
            
            // 사이클 진행
            context.AdvanceCycle();
        }

        _logger?.LogDebug("루프 노드 {NodeType} 총 {Count}회 반복 완료", _loopNode.GetType().Name, iterationCount);

        // 루프가 완료된 후 누적된 실행 상태를 부모 컨텍스트에 병합
        context.MergeExecutionState(accumulatedContext);

        // 루프가 완료된 후에만 실행 완료로 표시
        context.MarkNodeExecuted(_loopNode);
        
        // 백프레셔 패턴: 루프 완료 후 예약된 노드가 있으면 처리
        await ProcessScheduledNodesAsync(context, cancellationToken);
    }
    
    /// <summary>
    /// 백프레셔 패턴: 예약된 노드들을 처리합니다.
    /// </summary>
    private async Task ProcessScheduledNodesAsync(ExecutionContext context, CancellationToken cancellationToken)
    {
        // 예약된 노드가 있는 경우에만 처리
        if (!context.HasScheduledNodes)
            return;
            
        _logger?.LogDebug("LoopExecutor: 예약된 노드 처리 시작");
        
        // 현재 예약된 노드들만 처리 (처리 중에 새로 예약된 노드는 다음 반복에서 처리)
        int count = 0;
        while (context.HasScheduledNodes)
        {
            var node = context.DequeueScheduledNode();
            if (node == null) break;
            
            count++;
            _logger?.LogDebug("LoopExecutor: 예약된 노드 {NodeType} 실행", node.GetType().Name);
            
            // 노드 실행
            await node.ExecuteAsync(cancellationToken);
            context.MarkNodeExecuted(node);
            
            // 이 노드에 의존하는 다른 노드들 확인
            context.CheckAndSchedulePendingNodes(node);
        }
        
        _logger?.LogDebug("LoopExecutor: {Count}개의 예약된 노드 처리 완료", count);
    }
} 