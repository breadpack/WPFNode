using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using WPFNode.Exceptions;
using WPFNode.Interfaces;

namespace WPFNode.Models.Execution;

/// <summary>
/// IFlowEntry 노드를 시작점으로 하는 Flow 기반 실행 엔진
/// </summary>
public class FlowExecutionEngine {
    private readonly ILogger? _logger;
    private readonly bool     _enableParallelExecution;

    public FlowExecutionEngine(ILogger? logger = null, bool enableParallelExecution = true) {
        _logger                  = logger;
        _enableParallelExecution = enableParallelExecution;
    }

    /// <summary>
    /// 노드 집합을 실행합니다.
    /// </summary>
    public async Task ExecuteAsync(
        IEnumerable<NodeBase>    nodes,
        IEnumerable<IConnection> connections,
        CancellationToken        cancellationToken = default
    ) {
        var context = new FlowExecutionContext(_logger);

        // 1. IFlowEntry 인터페이스를 구현하는 시작 노드들 찾기
        var entryNodes = nodes.Where(n => n is IFlowEntry).ToList();

        if (entryNodes.Count == 0) {
            _logger?.LogWarning("No IFlowEntry nodes found, execution will not proceed");
            return;
        }

        _logger?.LogInformation("Found {Count} IFlowEntry nodes", entryNodes.Count);

        // 2. 각 시작 노드 실행
        if (_enableParallelExecution && entryNodes.Count > 1) {
            // 병렬 실행
            var executionTasks = entryNodes.Select(node => ExecuteFlowEntryNodeAsync(node, context, cancellationToken));
            await Task.WhenAll(executionTasks);
        }
        else {
            // 순차 실행
            foreach (var entryNode in entryNodes) {
                await ExecuteFlowEntryNodeAsync(entryNode, context, cancellationToken);
            }
        }

        _logger?.LogInformation("Flow execution completed");
    }

    /// <summary>
    /// 단일 FlowEntry 노드와 그 흐름을 실행합니다.
    /// </summary>
    private async Task ExecuteFlowEntryNodeAsync(
        NodeBase             entryNode,
        FlowExecutionContext context,
        CancellationToken    cancellationToken
    ) {
        _logger?.LogDebug("Executing flow entry node: {NodeType}", entryNode.GetType().Name);

        // 노드가 이미 실행되었으면 중복 실행하지 않음
        if (context.IsNodeExecuted(entryNode) && !context.GetNodeExecutionState(entryNode).ShouldReExecute) {
            _logger?.LogDebug("Node {NodeType} already executed, skipping", entryNode.GetType().Name);
            return;
        }

        try {
            // 노드 실행
            await ExecuteNodeAsync(entryNode, context, cancellationToken);
        }
        catch (Exception ex) {
            _logger?.LogError(ex, "Error executing flow entry node {NodeType}", entryNode.GetType().Name);
            throw new NodeExecutionException($"Flow 실행 중 오류 발생: {entryNode.GetType().Name}", ex, entryNode);
        }
    }

    /// <summary>
    /// FlowOutPort와 연결된 모든 노드들을 실행합니다.
    /// </summary>
    private async Task ExecuteFlowOutPortAsync(
        IFlowOutPort         flowOutPort,
        FlowExecutionContext context,
        CancellationToken    cancellationToken
    ) {
        _logger?.LogDebug("Executing flow from out port: {PortName}", flowOutPort.Name);

        // FlowOutPort에 연결된 모든 연결 가져오기
        var connections = flowOutPort.Connections;

        if (connections.Count == 0) {
            _logger?.LogDebug("No connections from flow out port {PortName}", flowOutPort.Name);
            return;
        }

        // FlowInPort로 연결된 노드들 찾기
        var targetNodes = new HashSet<NodeBase>();

        foreach (var connection in connections) {
            if (connection.Target is IFlowInPort flowInPort && flowInPort.Node is NodeBase targetNode) {
                targetNodes.Add(targetNode);
            }
        }

        if (targetNodes.Count == 0) {
            _logger?.LogDebug("No valid target nodes from flow out port {PortName}", flowOutPort.Name);
            return;
        }

        // 연결된 노드들 실행 (병렬 또는 순차적으로)
        if (_enableParallelExecution && targetNodes.Count > 1) {
            // 병렬 실행
            var executionTasks = targetNodes.Select(node => ExecuteNodeFlowAsync(node, context, cancellationToken));
            await Task.WhenAll(executionTasks);
        }
        else {
            // 순차 실행
            foreach (var targetNode in targetNodes) {
                await ExecuteNodeFlowAsync(targetNode, context, cancellationToken);
            }
        }
    }

