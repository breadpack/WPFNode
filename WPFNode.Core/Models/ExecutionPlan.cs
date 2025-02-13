using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WPFNode.Abstractions;

namespace WPFNode.Core.Models;

public class ExecutionPlan
{
    private readonly List<ExecutionLevel> _levels = new();
    private readonly ExecutionContext _context;
    
    public IReadOnlyList<ExecutionLevel> Levels => _levels;
    public ExecutionContext Context => _context;

    private ExecutionPlan(List<ExecutionLevel> levels)
    {
        _levels = levels;
        _context = new ExecutionContext();
    }

    public static ExecutionPlan Create(IEnumerable<NodeBase> nodes, IEnumerable<IConnection> connections)
    {
        var visited = new HashSet<Guid>();
        var levels = new Dictionary<Guid, int>();
        var inProcess = new HashSet<Guid>();

        // 출력 노드들을 찾아서 실행 계획 수립
        var outputNodes = nodes.Where(n => n.IsOutputNode).ToList();
        if (!outputNodes.Any())
        {
            throw new InvalidOperationException("실행 가능한 출력 노드가 없습니다.");
        }

        // 각 노드의 레벨을 계산
        foreach (var outputNode in outputNodes)
        {
            if (!visited.Contains(outputNode.Id))
            {
                CalculateNodeLevels(outputNode, visited, inProcess, levels, connections);
            }
        }

        // 레벨별로 노드 그룹화
        var maxLevel = levels.Values.Max();
        var executionLevels = new List<ExecutionLevel>();
        
        for (int i = maxLevel; i >= 0; i--)
        {
            var levelNodes = nodes
                .Where(n => levels.TryGetValue(n.Id, out var level) && level == i)
                .ToList();
                
            if (levelNodes.Any())
            {
                executionLevels.Add(new ExecutionLevel(i, levelNodes));
            }
        }

        executionLevels.Reverse();

        return new ExecutionPlan(executionLevels);
    }

    private static int CalculateNodeLevels(
        NodeBase node, 
        HashSet<Guid> visited, 
        HashSet<Guid> inProcess, 
        Dictionary<Guid, int> levels,
        IEnumerable<IConnection> connections)
    {
        if (inProcess.Contains(node.Id))
            throw new InvalidOperationException("순환 의존성이 감지되었습니다.");

        if (visited.Contains(node.Id))
            return levels[node.Id];

        inProcess.Add(node.Id);

        // 현재 노드의 입력 포트에 연결된 모든 소스 노드를 찾음
        var dependencies = connections
            .Where(c => c.Target.Node == node)
            .Select(c => c.Source.Node as NodeBase)
            .Where(n => n != null)
            .Distinct();

        int maxDependencyLevel = -1;
        foreach (var dependency in dependencies)
        {
            var dependencyLevel = CalculateNodeLevels(dependency!, visited, inProcess, levels, connections);
            maxDependencyLevel = Math.Max(maxDependencyLevel, dependencyLevel);
        }

        var nodeLevel = maxDependencyLevel + 1;
        levels[node.Id] = nodeLevel;

        visited.Add(node.Id);
        inProcess.Remove(node.Id);

        return nodeLevel;
    }

    public IEnumerable<NodeBase> GetNodesAtLevel(int level)
    {
        var executionLevel = _levels.FirstOrDefault(l => l.Level == level);
        return executionLevel?.Nodes ?? Enumerable.Empty<NodeBase>();
    }

    public int MaxLevel => _levels.Count > 0 ? _levels.Max(l => l.Level) : -1;

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        foreach (var level in _levels)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            await ExecuteLevelAsync(level, cancellationToken);
        }
    }

    private async Task ExecuteLevelAsync(ExecutionLevel level, CancellationToken cancellationToken)
    {
        // 같은 레벨의 노드들은 병렬 실행
        var executionTasks = level.Nodes.Select(node => ExecuteNodeAsync(node, cancellationToken));

        try
        {
            // 현재 레벨의 모든 노드가 완료될 때까지 대기
            await Task.WhenAll(executionTasks);
        }
        catch (AggregateException ae)
        {
            // 여러 노드에서 발생한 예외들을 모아서 처리
            var failedNodes = ae.InnerExceptions
                .OfType<NodeExecutionException>()
                .Select(ex => ex.Node)
                .ToList();

            throw new NodeExecutionException(
                $"다음 노드들의 실행이 실패했습니다: {string.Join(", ", failedNodes.Select(n => n.Name))}",
                ae.InnerExceptions.First(),
                failedNodes);
        }
    }

    private async Task ExecuteNodeAsync(NodeBase node, CancellationToken cancellationToken)
    {
        try
        {
            _context.SetNodeState(node, NodeExecutionState.Running);
            await node.ProcessAsync();
            _context.SetNodeState(node, NodeExecutionState.Completed);
        }
        catch (Exception ex)
        {
            _context.SetNodeState(node, NodeExecutionState.Failed);
            throw new NodeExecutionException($"Node '{node.Name}' execution failed", ex, node);
        }
    }
}

public class ExecutionLevel
{
    public int Level { get; }
    public IReadOnlyList<NodeBase> Nodes { get; }

    public ExecutionLevel(int level, IEnumerable<NodeBase> nodes)
    {
        Level = level;
        Nodes = nodes.ToList();
    }
}

public class NodeExecutionException : Exception
{
    public NodeBase Node { get; }
    public IReadOnlyList<NodeBase>? FailedNodes { get; }

    public NodeExecutionException(string message, Exception innerException, NodeBase node)
        : base(message, innerException)
    {
        Node = node;
    }

    public NodeExecutionException(string message, Exception innerException, IReadOnlyList<NodeBase> failedNodes)
        : base(message, innerException)
    {
        FailedNodes = failedNodes;
    }
} 