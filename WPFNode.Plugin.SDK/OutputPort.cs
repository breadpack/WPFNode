using System;
using WPFNode.Abstractions;

namespace WPFNode.Plugin.SDK;

public class OutputPort<T> : PortBase, IPort<T>
{
    public OutputPort(string name) : base(name, typeof(T), false)
    {
    }

    public new T? Value
    {
        get => base.Value == null ? default : (T)base.Value;
        set => base.Value = value;
    }
} 