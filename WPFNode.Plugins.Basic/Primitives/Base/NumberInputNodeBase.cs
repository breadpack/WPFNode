using WPFNode.Abstractions;
using WPFNode.Controls.Behaviors;
using WPFNode.Core.Models;
using WPFNode.Core.Models.Properties;

namespace WPFNode.Plugins.Basic.Primitives.Base;

public abstract class NumberInputNodeBase<T> : InputNodeBase<T>, INumberInputNode where T : struct
{
    private readonly OutputPort<T> _output;
    
    public OutputPort<T> Output => _output;
    

    protected NumberInputNodeBase(INodeCanvas canvas) : base(canvas)
    {
    }

    public void Increment() => OnIncrement();
    public void Decrement() => OnDecrement();

    protected abstract void OnIncrement();
    protected abstract void OnDecrement();
} 