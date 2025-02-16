using WPFNode.Abstractions;

namespace WPFNode.Core.Models;

public abstract class InputNodeBase<T> : NodeBase
{
    protected readonly OutputPort<T> _output;
    protected T _value;
    
    public OutputPort<T> Result => _output;

    protected InputNodeBase(INodeCanvas canvas) : base(canvas) {
        _output = CreateOutputPort<T>("Value");
        _value  = default!;
    }

    public virtual T Value
    {
        get => _value;
        set
        {
            if (!EqualityComparer<T>.Default.Equals(_value, value))
            {
                _value = value;
                _output.Value = value;
                OnPropertyChanged();
            }
        }
    }

    public override Task ProcessAsync()
    {
        _output.Value = _value;
        return Task.CompletedTask;
    }
} 