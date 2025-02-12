using System;
using System.Text.Json.Serialization;
using WPFNode.Abstractions;

namespace WPFNode.Core.Models;

public class Connection : IConnection
{
    private bool _isEnabled = true;

    [JsonConstructor]
    public Connection(IOutputPort source, IInputPort target)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Target = target ?? throw new ArgumentNullException(nameof(target));
        Id = Guid.NewGuid();
    }

    [JsonPropertyName("id")]
    public Guid Id { get; }
    
    [JsonPropertyName("source")]
    public IOutputPort Source { get; }
    
    [JsonPropertyName("target")]
    public IInputPort Target { get; }
    
    [JsonPropertyName("isEnabled")]
    public bool IsEnabled 
    { 
        get => _isEnabled;
        set => _isEnabled = value;
    }

    [JsonPropertyName("isValid")]
    public bool IsValid => Source != null && Target != null && 
                          Target.DataType.IsAssignableFrom(Source.DataType);
} 