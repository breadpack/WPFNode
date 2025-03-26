using System.IO;
using System.Text.Json;
using System.Text;
using WPFNode.Interfaces;
using WPFNode.Models.Serialization;

namespace WPFNode.Utilities;

/// <summary>
/// 노드 복사/붙여넣기를 위한 확장 메서드 모음
/// </summary>
public static class NodeExtensions
{
    /// <summary>
    /// 노드를 JSON 문자열로 직렬화합니다.
    /// </summary>
    public static string ToJson(this INode node)
    {
        if (node is not IJsonSerializable serializableNode)
            throw new ArgumentException("Node must implement IJsonSerializable");
            
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        
        writer.WriteStartObject();
        serializableNode.WriteJson(writer);
        writer.WriteEndObject();
        
        writer.Flush();
        return Encoding.UTF8.GetString(stream.ToArray());
    }
    
    /// <summary>
    /// 여러 노드를 JSON 배열로 직렬화합니다.
    /// </summary>
    public static string NodesToJson(this IEnumerable<INode> nodes)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        
        writer.WriteStartArray();
        
        foreach (var node in nodes)
        {
            if (node is IJsonSerializable serializableNode)
            {
                writer.WriteStartObject();
                serializableNode.WriteJson(writer);
                writer.WriteEndObject();
            }
        }
        
        writer.WriteEndArray();
        writer.Flush();
        
        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
