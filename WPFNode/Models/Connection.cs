using System.Text.Json;
using System.Text.Json.Serialization;
using WPFNode.Exceptions;
using WPFNode.Interfaces;

namespace WPFNode.Models;

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
        if (source == null)
            throw new NodeConnectionException("소스 포트가 null입니다.");
        if (target == null)
            throw new NodeConnectionException("타겟 포트가 null입니다.");
        if (source.Node == null)
            throw new NodeConnectionException("소스 포트가 노드에 연결되어 있지 않습니다.", source, target);
        if (target.Node == null)
            throw new NodeConnectionException("타겟 포트가 노드에 연결되어 있지 않습니다.", source, target);

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

    public void WriteJson(Utf8JsonWriter writer)
    {
        writer.WriteString("Id", Id.ToString());
        writer.WriteString("SourcePortId", Source.Id.ToString());
        writer.WriteString("TargetPortId", Target.Id.ToString());
        writer.WriteBoolean("IsEnabled", IsEnabled);
    }

    public void ReadJson(JsonElement element)
    {
        // 가변 상태만 복원
        if (element.TryGetProperty("IsEnabled", out var isEnabledElement))
        {
            IsEnabled = isEnabledElement.GetBoolean();
        }
    }
} 