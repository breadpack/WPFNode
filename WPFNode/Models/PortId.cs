namespace WPFNode.Models;

public readonly struct PortId : IEquatable<PortId>
{
    public Guid NodeId { get; }
    public bool IsInput { get; }
    public int Index { get; }

    public PortId(Guid nodeId, bool isInput, int index)
    {
        NodeId = nodeId;
        IsInput = isInput;
        Index = index;
    }

    public override bool Equals(object? obj)
    {
        return obj is PortId other && Equals(other);
    }

    public bool Equals(PortId other)
    {
        return NodeId.Equals(other.NodeId) && 
               IsInput == other.IsInput && 
               Index == other.Index;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(NodeId, IsInput, Index);
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
        return $"{NodeId}:{(IsInput ? "in" : "out")}[{Index}]";
    }
} 