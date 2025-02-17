using WPFNode.Behaviors;
using WPFNode.Interfaces;
using WPFNode.Models;

namespace WPFNode.Plugins.Basic.Primitives.Base;

public abstract class NumberInputNodeBase<T> : InputNodeBase<T>, INumberInputNode where T : struct
{
    protected NumberInputNodeBase(INodeCanvas canvas, Guid id) : base(canvas, id)
    {
    }

    public void Increment() => OnIncrement();
    public void Decrement() => OnDecrement();

    protected abstract void OnIncrement();
    protected abstract void OnDecrement();
} 