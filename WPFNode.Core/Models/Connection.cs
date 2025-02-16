using System;
using System.Text.Json.Serialization;
using WPFNode.Abstractions;

namespace WPFNode.Core.Models;

public class Connection : IConnection
{
    private bool _isEnabled = true;

    [JsonConstructor]
    public Connection(IOutputPort source, IInputPort target)
        : this(Guid.NewGuid(), source, target)
    {
    }

    public Connection(Guid id, IOutputPort source, IInputPort target)
    {
        if (source.Node == null)
            throw new ArgumentException("Source port must be attached to a node", nameof(source));
        if (target.Node == null)
            throw new ArgumentException("Target port must be attached to a node", nameof(target));

        Id = id;
        Source = source;
        Target = target;
        
        // PortId 가져오기
        SourcePortId = source.Id;
        TargetPortId = target.Id;
    }

    [JsonPropertyName("id")]
    public Guid Id { get; }
    
    [JsonPropertyName("source")]
    public IOutputPort Source { get; }
    
    [JsonPropertyName("target")]
    public IInputPort Target { get; }

    [JsonPropertyName("sourcePortId")]
    public PortId SourcePortId { get; }

    [JsonPropertyName("targetPortId")]
    public PortId TargetPortId { get; }
    
    [JsonPropertyName("isEnabled")]
    public bool IsEnabled 
    { 
        get => _isEnabled;
        set => _isEnabled = value;
    }

    [JsonPropertyName("isValid")]
    public bool IsValid => Source != null && Target != null && 
                          Target.DataType.IsAssignableFrom(Source.DataType);

    public void Disconnect()
    {
        // 양쪽 포트에서 연결 제거
        Source.RemoveConnection(this);
        Target.RemoveConnection(this);
    }
} 