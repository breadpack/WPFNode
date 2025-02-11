using System;
using WPFNode.Abstractions;

namespace WPFNode.Core.Models;

public class Connection : IConnection
{
    private bool _isEnabled = true;

    public Connection(IPort source, IPort target)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Target = target ?? throw new ArgumentNullException(nameof(target));
        Id = Guid.NewGuid();
    }

    public Guid Id { get; }
    public IPort Source { get; }
    public IPort Target { get; }
    
    public bool IsEnabled 
    { 
        get => _isEnabled;
        set => _isEnabled = value;
    }

    public bool IsValid => Source != null && Target != null && 
                          Target.DataType.IsAssignableFrom(Source.DataType);
} 