    /// <summary>
    /// 노드와 그 노드의 FlowOutPort를 실행합니다.
    /// </summary>
    private async Task ExecuteNodeFlowAsync(
        NodeBase             node,
        FlowExecutionContext context,
        CancellationToken    cancellationToken
    ) {
        // 노드가 이미 실행되었는지 확인 - 루프백 신호로 재실행이 요청된 경우는 제외
        var nodeState = context.GetNodeExecutionState(node);
        if (nodeState.IsExecuted && !nodeState.ShouldReExecute) {
            _logger?.LogDebug("Node {NodeType} already executed, skipping", node.GetType().Name);
            return;
        }

        // 재실행 플래그가 설정되어 있었다면 리셋
        if (nodeState.ShouldReExecute) {
            nodeState.ShouldReExecute = false;
        }

        // 노드 실행
        await ExecuteNodeAsync(node, context, cancellationToken);
    }

    /// <summary>
    /// 단일 노드를 실행하고 출력 값을 설정합니다.
    /// </summary>
    private async Task ExecuteNodeAsync(
        NodeBase             node,
        FlowExecutionContext context,
        CancellationToken    cancellationToken
    ) {
        _logger?.LogDebug("Executing node: {NodeType}", node.GetType().Name);

        try {
            // 1. 우선 InputPort에 연결된 노드들 실행하여 입력값 계산
            await CalculateInputsAsync(node, context, cancellationToken);

            // 2. 노드 실행 및 FlowOutPort 순차 처리
            await foreach (var flowOutPort in node.ExecuteAsyncFlow(cancellationToken).ConfigureAwait(false))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                // 특수 제어 신호 처리 (루프백)
                if (flowOutPort.IsLoopBackSignal())
                {
                    _logger?.LogDebug("Loop back signal detected for node {NodeType}", node.GetType().Name);
                    
                    // 노드 상태 조정 (재실행 준비)
                    context.PrepareForReexecution(node);
                    
                    // 루프 반복 횟수 증가
                    context.IncrementLoopIteration(node);
                    
                    // 현재 노드를 다시 실행 (재귀)
                    await ExecuteNodeAsync(node, context, cancellationToken);
                    return; // 중요: 재귀 호출 후 함수 종료
                }
                else
                {
                    // 일반 FlowOutPort 실행
                    await ExecuteFlowOutPortAsync(flowOutPort, context, cancellationToken);
                }
            }
        }
        catch (Exception ex) {
            _logger?.LogError(ex, "Error executing node {NodeType}", node.GetType().Name);
            throw new NodeExecutionException($"노드 실행 중 오류 발생: {node.GetType().Name}", ex, node);
        }
    }

    /// <summary>
    /// 노드의 입력 값을 계산하기 위해 InputPort에 연결된 노드들을 실행합니다.
    /// </summary>
    private async Task CalculateInputsAsync(
        NodeBase             node,
        FlowExecutionContext context,
        CancellationToken    cancellationToken
    ) {
        _logger?.LogDebug("Calculating inputs for node: {NodeType}", node.GetType().Name);

        // InputPort에 연결된 노드들 수집
        var inputNodes = new HashSet<NodeBase>();

        foreach (var inputPort in node.InputPorts) {
            // Flow 포트는 값 계산이 필요없으므로 제외
            if (inputPort is IFlowInPort) continue;

            foreach (var connection in inputPort.Connections) {
                if (connection.Source.Node is NodeBase sourceNode && 
                    (!context.IsNodeExecuted(sourceNode) || context.GetNodeExecutionState(sourceNode).ShouldReExecute)) {
                    inputNodes.Add(sourceNode);
                }
            }
        }

        if (inputNodes.Count == 0) {
            _logger?.LogDebug("No input dependencies for node {NodeType}", node.GetType().Name);
            return;
        }

        // 입력 노드들 실행
        if (_enableParallelExecution && inputNodes.Count > 1) {
            // 병렬 실행
            var executionTasks = inputNodes.Select(n => ExecuteNodeAsync(n, context, cancellationToken));
            await Task.WhenAll(executionTasks);
        }
        else {
            // 순차 실행
            foreach (var inputNode in inputNodes) {
                await ExecuteNodeAsync(inputNode, context, cancellationToken);
            }
        }
    }
}
