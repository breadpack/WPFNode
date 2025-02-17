using WPFNode.Behaviors;
using WPFNode.Interfaces;
using WPFNode.Models;

namespace WPFNode.Plugins.Basic.Primitives.Base;

public abstract class NumberInputNodeBase<T> : InputNodeBase<T>, INumberInputNode where T : struct
{
    private readonly OutputPort<T> _output;
    
    public OutputPort<T> Output => _output;
    

    protected NumberInputNodeBase(INodeCanvas canvas, Guid id) : base(canvas, id)
    {
    }

    public void Increment() => OnIncrement();
    public void Decrement() => OnDecrement();

    protected abstract void OnIncrement();
    protected abstract void OnDecrement();
} 