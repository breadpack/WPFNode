using Microsoft.Extensions.Logging;
using WPFNode.Interfaces;
using WPFNode.Models.Execution.Executors;

namespace WPFNode.Models.Execution;

public class ExecutionPlanBuilder
{
    private readonly ILogger? _logger;
    private readonly Dictionary<INode, IExecutable> _executorMap = new();

    public ExecutionPlanBuilder(ILogger? logger = null)
    {
        _logger = logger;
    }

    public IExecutable BuildExecutionPlan(
        IEnumerable<NodeBase> nodes,
        IEnumerable<IConnection> connections,
        bool enableParallel = true)
    {
        var nodeHierarchy = AnalyzeNodeHierarchy(nodes, connections);
        return CreateExecutors(nodeHierarchy, enableParallel);
    }

    /// <summary>
    /// IFlowEntry를 상속받은 노드를 시작점으로 사용하여 실행 계획을 구성합니다.
    /// </summary>
    public IExecutable BuildExecutionPlanWithEntryPoints(
        IEnumerable<NodeBase> nodes,
        IEnumerable<IConnection> connections,
        bool enableParallel = true)
    {
        // IFlowEntry를 구현한 노드들을 찾습니다.
        var entryNodes = nodes.Where(n => n is IFlowEntry).ToList();
        
        if (entryNodes.Count == 0)
        {
            _logger?.LogWarning("No valid IFlowEntry nodes found. Using standard node hierarchy analysis.");
            return BuildExecutionPlan(nodes, connections, enableParallel);
        }
        
        _logger?.LogInformation("Found {Count} entry points", entryNodes.Count);
        
        // 엔트리 포인트 노드를 기준으로 실행 계획 생성
        var nodeHierarchy = AnalyzeNodeHierarchyWithEntryPoints(entryNodes, nodes, connections);
        return CreateExecutors(nodeHierarchy, enableParallel);
    }

    private class NodeInfo
    {
        public INode Node { get; }
        public bool IsLoopNode { get; }
        public bool HasParent { get; set; }
        public List<NodeInfo> Children { get; } = new();
        public HashSet<INode> DirectDependencies { get; } = new();
        public bool IsEntryPoint { get; set; }

        public NodeInfo(INode node, bool isEntryPoint = false)
        {
            Node = node;
            IsLoopNode = node is ILoopNode;
            IsEntryPoint = isEntryPoint;
        }
    }

    private List<NodeInfo> AnalyzeNodeHierarchy(
        IEnumerable<NodeBase> nodes,
        IEnumerable<IConnection> connections)
    {
        // 노드 정보 한 번만 생성
        var nodeInfos = nodes.Select(n => new NodeInfo(n)).ToList();
        var nodeInfoMap = nodeInfos.ToDictionary(info => info.Node);
        
        // 직접적인 의존성 관계 구성 (데이터 흐름 방향)
        BuildDirectDependencies(connections, nodeInfoMap);
        
        // 위상 정렬 수행 (nodeInfos 활용)
        var sortedNodeInfos = PerformTopologicalSort(nodeInfos, connections, nodeInfoMap);
        
        // 계층 구조 구성
        BuildNodeHierarchy(nodeInfos, sortedNodeInfos);
        
        // 루프 노드에 대한 특별 처리
        ProcessLoopNodes(nodeInfos);
        
        // 루트 노드 (부모가 없는 노드) 반환
        var rootNodes = nodeInfos.Where(n => !n.HasParent).ToList();
        
        // 의존성 관계 로깅
        LogDependencyInfo(rootNodes);
        
        return rootNodes;
    }
    
    /// <summary>
    /// IFlowEntry 노드를 시작점으로 사용하여 노드 계층 구조를 분석합니다.
    /// </summary>
    private List<NodeInfo> AnalyzeNodeHierarchyWithEntryPoints(
        List<NodeBase> entryNodes,
        IEnumerable<NodeBase> allNodes,
        IEnumerable<IConnection> connections)
    {
        var nodeMap = allNodes.ToDictionary(n => n, n => new NodeInfo(n, entryNodes.Contains(n)));
        var rootNodes = new List<NodeInfo>();

        // 엔트리 포인트 노드들을 루트 노드로 설정
        foreach (var entryNode in entryNodes)
        {
            rootNodes.Add(nodeMap[entryNode]);
        }

        // Flow 연결을 기반으로 노드 계층 구조 생성
        foreach (var connection in connections.Where(c => c.Target is IFlowInPort))
        {
            var sourceNode = connection.Source.Node;
            var targetNode = connection.Target.Node;

            if (sourceNode is NodeBase sourceBase && targetNode is NodeBase targetBase &&
                nodeMap.TryGetValue(sourceBase, out var sourceInfo) && 
                nodeMap.TryGetValue(targetBase, out var targetInfo))
            {
                sourceInfo.Children.Add(targetInfo);
                targetInfo.HasParent = true;
                targetInfo.DirectDependencies.Add(sourceBase);
            }
        }

        // 엔트리 포인트가 아닌 부모가 없는 노드들을 추가
        if (rootNodes.Count == 0)
        {
            rootNodes.AddRange(nodeMap.Values.Where(n => !n.HasParent && !n.IsEntryPoint));
        }

        return rootNodes;
    }
    
