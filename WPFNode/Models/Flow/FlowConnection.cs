using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using WPFNode.Interfaces;
using WPFNode.Interfaces.Flow;

namespace WPFNode.Models.Flow;

/// <summary>
/// 흐름 포트 간의 연결을 나타내는 클래스입니다.
/// </summary>
public class FlowConnection : IFlowConnection, IJsonSerializable
{
    /// <summary>
    /// 연결 고유 ID
    /// </summary>
    public Guid Guid { get; }

    /// <summary>
    /// 소스 흐름 출력 포트
    /// </summary>
    public IFlowOutPort Source { get; }

    /// <summary>
    /// 타겟 흐름 입력 포트
    /// </summary>
    public IFlowInPort Target { get; }

    /// <summary>
    /// 역직렬화를 위한 기본 생성자
    /// </summary>
    [JsonConstructor]
    private FlowConnection()
    {
        Guid = Guid.NewGuid();
    }

    /// <summary>
    /// 주어진 소스와 타겟 포트 간의 연결 생성
    /// </summary>
    /// <param name="id">연결 ID</param>
    /// <param name="source">소스 출력 포트</param>
    /// <param name="target">타겟 입력 포트</param>
    public FlowConnection(Guid id, IFlowOutPort source, IFlowInPort target)
    {
        Guid = id;
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Target = target ?? throw new ArgumentNullException(nameof(target));
    }

    /// <summary>
    /// 메타데이터: 소스 노드 ID
    /// </summary>
    [JsonIgnore]
    public Guid SourceNodeId => Source?.Node.Guid ?? Guid.Empty;

    /// <summary>
    /// 메타데이터: 타겟 노드 ID
    /// </summary>
    [JsonIgnore]
    public Guid TargetNodeId => Target?.Node.Guid ?? Guid.Empty;

    /// <summary>
    /// 메타데이터: 소스 포트 이름
    /// </summary>
    [JsonIgnore]
    public string SourcePortName => GetPortName(Source);

    /// <summary>
    /// 메타데이터: 타겟 포트 이름
    /// </summary>
    [JsonIgnore]
    public string TargetPortName => GetPortName(Target);

    /// <summary>
    /// 포트의 이름을 가져옵니다.
    /// </summary>
    /// <param name="port">대상 포트</param>
    /// <returns>포트 이름</returns>
    private string GetPortName(IFlowPort port)
    {
        if (port == null)
            return string.Empty;

        // Name 속성이 있는 경우 사용
        if (port is FlowPort flowPort)
            return flowPort.Name;

        // 속성을 통해 찾기
        var portType = port.GetType();
        foreach (var prop in port.Node.GetType().GetProperties())
        {
            if (prop.PropertyType.IsAssignableFrom(portType))
            {
                var value = prop.GetValue(port.Node);
                if (ReferenceEquals(value, port))
                {
                    return prop.Name;
                }
            }
        }

        // 인덱스 기반 구분
        if (port.PortType == FlowPortType.FlowIn)
        {
            string prefix = "FlowIn_";
            var ports = ((FlowNodeBase)port.Node).FlowInPorts;
            
            int index = 0;
            foreach (var p in ports)
            {
                if (ReferenceEquals(p, port))
                    return $"{prefix}{index}";
                index++;
            }
        }
        else
        {
            string prefix = "FlowOut_";
            var ports = ((FlowNodeBase)port.Node).FlowOutPorts;
            
            int index = 0;
            foreach (var p in ports)
            {
                if (ReferenceEquals(p, port))
                    return $"{prefix}{index}";
                index++;
            }
        }

        return port.PortType == FlowPortType.FlowIn ? "FlowIn_Unknown" : "FlowOut_Unknown";
    }

    /// <inheritdoc />
    public void WriteJson(Utf8JsonWriter writer)
    {
        writer.WriteString("Guid", Guid.ToString());
        writer.WriteString("Type", GetType().AssemblyQualifiedName);

        // 소스 및 타겟 노드/포트 정보
        writer.WriteString("SourceNodeId", SourceNodeId.ToString());
        writer.WriteString("SourcePortName", SourcePortName);
        writer.WriteString("SourceIsFlowPort", "true");

        writer.WriteString("TargetNodeId", TargetNodeId.ToString());
        writer.WriteString("TargetPortName", TargetPortName);
        writer.WriteString("TargetIsFlowPort", "true");
    }

    /// <inheritdoc />
    public void ReadJson(JsonElement element, JsonSerializerOptions options)
    {
        // 역직렬화는 NodeCanvas에서 수행됨
    }
}
