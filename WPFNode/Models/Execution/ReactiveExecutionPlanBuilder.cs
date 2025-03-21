using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WPFNode.Interfaces;
using WPFNode.Interfaces.Flow;

namespace WPFNode.Models.Execution;

/// <summary>
/// 입력 기반 실행 계획을 구성하는 빌더 클래스 
/// (Reactive Programming 방식으로 입력 노드에서 출력 노드로 흐름)
/// </summary>
public class ReactiveExecutionPlanBuilder
{
    private readonly ILogger? _logger;
    private readonly Dictionary<INode, NodeInfo> _nodeInfos = new();
    
    /// <summary>
    /// 기본 생성자
    /// </summary>
    /// <param name="logger">로거</param>
    public ReactiveExecutionPlanBuilder(ILogger? logger = null)
    {
        _logger = logger;
    }
    
    /// <summary>
    /// 노드 정보를 저장하는 내부 클래스
    /// </summary>
    internal class NodeInfo
    {
        /// <summary>
        /// 노드 객체
        /// </summary>
        public INode Node { get; }
        
        /// <summary>
        /// 이 노드가 IFlowEntryPoint 인터페이스를 구현하는지 여부
        /// </summary>
        public bool IsEntryPoint { get; }
        
        /// <summary>
        /// 이 노드가 의존하는 노드 목록 (입력 의존성)
        /// </summary>
        public HashSet<INode> Dependencies { get; } = new();
        
        /// <summary>
        /// 이 노드에 의존하는 노드 목록 (출력 의존성)
        /// </summary>
        public HashSet<INode> DependentNodes { get; } = new();
        
        /// <summary>
        /// 흐름 입력 포트 목록
        /// </summary>
        public List<IFlowInPort> FlowInPorts { get; } = new();
        
        /// <summary>
        /// 흐름 출력 포트 목록
        /// </summary>
        public List<IFlowOutPort> FlowOutPorts { get; } = new();
        
        /// <summary>
        /// 데이터 입력 포트 목록
        /// </summary>
        public List<IInputPort> InputPorts { get; } = new();
        
        /// <summary>
        /// 데이터 출력 포트 목록
        /// </summary>
        public List<IOutputPort> OutputPorts { get; } = new();
        
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="node">노드 객체</param>
        public NodeInfo(INode node)
        {
            Node = node;
            IsEntryPoint = node is IFlowEntryPoint;
            
            // 노드 포트 초기화
            if (node is NodeBase nodeBase)
            {
                InputPorts.AddRange(nodeBase.InputPorts);
                OutputPorts.AddRange(nodeBase.OutputPorts);
                FlowInPorts.AddRange(nodeBase.FlowInPorts);
                FlowOutPorts.AddRange(nodeBase.FlowOutPorts);
            }
        }
    }
    
    /// <summary>
    /// 노드 간의 의존성 관계를 분석하고 실행 계획 구성
    /// </summary>
    /// <param name="canvas">노드 캔버스</param>
    /// <returns>실행 계획</returns>
    public ReactiveExecutionPlan BuildExecutionPlan(NodeCanvas canvas)
    {
        // 노드 정보 초기화
        InitializeNodeInfos(canvas.Nodes, canvas.Connections);
        
        // 입력 의존성 및 출력 의존성 구축
        AnalyzeDependencies();
        
        // 시작점 노드들 찾기
        var entryPoints = FindEntryPoints();
        
        _logger?.LogDebug("ReactiveExecutionPlan 구성 완료: 시작점 {EntryPointCount}개", entryPoints.Count);
        
        return new ReactiveExecutionPlan(entryPoints, _nodeInfos);
    }
    
    /// <summary>
    /// 노드 정보 초기화
    /// </summary>
    private void InitializeNodeInfos(IEnumerable<INode> nodes, IEnumerable<IConnection> connections)
    {
        // 노드 정보 생성
        foreach (var node in nodes)
        {
            _nodeInfos[node] = new NodeInfo(node);
        }
        
        // 연결 정보 분석
        foreach (var connection in connections)
        {
            var sourceNode = connection.Source.Node;
            var targetNode = connection.Target.Node;
            
            if (sourceNode != null && targetNode != null)
            {
                if (_nodeInfos.TryGetValue(sourceNode, out var sourceInfo) && 
                    _nodeInfos.TryGetValue(targetNode, out var targetInfo))
                {
                    // 입력 노드가 출력 노드에 의존함
                    targetInfo.Dependencies.Add(sourceNode);
                    sourceInfo.DependentNodes.Add(targetNode);
                }
            }
        }
    }
    
