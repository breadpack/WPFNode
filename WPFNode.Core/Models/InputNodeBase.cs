namespace WPFNode.Core.Models;

public abstract class InputNodeBase<T> : NodeBase
{
    protected readonly OutputPort<T> _output;
    protected T _value;

    protected InputNodeBase()
    {
        _output = new OutputPort<T>("Value", this);
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

    protected override void InitializePorts() {
        RegisterOutputPort(_output);
    }

    public override Task ProcessAsync()
    {
        _output.Value = _value;
        return Task.CompletedTask;
    }
} 