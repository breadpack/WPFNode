using WPFNode.Abstractions;

namespace WPFNode.Core.Models;

public class OutputPort<T> : PortBase, IPort<T>
{
    public OutputPort(string name, INode node) : base(name, typeof(T), false, node)
    {
    }

    public new T? Value
    {
        get => base.Value == null ? default : (T)base.Value;
        set => base.Value = value;
    }
} 