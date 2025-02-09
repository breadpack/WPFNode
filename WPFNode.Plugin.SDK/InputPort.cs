using System;
using WPFNode.Abstractions;

namespace WPFNode.Plugin.SDK;

public class InputPort<T> : PortBase, IPort<T>
{
    public InputPort(string name) : base(name, typeof(T), true)
    {
    }

    public new T? Value
    {
        get => base.Value == null ? default : (T)base.Value;
        set => base.Value = value;
    }

    public T GetValueOrDefault(T defaultValue)
    {
        return Value ?? defaultValue;
    }

    public T GetValueOrDefault()
    {
        return Value ?? default(T)!;
    }

    public bool TryGetValue(out T? value)
    {
        value = default;
        if (!IsConnected || base.Value == null)
            return false;

        value = (T)base.Value;
        return true;
    }
} 