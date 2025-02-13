using WPFNode.Abstractions;
using WPFNode.Controls.Behaviors;
using WPFNode.Core.Models;

namespace WPFNode.Plugins.Basic.Primitives.Base;

public abstract class NumberInputNodeBase<T> : InputNodeBase<T>, INumberInputNode where T : struct
{
    protected NumberInputNodeBase(INodeCanvas canvas) : base(canvas) { }

    public void Increment() => OnIncrement();
    public void Decrement() => OnDecrement();

    protected abstract void OnIncrement();
    protected abstract void OnDecrement();
} 