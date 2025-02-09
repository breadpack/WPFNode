using System;

namespace WPFNode.Abstractions;

public class Connection
{
    public Connection(IPort source, IPort target)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Target = target ?? throw new ArgumentNullException(nameof(target));
        Id = Guid.NewGuid();
    }

    public Guid Id { get; }
    public IPort Source { get; }
    public IPort Target { get; }
} 