    private void BuildDirectDependencies(
        IEnumerable<IConnection> connections, 
        Dictionary<INode, NodeInfo> nodeInfoMap)
    {
        foreach (var connection in connections)
        {
            var sourceNode = connection.Source.Node;
            var targetNode = connection.Target.Node;
            
            if (nodeInfoMap.TryGetValue(targetNode, out var targetInfo) && 
                nodeInfoMap.TryGetValue(sourceNode, out var sourceInfo))
            {
                targetInfo.DirectDependencies.Add(sourceNode);
            }
        }
    }
    
    private List<NodeInfo> PerformTopologicalSort(
        List<NodeInfo> nodeInfos,
        IEnumerable<IConnection> connections,
        Dictionary<INode, NodeInfo> nodeInfoMap)
    {
        // 위상 정렬을 위한 준비
        var inDegree = new Dictionary<NodeInfo, int>();
        var outgoingEdges = new Dictionary<NodeInfo, List<NodeInfo>>();
        
        // 모든 노드에 대해 초기화
        foreach (var nodeInfo in nodeInfos)
        {
            inDegree[nodeInfo] = 0;
            outgoingEdges[nodeInfo] = new List<NodeInfo>();
        }
        
        // 진입 차수(in-degree)와 나가는 엣지 계산
        foreach (var connection in connections)
        {
            var sourceNode = connection.Source.Node;
            var targetNode = connection.Target.Node;
            
            if (nodeInfoMap.TryGetValue(sourceNode, out var sourceInfo) && 
                nodeInfoMap.TryGetValue(targetNode, out var targetInfo))
            {
                inDegree[targetInfo]++;
                outgoingEdges[sourceInfo].Add(targetInfo);
            }
        }
        
        // 위상 정렬 수행
        var sortedNodeInfos = new List<NodeInfo>();
        var noIncomingEdges = new Queue<NodeInfo>(nodeInfos.Where(n => inDegree[n] == 0));
        
        while (noIncomingEdges.Count > 0)
        {
            var nodeInfo = noIncomingEdges.Dequeue();
            sortedNodeInfos.Add(nodeInfo);
            
            foreach (var targetNodeInfo in outgoingEdges[nodeInfo])
            {
                inDegree[targetNodeInfo]--;
                if (inDegree[targetNodeInfo] == 0)
                {
                    noIncomingEdges.Enqueue(targetNodeInfo);
                }
            }
        }
        
        // 순환 의존성이 있는 경우 처리
        if (sortedNodeInfos.Count < nodeInfos.Count)
        {
            _logger?.LogWarning("순환 의존성이 감지되었습니다. 모든 노드가 위상 정렬되지 않았습니다.");
            
            // 아직 처리되지 않은 노드들 추가 (HashSet 사용하여 중복 검사 최적화)
            var sortedNodeInfosSet = new HashSet<NodeInfo>(sortedNodeInfos);
            foreach (var nodeInfo in nodeInfos)
            {
                if (!sortedNodeInfosSet.Contains(nodeInfo))
                {
                    sortedNodeInfos.Add(nodeInfo);
                }
            }
        }
        
        return sortedNodeInfos;
    }
    
    private void BuildNodeHierarchy(
        List<NodeInfo> nodeInfos,
        List<NodeInfo> sortedNodeInfos)
    {
        // 계층 구조 구성 (위상 정렬된 순서의 역순으로)
        for (int i = sortedNodeInfos.Count - 1; i >= 0; i--)
        {
            var currentInfo = sortedNodeInfos[i];
            
            // 이미 부모가 있는 경우 건너뜀
            if (currentInfo.HasParent)
                continue;
            
            // 이 노드에 직접 의존하는 노드들 찾기
            var dependentNodes = nodeInfos
                .Where(n => n.DirectDependencies.Contains(currentInfo.Node) && !n.HasParent)
                .ToList();
            
            // 의존하는 노드들을 자식으로 추가
            foreach (var dependentNode in dependentNodes)
            {
                // 루프 노드가 다른 루프 노드에 의존하는 특별한 경우 처리
                if (currentInfo.IsLoopNode && dependentNode.IsLoopNode)
                {
                    // 중첩 루프의 경우, 내부 루프가 외부 루프의 자식이 되어야 함
                    // 따라서 현재 루프 노드가 의존하는 루프 노드는 자식으로 추가하지 않음
                    if (currentInfo.DirectDependencies.Contains(dependentNode.Node))
                        continue;
                }
                
                currentInfo.Children.Add(dependentNode);
                dependentNode.HasParent = true;
            }
        }
    }
    
