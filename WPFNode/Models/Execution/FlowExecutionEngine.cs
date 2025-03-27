using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using WPFNode.Exceptions;
using WPFNode.Interfaces;

namespace WPFNode.Models.Execution;

/// <summary>
/// 노드 실행 작업의 유형을 나타냅니다.
/// </summary>
public enum ExecutionTaskType
{
    /// <summary>실행 진입점 노드</summary>
    EntryNode,
    /// <summary>일반 노드</summary>
    Node,
    /// <summary>FlowOutPort</summary>
    FlowOutPort,
    /// <summary>입력 계산</summary>
    CalculateInputs
}

/// <summary>
/// 단일 실행 작업을 나타내는 클래스입니다.
/// </summary>
public class ExecutionTask
{
    /// <summary>작업 유형</summary>
    public ExecutionTaskType TaskType { get; }
    
    /// <summary>실행할 노드</summary>
    public NodeBase? Node { get; }
    
    /// <summary>실행할 출력 포트</summary>
    public IFlowOutPort? FlowOutPort { get; }
    
    /// <summary>작업의 경로 깊이</summary>
    public int PathLength { get; }
    
    public ExecutionTask(ExecutionTaskType taskType, NodeBase? node, IFlowOutPort? flowOutPort, int pathLength)
    {
        TaskType = taskType;
        Node = node;
        FlowOutPort = flowOutPort;
        PathLength = pathLength;
    }
    
    /// <summary>노드 식별자를 반환합니다.</summary>
    public string GetId()
    {
        if (Node != null)
            return $"{Node.GetType().Name}_{Node.Guid}";
        else if (FlowOutPort != null)
            return $"Port_{FlowOutPort.Name}";
        else
            return "Unknown";
    }
}

/// <summary>
/// IFlowEntry 노드를 시작점으로 하는 Flow 기반 실행 엔진
/// </summary>
public class FlowExecutionEngine {
    private readonly ILogger? _logger;
    private readonly bool     _enableParallelExecution;

    // 실행 제한을 위한 설정
    private readonly int _maxPathLength = 100;  // 최대 경로 길이 제한
    private readonly HashSet<string> _visitedNodesInCurrentPath = new();  // 현재 실행 경로에서 방문한 노드들

    public FlowExecutionEngine(ILogger? logger = null, bool enableParallelExecution = true) {
        _logger = logger;
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
        _visitedNodesInCurrentPath.Clear();  // 경로 추적 초기화

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
            var executionTasks = entryNodes.Select(node => ExecuteNodeGraphAsync(node, context, cancellationToken));
            await Task.WhenAll(executionTasks);
        }
        else {
            // 순차 실행
            foreach (var entryNode in entryNodes) {
                await ExecuteNodeGraphAsync(entryNode, context, cancellationToken);
            }
        }

