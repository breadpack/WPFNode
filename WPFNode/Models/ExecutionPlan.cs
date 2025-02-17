using Microsoft.Extensions.Logging;
using WPFNode.Constants;
using WPFNode.Exceptions;
using WPFNode.Interfaces;
using WPFNode.Utilities;

namespace WPFNode.Models;

public class ExecutionPlan
{
    private readonly List<ExecutionLevel> _levels = new();
    private readonly ExecutionContext _context;
    private readonly ILogger<ExecutionPlan>? _logger;
    
    public IReadOnlyList<ExecutionLevel> Levels => _levels;
    public ExecutionContext Context => _context;

    private ExecutionPlan(List<ExecutionLevel> levels, ILogger<ExecutionPlan>? logger = null)
    {
        _levels = levels;
        _context = new ExecutionContext();
        _logger = logger;
    }

    public static ExecutionPlan Create(IEnumerable<NodeBase> nodes, IEnumerable<IConnection> connections, ILogger<ExecutionPlan>? logger = null)
    {
        var visited = new HashSet<Guid>();
        var levels = new Dictionary<Guid, int>();
        var inProcess = new HashSet<Guid>();

        // 출력 노드들을 찾아서 실행 계획 수립
        var outputNodes = nodes.Where(n => n.IsOutputNode).ToList();
        if (!outputNodes.Any())
        {
            throw new NodeValidationException("실행 가능한 출력 노드가 없습니다.");
        }

        // 각 노드의 레벨을 계산
        foreach (var outputNode in outputNodes)
        {
            if (!visited.Contains(outputNode.Id))
            {
                try
                {
                    CalculateNodeLevels(outputNode, visited, inProcess, levels, connections);
                }
                catch (NodeExecutionException ex)
                {
                    logger?.LogError(ex, LoggerMessages.CircularDependencyDetected, outputNode.Name);
                    throw;
                }
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
        return new ExecutionPlan(executionLevels, logger);
    }

    private static int CalculateNodeLevels(
        NodeBase node, 
        HashSet<Guid> visited, 
        HashSet<Guid> inProcess, 
        Dictionary<Guid, int> levels,
        IEnumerable<IConnection> connections)
    {
        if (inProcess.Contains(node.Id))
            throw new NodeExecutionException(
                $"순환 의존성이 감지되었습니다: {node.Name}", 
                node);

        if (visited.Contains(node.Id))
            return levels[node.Id];

        inProcess.Add(node.Id);

        try
        {
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

            return nodeLevel;
        }
        finally
        {
            visited.Add(node.Id);
            inProcess.Remove(node.Id);
        }
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
            {
                _logger?.LogInformation(LoggerMessages.ExecutionCancelled);
                break;
            }

            try
            {
                await ExecuteLevelAsync(level, cancellationToken);
            }
            catch (NodeExecutionException ex)
            {
                _logger?.LogError(ex, LoggerMessages.NodeExecutionFailed, 
                    ex.FailedNodes != null 
                        ? string.Join(", ", ex.FailedNodes.Select(n => n.Name))
                        : ex.Node.Name);
                throw;
            }
        }
    }

    private async Task ExecuteLevelAsync(ExecutionLevel level, CancellationToken cancellationToken)
    {
        _logger?.LogInformation(LoggerMessages.LevelExecutionStarted, level.Level);
        
        // 같은 레벨의 노드들은 병렬 실행
        var executionTasks = level.Nodes.Select(node => ExecuteNodeAsync(node, cancellationToken));

        try
        {
            // 현재 레벨의 모든 노드가 완료될 때까지 대기
            await Task.WhenAll(executionTasks);
            _logger?.LogInformation(LoggerMessages.LevelExecutionCompleted, level.Level);
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
            _logger?.LogNodeExecution(node, "실행 시작");
            _context.SetNodeState(node, NodeExecutionState.Running);
            
            var startTime = DateTime.Now;
            await node.ProcessAsync();
            var duration = DateTime.Now - startTime;
            
            _context.SetNodeState(node, NodeExecutionState.Completed);
            _logger?.LogNodeExecution(node, "실행 완료");
            _logger?.LogPerformance($"Node:{node.Name}", duration);
        }
        catch (Exception ex)
        {
            var state = NodeExecutionState.Failed;
            _context.SetNodeState(node, state);
            
            _logger?.LogNodeError(node, ex, "실행 실패");
            throw new NodeExecutionException(
                $"노드 '{node.Name}' 실행이 실패했습니다.", 
                ex, 
                node);
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