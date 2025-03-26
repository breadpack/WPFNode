using System.Text.Json;
using System.Text.Json.Serialization;
using WPFNode.Exceptions;
using WPFNode.Interfaces;

namespace WPFNode.Models;

public class Connection : IConnection
{
    private readonly NodeCanvas _nodeCanvas;
    private          bool       _isEnabled = true;

    [JsonConstructor]
    public Connection(NodeCanvas canvas, IOutputPort source, IInputPort target)
        : this(Guid.NewGuid(), canvas, source, target)
    {
    }

    public Connection(Guid guid, NodeCanvas nodeCanvas, IOutputPort source, IInputPort target)
    {
        if (source == null)
            throw new NodeConnectionException("소스 포트가 null입니다.");
        if (target == null)
            throw new NodeConnectionException("타겟 포트가 null입니다.");
        if (source.Node == null)
            throw new NodeConnectionException("소스 포트가 노드에 연결되어 있지 않습니다.", source, target);
        if (target.Node == null)
            throw new NodeConnectionException("타겟 포트가 노드에 연결되어 있지 않습니다.", source, target);
        _nodeCanvas = nodeCanvas;

        Guid = guid;
        Source = source;
        Target = target;
        
        // PortId 가져오기
        SourcePortId = source.Id;
        TargetPortId = target.Id;
    }

    [JsonPropertyName("id")]
    public Guid Guid { get; }
    
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

    public void Disconnect()
    {
        // 양쪽 포트에서 연결 제거
        _nodeCanvas.Disconnect(this);
    }

    public void WriteJson(Utf8JsonWriter writer)
    {
        writer.WriteString("Guid", Guid.ToString());
        
        // 기존 PortId 문자열도 유지 (역호환성)
        writer.WriteString("SourcePortId", Source.Id.ToString());
        writer.WriteString("TargetPortId", Target.Id.ToString());
        
        // 명시적인 Port 정보 추가
        writer.WriteString("SourceNodeId", Source.Node.Guid.ToString());
        writer.WriteString("SourcePortName", Source.Name);
        writer.WriteString("SourceIsInput", Source.IsInput.ToString());
        
        writer.WriteString("TargetNodeId", Target.Node.Guid.ToString());
        writer.WriteString("TargetPortName", Target.Name);
        writer.WriteString("TargetIsInput", Target.IsInput.ToString());
        
        writer.WriteBoolean("IsEnabled", IsEnabled);
    }

    public void ReadJson(JsonElement element, JsonSerializerOptions options)
    {
        // 가변 상태만 복원
        if (element.TryGetProperty("IsEnabled", out var isEnabledElement))
        {
            IsEnabled = isEnabledElement.GetBoolean();
        }
    }
}