        _logger?.LogInformation("Flow execution completed");
        _visitedNodesInCurrentPath.Clear();  // 정리
    }

    /// <summary>
    /// 노드 그래프를 반복적(iterative) 방식으로 실행합니다.
    /// </summary>
    private async Task ExecuteNodeGraphAsync(
        NodeBase             entryNode,
        FlowExecutionContext context,
        CancellationToken    cancellationToken
    ) {
        _logger?.LogDebug("Starting execution from entry node: {NodeType}", entryNode.GetType().Name);
        
        // 작업 큐 초기화
        var taskQueue = new Queue<ExecutionTask>();
        
        // 시작 노드를 큐에 추가
        taskQueue.Enqueue(new ExecutionTask(ExecutionTaskType.EntryNode, entryNode, null, 0));
        
        // 모든 작업이 처리될 때까지 반복
        while (taskQueue.Count > 0 && !cancellationToken.IsCancellationRequested)
        {
            // 다음 작업 가져오기
            var task = taskQueue.Dequeue();
            
            // 경로 길이 제한 확인
            if (task.PathLength > _maxPathLength)
            {
                _logger?.LogError("Maximum path length exceeded at {Id}", task.GetId());
                continue;
            }
            
            try
            {
                // 작업 유형에 따라 처리
                switch (task.TaskType)
                {
                    case ExecutionTaskType.EntryNode:
                    case ExecutionTaskType.Node:
                        await ProcessNodeTask(task.Node!, context, task.PathLength, taskQueue, cancellationToken);
                        break;
                        
                    case ExecutionTaskType.FlowOutPort:
                        await ProcessFlowOutPortTask(task.FlowOutPort!, context, task.PathLength, taskQueue, cancellationToken);
                        break;
                        
                    case ExecutionTaskType.CalculateInputs:
                        await ProcessCalculateInputsTask(task.Node!, context, task.PathLength, taskQueue, cancellationToken);
                        break;
                }
            }
            catch (Exception ex)
            {
                if (task.Node != null)
                {
                    _logger?.LogError(ex, "Error executing task {TaskType} for {Id}", task.TaskType, task.GetId());
                    throw new NodeExecutionException($"노드 실행 중 오류 발생: {task.Node.GetType().Name}", ex, task.Node);
                }
                else
                {
                    _logger?.LogError(ex, "Error executing task {TaskType}", task.TaskType);
                    throw;
                }
            }
        }
    }
    
    /// <summary>
    /// 노드 실행 작업을 처리합니다.
    /// </summary>
    private async Task ProcessNodeTask(
        NodeBase             node,
        FlowExecutionContext context,
        int                  pathLength,
        Queue<ExecutionTask> taskQueue,
        CancellationToken    cancellationToken
    ) {
        string nodeId = $"{node.GetType().Name}_{node.Guid}";
        _logger?.LogDebug("Processing node: {NodeId} (path length: {PathLength})", nodeId, pathLength);
        
        // 중복 실행 체크를 제거하고 항상 실행합니다.
        // 실행 여부는 상위 노드(예: ForNode)가 결정하도록 합니다.
        
        // 순환 감지 (이것은 무한 순환을 방지하기 위해 유지)
        if (_visitedNodesInCurrentPath.Contains(nodeId))
        {
            _logger?.LogWarning("Cycle detected: Node {NodeId} is already in execution path", nodeId);
            return;
        }
        
        // 현재 경로에 노드 추가
        _visitedNodesInCurrentPath.Add(nodeId);
        
        try
        {
            // 노드 실행 전에 먼저 입력 계산 작업을 즉시 처리 (큐에 추가하지 않고)
            await ProcessCalculateInputsTask(node, context, pathLength + 1, taskQueue, cancellationToken);
            
            // 노드를 실행완료로 표시
            context.MarkNodeExecuted(node);
            _logger?.LogDebug("Executing node: {NodeId}", nodeId);
            
            // 노드 실행 및 FlowOutPort 즉시 처리
            await foreach (var flowOutPort in node.ExecuteAsyncFlow(cancellationToken).ConfigureAwait(false))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                
                _logger?.LogDebug("Node {NodeId} returned flow port: {PortName}", 
                    nodeId, flowOutPort.Name);
                
                // 출력 포트 즉시 처리 - 큐에 추가하는 대신 바로 처리
                await ProcessFlowOutPortImmediatelyAsync(flowOutPort, context, pathLength + 1, taskQueue, cancellationToken);
            }
        }
        finally
        {
            // 경로에서 노드 제거
            _visitedNodesInCurrentPath.Remove(nodeId);
        }
    }
    
    /// <summary>
    /// FlowOutPort를 즉시 처리합니다.
    /// </summary>
    private async Task ProcessFlowOutPortImmediatelyAsync(
        IFlowOutPort         flowOutPort,
        FlowExecutionContext context,
        int                  pathLength,
        Queue<ExecutionTask> taskQueue,
        CancellationToken    cancellationToken
    ) {
        _logger?.LogDebug("Immediately processing flow out port: {PortName} (path length: {PathLength})", 
            flowOutPort.Name, pathLength);
        
        // FlowOutPort에 연결된 모든 연결 가져오기
        var connections = flowOutPort.Connections;
        
        if (connections.Count == 0)
        {
            _logger?.LogDebug("No connections from flow out port {PortName}", flowOutPort.Name);
            return;
        }
        
        // FlowInPort로 연결된 노드들 찾기 및 즉시 실행
        var connectedNodes = new List<NodeBase>();
        
        foreach (var connection in connections)
        {
            if (connection.Target is IFlowInPort flowInPort && flowInPort.Node is NodeBase targetNode)
            {
                // 순환 감지를 위한 노드 ID 확인
                string nodeId = $"{targetNode.GetType().Name}_{targetNode.Guid}";
                
                // 이미 현재 경로에서 실행 중인 노드인지 확인
                if (_visitedNodesInCurrentPath.Contains(nodeId))
                {
                    _logger?.LogWarning("Cycle detected: Node {NodeId} is already in execution path", nodeId);
                    continue; // 순환 감지 시 건너뛰기
                }
                
                connectedNodes.Add(targetNode);
            }
        }
        
        if (connectedNodes.Count == 0)
        {
            _logger?.LogDebug("No valid target nodes from flow out port {PortName}", flowOutPort.Name);
            return;
        }
        
        // 연결된 노드들을 즉시 실행
        foreach (var targetNode in connectedNodes)
        {
            // 각 노드를 직접 처리 (큐에 추가하지 않고)
            await ProcessNodeTask(targetNode, context, pathLength + 1, taskQueue, cancellationToken);
        }
    }
    
    /// <summary>
    /// FlowOutPort 작업을 처리합니다. (이전 큐 기반 방식)
    /// </summary>
    private async Task ProcessFlowOutPortTask(
        IFlowOutPort         flowOutPort,
        FlowExecutionContext context,
        int                  pathLength,
        Queue<ExecutionTask> taskQueue,
        CancellationToken    cancellationToken
    ) {
        // 즉시 처리 방식으로 위임
        await ProcessFlowOutPortImmediatelyAsync(flowOutPort, context, pathLength, taskQueue, cancellationToken);
    }
    
    /// <summary>
    /// 입력 계산 작업을 처리합니다.
    /// </summary>
    private async Task ProcessCalculateInputsTask(
        NodeBase             node,
        FlowExecutionContext context,
        int                  pathLength,
        Queue<ExecutionTask> taskQueue,
        CancellationToken    cancellationToken
    ) {
        string nodeId = $"{node.GetType().Name}_{node.Guid}";
        _logger?.LogDebug("Calculating inputs for node: {NodeId} (path length: {PathLength})", 
            nodeId, pathLength);
        
        // InputPort에 연결된 노드들 수집
        var inputNodes = new List<NodeBase>();
        
        foreach (var inputPort in node.InputPorts)
        {
            // Flow 포트는 값 계산이 필요없으므로 제외
            if (inputPort is IFlowInPort) continue;
            
            foreach (var connection in inputPort.Connections)
            {
                if (connection.Source.Node is NodeBase sourceNode)
                {
                    // 현재 노드에 의존하는 순환 참조 감지
                    string sourceNodeId = $"{sourceNode.GetType().Name}_{sourceNode.Guid}";
                    
                    if (_visitedNodesInCurrentPath.Contains(sourceNodeId))
                    {
                        _logger?.LogWarning("Circular dependency detected: Node {SourceNodeId} depends on {NodeId}", 
                            sourceNodeId, nodeId);
                        continue; // 순환 참조 시 건너뛰기
                    }
                    
                    // 이미 실행된 노드인지 확인
                    if (!context.IsNodeExecuted(sourceNode))
                    {
                        // 실행되지 않은 노드만 추가
                        _logger?.LogDebug("Adding source node {SourceNodeId} to input nodes as it was not executed yet", 
                            sourceNodeId);
                        inputNodes.Add(sourceNode);
                    }
                    else
                    {
                        _logger?.LogDebug("Skipping source node {SourceNodeId} as it was already executed", 
                            sourceNodeId);
                    }
                }
            }
        }
        
        if (inputNodes.Count == 0)
        {
            _logger?.LogDebug("No input dependencies for node {NodeId}", nodeId);
            return;
        }
        
        // 입력 노드들을 즉시 실행 (큐에 추가하지 않고 직접 실행)
        foreach (var inputNode in inputNodes)
        {
            // 각 종속 노드를 직접 실행
            await ProcessNodeTask(inputNode, context, pathLength, taskQueue, cancellationToken);
        }
    }
}