    /// <summary>
    /// 노드 간의 의존성 관계 분석
    /// </summary>
    private void AnalyzeDependencies()
    {
        foreach (var nodeInfo in _nodeInfos.Values)
        {
            var node = nodeInfo.Node;
            
            // 흐름 의존성 분석
            if (node is NodeBase nodeBase)
            {
                foreach (var flowInPort in nodeBase.FlowInPorts)
                {
                    // 흐름 입력 포트에 연결된 출력 포트 찾기
                    if (flowInPort is IFlowInPort flowPort)
                    {
                        foreach (var connection in flowPort.Connections)
                        {
                            if (connection.Source is IFlowOutPort && connection.Source.Node != null)
                            {
                                // 입력 노드가 출력 노드에 의존함
                                nodeInfo.Dependencies.Add(connection.Source.Node);
                                
                                if (_nodeInfos.TryGetValue(connection.Source.Node, out var sourceInfo))
                                {
                                    sourceInfo.DependentNodes.Add(node);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 시작점 노드 찾기
    /// </summary>
    /// <returns>시작점 노드 목록</returns>
    private List<INode> FindEntryPoints()
    {
        var entryPoints = new List<INode>();
        
        foreach (var (node, info) in _nodeInfos)
        {
            // IFlowEntryPoint 인터페이스를 구현하는 노드는 시작점
            if (info.IsEntryPoint)
            {
                entryPoints.Add(node);
                continue;
            }
            
            // 흐름 입력 포트가 없는 노드 중 흐름 출력 포트가 있는 노드도 시작점
            if (info.FlowInPorts.Count == 0 && info.FlowOutPorts.Count > 0)
            {
                entryPoints.Add(node);
            }
        }
        
        return entryPoints;
    }
    
    /// <summary>
    /// 입력 노드 기반 실행 계획
    /// </summary>
    public class ReactiveExecutionPlan
    {
        private readonly List<INode> _entryPoints;
        private readonly Dictionary<INode, NodeInfo> _nodeInfos;
        private readonly ILogger? _logger;
        
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="entryPoints">시작점 노드 목록</param>
        /// <param name="nodeInfos">노드 정보 딕셔너리</param>
        /// <param name="logger">로거</param>
        internal ReactiveExecutionPlan(
            List<INode> entryPoints, 
            Dictionary<INode, NodeInfo> nodeInfos,
            ILogger? logger = null)
        {
            _entryPoints = entryPoints;
            _nodeInfos = nodeInfos;
            _logger = logger;
        }
        
        /// <summary>
        /// 실행 계획 실행
        /// </summary>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>비동기 작업</returns>
        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var context = new ExecutionContext();
            
            try
            {
                // 시작점 노드부터 실행
                foreach (var entryPoint in _entryPoints)
                {
                    if (entryPoint is NodeBase nodeBase)
                    {
                        await ExecuteNodeAsync(nodeBase, context, cancellationToken);
                    }
                }
                
                // 백프레셔 패턴: 예약된 노드 처리
                await ProcessScheduledNodesAsync(context, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ReactiveExecutionPlan 실행 중 오류 발생");
                throw;
            }
        }
        
        /// <summary>
        /// 노드 실행
        /// </summary>
        /// <param name="node">실행할 노드</param>
        /// <param name="context">실행 컨텍스트</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>비동기 작업</returns>
        private async Task ExecuteNodeAsync(
            NodeBase node, 
            ExecutionContext context, 
            CancellationToken cancellationToken)
        {
            // 이미 실행된 노드는 다시 실행하지 않음
            if (context.IsNodeExecuted(node))
                return;
            
            try
            {
                // 입력 의존성이 모두 충족되었는지 확인
                await EnsureInputsExecutedAsync(node, context, cancellationToken);
                
                // 노드 실행
                await node.ExecuteAsync(cancellationToken);
                
                // 노드 실행 완료 표시
                context.MarkNodeExecuted(node);
                
                // 흐름 전파
                await node.PropagateFlowAsync(context, cancellationToken);
                
                // 이 노드에 의존하는 노드 확인
                if (_nodeInfos.TryGetValue(node, out var nodeInfo))
                {
                    foreach (var dependentNode in nodeInfo.DependentNodes)
                    {
                        if (dependentNode is NodeBase dependentNodeBase)
                        {
                            // 종속 노드 실행 예약
                            context.ScheduleNode(dependentNodeBase);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "노드 {NodeType} 실행 중 오류 발생", node.GetType().Name);
                
                if (node is NodeBase nodeBase)
                {
                    context.SetNodeState(nodeBase, NodeExecutionState.Failed);
                }
                
                throw;
            }
        }
        
        /// <summary>
        /// 백프레셔 패턴: 예약된 노드들을 처리
        /// </summary>
        /// <param name="context">실행 컨텍스트</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>비동기 작업</returns>
        private async Task ProcessScheduledNodesAsync(
            ExecutionContext context, 
            CancellationToken cancellationToken)
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
                
                // 이미 실행된 노드는 건너뜀
                if (context.IsNodeExecuted(node))
                    continue;
                
                // 노드 실행
                await ExecuteNodeAsync(node, context, cancellationToken);
            }
            
            if (iterationCount >= maxIterations)
            {
                _logger?.LogWarning("백프레셔: 최대 반복 횟수 ({MaxIterations})에 도달했습니다. 순환 의존성이 있을 수 있습니다.", 
                    maxIterations);
            }
        }
        
        /// <summary>
        /// 노드의 입력 의존성 충족 확인 및 처리
        /// </summary>
        /// <param name="node">확인할 노드</param>
        /// <param name="context">실행 컨텍스트</param>
        /// <param name="cancellationToken">취소 토큰</param>
        /// <returns>비동기 작업</returns>
        private async Task EnsureInputsExecutedAsync(
            NodeBase node, 
            ExecutionContext context, 
            CancellationToken cancellationToken)
        {
            // 의존성 노드들 먼저 실행
            if (_nodeInfos.TryGetValue(node, out var nodeInfo))
            {
                var pendingDependencies = new HashSet<INode>();
                
                // 데이터 입력 포트 연결 확인
                foreach (var inputPort in node.InputPorts)
                {
                    if (inputPort.IsConnected)
                    {
                        foreach (var connection in inputPort.Connections)
                        {
                            var sourceNode = connection.Source.Node;
                            if (sourceNode != null && !context.IsNodeExecuted(sourceNode))
                            {
                                pendingDependencies.Add(sourceNode);
                            }
                        }
                    }
                }
                
                // 의존성 노드들 실행
                if (pendingDependencies.Count > 0)
                {
                    context.AddPendingNode(node, pendingDependencies);
                    
                    foreach (var dependency in pendingDependencies)
                    {
                        if (dependency is NodeBase dependencyBase)
                        {
                            await ExecuteNodeAsync(dependencyBase, context, cancellationToken);
                        }
                    }
                }
            }
        }
    }
}
