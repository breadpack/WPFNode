using WPFNode.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace WPFNode.Models.Execution;

public enum NodeExecutionState
{
    NotStarted,
    Running,
    Completed,
    Failed
}

public class ExecutionContext
{
    private readonly Dictionary<Guid, NodeExecutionState> _nodeStates = new();
    private readonly HashSet<INode> _executedNodes = new();
    private readonly CancellationTokenSource _cancellationTokenSource;
    
    // 백프레셔 패턴을 위한 필드 추가
    private readonly Dictionary<NodeBase, HashSet<INode>> _pendingNodes = new();
    private readonly Dictionary<INode, HashSet<NodeBase>> _dependentNodes = new();
    private readonly Queue<NodeBase> _scheduledNodes = new();
    
    // 실행 사이클 관리를 위한 필드 추가
    private int _currentCycle = 0;
    private readonly Dictionary<INode, int> _nodeCycles = new();
    private readonly Dictionary<int, HashSet<INode>> _cycleNodes = new();
    
    public ExecutionContext()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        
        // 첫 번째 사이클 초기화
        _cycleNodes[_currentCycle] = new HashSet<INode>();
    }
    
    public bool IsCancelled => _cancellationTokenSource.Token.IsCancellationRequested;
    public CancellationToken CancellationToken => _cancellationTokenSource.Token;
    
    // 백프레셔 패턴을 위한 속성 추가
    public bool HasScheduledNodes => _scheduledNodes.Count > 0;
    public NodeBase? NextScheduledNode => _scheduledNodes.Count > 0 ? _scheduledNodes.Peek() : null;
    
    public void MarkNodeExecuted(INode node)
    {
        _executedNodes.Add(node);
        if (node is NodeBase nodeBase)
        {
            _nodeStates[nodeBase.Guid] = NodeExecutionState.Completed;
        }
    }
    
    public bool IsNodeExecuted(INode node) => _executedNodes.Contains(node);
    
    public NodeExecutionState GetNodeState(NodeBase node) => 
        _nodeStates.TryGetValue(node.Guid, out var state) ? state : NodeExecutionState.NotStarted;
    
    public void SetNodeState(NodeBase node, NodeExecutionState state)
    {
        _nodeStates[node.Guid] = state;
        if (state == NodeExecutionState.Completed)
        {
            _executedNodes.Add(node);
        }
    }

    public void Reset()
    {
        _executedNodes.Clear();
        _nodeStates.Clear();
        _pendingNodes.Clear();
        _dependentNodes.Clear();
        _scheduledNodes.Clear();
        
        // 사이클 관련 필드 초기화
        _currentCycle = 0;
        _nodeCycles.Clear();
        _cycleNodes.Clear();
        _cycleNodes[_currentCycle] = new HashSet<INode>();
    }

    public void Cancel()
    {
        _cancellationTokenSource.Cancel();
    }

    public void MergeExecutionState(ExecutionContext other)
    {
        foreach (var node in other._executedNodes)
        {
            _executedNodes.Add(node);
        }
        
        // 노드 상태 병합
        foreach (var state in other._nodeStates)
        {
            _nodeStates[state.Key] = state.Value;
        }
    }
    
    // 백프레셔 패턴을 위한 메서드 추가
    
    /// <summary>
    /// 의존성이 충족되지 않아 실행이 보류된 노드를 등록합니다.
    /// </summary>
    /// <param name="node">실행이 보류된 노드</param>
    /// <param name="dependencies">충족되지 않은 의존성 노드 목록</param>
    public void AddPendingNode(NodeBase node, IEnumerable<INode> dependencies)
    {
        if (!_pendingNodes.TryGetValue(node, out var deps))
        {
            deps = new HashSet<INode>();
            _pendingNodes[node] = deps;
        }
        
        foreach (var dependency in dependencies)
        {
            deps.Add(dependency);
            
            // 역방향 매핑도 추가 (의존성 노드가 실행되었을 때 어떤 노드를 확인해야 하는지)
            if (!_dependentNodes.TryGetValue(dependency, out var dependents))
            {
                dependents = new HashSet<NodeBase>();
                _dependentNodes[dependency] = dependents;
            }
            
            dependents.Add(node);
        }
    }
    
    /// <summary>
    /// 노드가 실행된 후, 이 노드에 의존하는 다른 노드들을 확인하고 필요한 경우 실행을 예약합니다.
    /// </summary>
    /// <param name="executedNode">실행이 완료된 노드</param>
    public void CheckAndSchedulePendingNodes(NodeBase executedNode)
    {
        // 이 노드에 의존하는 노드들 확인
        if (_dependentNodes.TryGetValue(executedNode, out var dependents))
        {
            foreach (var dependent in dependents)
            {
                // 의존성 목록에서 실행된 노드 제거
                if (_pendingNodes.TryGetValue(dependent, out var dependencies))
                {
                    dependencies.Remove(executedNode);
                    
                    // 모든 의존성이 충족되었으면 실행 예약
                    if (dependencies.Count == 0)
                    {
                        _pendingNodes.Remove(dependent);
                        ScheduleNode(dependent);
                    }
                }
            }
            
            // 처리 완료된 의존 관계 제거
            _dependentNodes.Remove(executedNode);
        }
    }
    
    /// <summary>
    /// 노드를 실행 대기열에 추가합니다.
    /// </summary>
    /// <param name="node">실행할 노드</param>
    public void ScheduleNode(NodeBase node)
    {
        if (!_scheduledNodes.Contains(node))
        {
            _scheduledNodes.Enqueue(node);
        }
    }
    
    /// <summary>
    /// 다음 실행 예약된 노드를 가져옵니다.
    /// </summary>
    /// <returns>실행할 노드 또는 없으면 null</returns>
    public NodeBase? DequeueScheduledNode()
    {
        return _scheduledNodes.Count > 0 ? _scheduledNodes.Dequeue() : null;
    }
    
    /// <summary>
    /// 현재 실행 사이클 번호를 가져옵니다. 다이아몬드 패턴 등에서 동기화에 사용됩니다.
    /// </summary>
    /// <returns>현재 실행 사이클 번호</returns>
    public int GetCurrentCycle()
    {
        return _currentCycle;
    }
    
    /// <summary>
    /// 노드를 현재 실행 사이클에 등록합니다.
    /// </summary>
    /// <param name="node">등록할 노드</param>
    public void RegisterNodeInCurrentCycle(INode node)
    {
        _nodeCycles[node] = _currentCycle;
        _cycleNodes[_currentCycle].Add(node);
    }
    
    /// <summary>
    /// 노드가 속한 실행 사이클을 가져옵니다.
    /// </summary>
    /// <param name="node">조회할 노드</param>
    /// <returns>노드가 속한 사이클 번호 또는 현재 사이클</returns>
    public int GetNodeCycle(INode node)
    {
        return _nodeCycles.TryGetValue(node, out var cycle) ? cycle : _currentCycle;
    }
    
    /// <summary>
    /// 다음 실행 사이클로 진행합니다.
    /// </summary>
    public void AdvanceCycle()
    {
        _currentCycle++;
        _cycleNodes[_currentCycle] = new HashSet<INode>();
    }
    
    /// <summary>
    /// 특정 사이클에 속한 모든 노드를 가져옵니다.
    /// </summary>
    /// <param name="cycle">조회할 사이클 번호</param>
    /// <returns>해당 사이클에 속한 노드 목록</returns>
    public IReadOnlyCollection<INode> GetNodesInCycle(int cycle)
    {
        return _cycleNodes.TryGetValue(cycle, out var nodes) ? nodes : Array.Empty<INode>();
    }
    
    /// <summary>
    /// 특정 사이클의 모든 노드가 실행 완료되었는지 확인합니다.
    /// </summary>
    /// <param name="cycle">확인할 사이클 번호</param>
    /// <returns>모든 노드가 실행 완료되었으면 true</returns>
    public bool IsCycleCompleted(int cycle)
    {
        if (!_cycleNodes.TryGetValue(cycle, out var nodes))
            return true; // 해당 사이클에 노드가 없으면 완료된 것으로 간주
            
        return nodes.All(IsNodeExecuted);
    }
}