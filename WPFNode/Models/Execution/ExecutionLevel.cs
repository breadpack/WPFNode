namespace WPFNode.Models.Execution;

/// <summary>
/// 실행 레벨을 나타내는 클래스
/// </summary>
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