    private void ProcessLoopNodes(List<NodeInfo> nodeInfos)
    {
        var loopNodes = nodeInfos.Where(n => n.IsLoopNode).ToList();
        foreach (var loopNode in loopNodes)
        {
            // 루프 노드의 자식들의 집합 구성
            var loopChildren = new HashSet<INode>(loopNode.Children.Select(c => c.Node));
            var additionalChildren = new List<NodeInfo>();
            
            // 루프 자식 노드에 의존하는 노드들 찾기
            foreach (var nodeInfo in nodeInfos)
            {
                // 이미 루프의 자식이거나 부모가 있는 경우 건너뜀
                if (loopChildren.Contains(nodeInfo.Node) || nodeInfo.HasParent)
                    continue;
                
                // 루프 자식 노드에 의존하는 노드를 찾아 추가
                if (nodeInfo.DirectDependencies.Any(d => loopChildren.Contains(d)))
                {
                    additionalChildren.Add(nodeInfo);
                    nodeInfo.HasParent = true;
                    loopChildren.Add(nodeInfo.Node);
                }
            }
            
            // 추가 자식 노드들을 루프 노드에 추가
            foreach (var additionalChild in additionalChildren)
            {
                loopNode.Children.Add(additionalChild);
            }
        }
    }
    
    private void LogDependencyInfo(List<NodeInfo> rootNodes)
    {
        foreach (var rootNode in rootNodes)
        {
            _logger?.LogDebug("루트 노드: {NodeType}, 자식 수: {ChildCount}", 
                rootNode.Node.GetType().Name, rootNode.Children.Count);
        }
    }

    private IExecutable CreateExecutors(List<NodeInfo> rootNodes, bool enableParallel)
    {
        var rootExecutor = new SequentialExecutor(_logger);

        foreach (var rootNode in rootNodes)
        {
            var executor = CreateNodeExecutor(rootNode, enableParallel);
            rootExecutor.AddExecutor(executor);
        }

        return rootExecutor;
    }

    private IExecutable CreateNodeExecutor(NodeInfo nodeInfo, bool enableParallel) {
        if (_executorMap.TryGetValue(nodeInfo.Node, out var existingExecutor)) {
            return existingExecutor;
        }

        // 노드 타입에 따라 적절한 실행기 생성
        IExecutable executor = nodeInfo.IsLoopNode 
            ? CreateLoopExecutor(nodeInfo, enableParallel)
            : CreateStandardExecutor(nodeInfo, enableParallel);

        return executor;
    }
    
    private IExecutable CreateLoopExecutor(NodeInfo nodeInfo, bool enableParallel)
    {
        var loopExecutor = new LoopExecutor((ILoopNode)nodeInfo.Node, _logger);
        var bodyExecutor = CreateLoopBodyExecutor(nodeInfo, enableParallel);
        loopExecutor.AddBodyExecutor(bodyExecutor);
        
        _executorMap[nodeInfo.Node] = loopExecutor;
        return loopExecutor;
    }
    
    private IExecutable CreateStandardExecutor(NodeInfo nodeInfo, bool enableParallel)
    {
        // 노드 실행기 생성
        var nodeExecutor = new NodeExecutor(nodeInfo.Node, _logger);
        _executorMap[nodeInfo.Node] = nodeExecutor;
        
        // 자식 노드가 없으면 노드 실행기만 반환
        if (!nodeInfo.Children.Any())
            return nodeExecutor;
            
        // 자식 노드가 있는 경우 순차 실행기 생성
        var sequentialExecutor = new SequentialExecutor(_logger);
        sequentialExecutor.AddExecutor(nodeExecutor);
            
        // 자식 노드들의 실행기 추가
        if (enableParallel && nodeInfo.Children.Count > 1) 
        {
            // 병렬 실행이 가능하면 병렬 실행기 사용
            var childExecutors = nodeInfo.Children.Select(child => 
                CreateNodeExecutor(child, enableParallel));
            sequentialExecutor.AddExecutor(new ParallelExecutor(childExecutors, _logger));
        }
        else 
        {
            // 그렇지 않으면 순차 실행기에 각 자식 노드 추가
            foreach (var child in nodeInfo.Children) 
            {
                sequentialExecutor.AddExecutor(CreateNodeExecutor(child, enableParallel));
            }
        }
            
        return sequentialExecutor;
    }

    private IExecutable CreateLoopBodyExecutor(NodeInfo loopInfo, bool enableParallel)
    {
        // 루프 노드의 모든 자식 노드들을 포함
        var dependentNodes = loopInfo.Children;

        if (!dependentNodes.Any())
            return new SequentialExecutor(_logger);

        if (enableParallel && dependentNodes.Count > 1)
        {
            var executors = dependentNodes.Select(n => CreateNodeExecutor(n, enableParallel));
            return new ParallelExecutor(executors, _logger);
        }
        else
        {
            var sequentialExecutor = new SequentialExecutor(_logger);
            foreach (var node in dependentNodes)
            {
                sequentialExecutor.AddExecutor(CreateNodeExecutor(node, enableParallel));
            }
            return sequentialExecutor;
        }
    }
} 
