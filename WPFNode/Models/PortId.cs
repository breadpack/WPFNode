namespace WPFNode.Models;

public readonly struct PortId : IEquatable<PortId>
{
    public Guid NodeId { get; }
    public bool IsInput { get; }
    public string Name { get; }

    public PortId(Guid nodeId, bool isInput, string name)
    {
        NodeId = nodeId;
        IsInput = isInput;
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    // 이전 버전과의 호환성을 위한 생성자
    [Obsolete("Name 기반 생성자를 사용하세요. 이 생성자는 역호환성을 위해 유지됩니다.")]
    public PortId(Guid nodeId, bool isInput, int index)
        : this(nodeId, isInput, index.ToString())
    {
    }

    public override bool Equals(object? obj)
    {
        return obj is PortId other && Equals(other);
    }

    public bool Equals(PortId other)
    {
        return NodeId.Equals(other.NodeId) && 
               IsInput == other.IsInput && 
               Name == other.Name;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(NodeId, IsInput, Name);
    }

    public static bool operator ==(PortId left, PortId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PortId left, PortId right)
    {
        return !(left == right);
    }

    public override string ToString()
    {
        return $"{NodeId}:{(IsInput ? "in" : "out")}[{Name}]";
    }
}
