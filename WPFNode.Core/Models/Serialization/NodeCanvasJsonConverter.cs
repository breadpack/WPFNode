using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Linq;
using WPFNode.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace WPFNode.Core.Models.Serialization;

public class NodeCanvasJsonConverter : JsonConverter<NodeCanvas>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly NodePluginService _pluginService;
    private readonly List<NodeBase> _nodes = new();

    public NodeCanvasJsonConverter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _pluginService = serviceProvider.GetRequiredService<NodePluginService>();
    }

    [JsonPropertyName("nodes")]
    public List<NodeBase> SerializableNodes => _nodes;

    public override NodeCanvas Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject token");
        }

        var canvas = new NodeCanvas();
        var nodeDict = new Dictionary<Guid, NodeBase>();
        var connections = new List<(Guid sourceNodeId, int sourcePortIndex, Guid targetNodeId, int targetPortIndex)>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected PropertyName token");
            }

            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName)
            {
                case "Nodes":
                    ReadNodes(ref reader, canvas, nodeDict, options);
                    break;
                case "Connections":
                    ReadConnections(ref reader, connections);
                    break;
                case "Scale":
                    canvas.Scale = reader.GetDouble();
                    break;
                case "OffsetX":
                    canvas.OffsetX = reader.GetDouble();
                    break;
                case "OffsetY":
                    canvas.OffsetY = reader.GetDouble();
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        // 연결 복원
        foreach (var conn in connections)
        {
            if (nodeDict.TryGetValue(conn.sourceNodeId, out var sourceNode) &&
                nodeDict.TryGetValue(conn.targetNodeId, out var targetNode))
            {
                if (sourceNode.OutputPorts.Count > conn.sourcePortIndex &&
                    targetNode.InputPorts.Count > conn.targetPortIndex)
                {
                    canvas.Connect(
                        sourceNode.OutputPorts[conn.sourcePortIndex],
                        targetNode.InputPorts[conn.targetPortIndex]);
                }
            }
        }

        return canvas;
    }

    private void ReadNodes(
        ref Utf8JsonReader reader,
        NodeCanvas canvas,
        Dictionary<Guid, NodeBase> nodeDict,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Expected StartArray token for nodes");
        }

        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected StartObject token for node");
            }

            var nodeTypeProperty = "";
            var nodeId = Guid.Empty;
            double x = 0, y = 0;
            var properties = new Dictionary<string, JsonElement>();

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException("Expected PropertyName token");
                }

                var propertyName = reader.GetString();
                reader.Read();

                switch (propertyName)
                {
                    case "$type":
                        nodeTypeProperty = reader.GetString() ?? "";
                        break;
                    case "Id":
                        nodeId = reader.GetGuid();
                        break;
                    case "X":
                        x = reader.GetDouble();
                        break;
                    case "Y":
                        y = reader.GetDouble();
                        break;
                    default:
                        // 다른 속성들은 나중에 처리하기 위해 저장
                        properties[propertyName ?? ""] = JsonElement.ParseValue(ref reader);
                        break;
                }
            }

            // 필수 필드 검증
            if (string.IsNullOrEmpty(nodeTypeProperty))
            {
                throw new JsonException("Node type information ($type) is missing");
            }
            if (nodeId == Guid.Empty)
            {
                throw new JsonException("Node Id is missing or invalid");
            }

            // 노드 타입 찾기
            var nodeType = Type.GetType(nodeTypeProperty);
            if (nodeType == null)
            {
                // 플러그인 서비스에서 등록된 타입 중에서 찾기
                var typeName = nodeTypeProperty.Split(',')[0].Trim();
                nodeType = _pluginService.NodeTypes.FirstOrDefault(t => t.FullName == typeName);
                
                if (nodeType == null)
                {
                    throw new JsonException($"Cannot find type: {nodeTypeProperty}");
                }
            }

            var createdNode = canvas.CreateNode(nodeType, x, y);
            ((NodeBase)createdNode).Id = nodeId;
            nodeDict[nodeId] = (NodeBase)createdNode;

            // 노드 초기화
            createdNode.Initialize();

            // Value 속성을 먼저 설정
            // 1. Value 속성은 출력 포트의 값과 직접적으로 연결되어 있음
            //    - InputNodeBase<T>의 Value 속성이 변경될 때 _output.Value도 함께 업데이트됨
            //    - 다른 속성들을 먼저 설정하면 Value 설정 시 출력 포트 값이 덮어써질 수 있음
            // 2. 다른 속성들이 Value에 의존할 수 있음
            //    - 노드의 표시 상태나 계산된 속성들이 Value를 기반으로 결정될 수 있음
            //    - Value를 먼저 설정하여 다른 속성들이 올바른 초기 상태를 가질 수 있도록 함
            // 3. 초기화 순서의 중요성
            //    - 노드 그래프의 직렬화/역직렬화 시 상태가 정확하게 복원되어야 함
            //    - Value와 출력 포트의 동기화가 중요함
            if (properties.TryGetValue("value", out var valueElement))
            {
                var valueProperty = nodeType.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
                if (valueProperty != null)
                {
                    try
                    {
                        var value = JsonSerializer.Deserialize(valueElement.GetRawText(), valueProperty.PropertyType, options);
                        valueProperty.SetValue(createdNode, value);
                        properties.Remove("value");
                    }
                    catch (JsonException ex)
                    {
                        throw new JsonException($"Failed to set Value property on node type {nodeType.Name}", ex);
                    }
                }
            }

            // 나머지 속성 설정
            foreach (var prop in properties)
            {
                var property = nodeType.GetProperty(prop.Key, BindingFlags.Public | BindingFlags.Instance);
                if (property != null && property.CanWrite)
                {
                    try
                    {
                        var value = JsonSerializer.Deserialize(prop.Value.GetRawText(), property.PropertyType, options);
                        property.SetValue(createdNode, value);
                    }
                    catch (JsonException ex)
                    {
                        throw new JsonException($"Failed to set property {prop.Key} on node type {nodeType.Name}", ex);
                    }
                }
            }
        }
    }

    private void ReadConnections(
        ref Utf8JsonReader reader,
        List<(Guid sourceNodeId, int sourcePortIndex, Guid targetNodeId, int targetPortIndex)> connections)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Expected StartArray token for connections");
        }

        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected StartObject token for connection");
            }

            Guid sourceNodeId = Guid.Empty, targetNodeId = Guid.Empty;
            int sourcePortIndex = -1, targetPortIndex = -1;

            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException("Expected PropertyName token");
                }

                var propertyName = reader.GetString()?.ToLower();
                reader.Read();

                switch (propertyName)
                {
                    case "sourcenodeid":
                        sourceNodeId = reader.GetGuid();
                        break;
                    case "sourceportindex":
                        sourcePortIndex = reader.GetInt32();
                        break;
                    case "targetnodeid":
                        targetNodeId = reader.GetGuid();
                        break;
                    case "targetportindex":
                        targetPortIndex = reader.GetInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            if (sourceNodeId != Guid.Empty && targetNodeId != Guid.Empty &&
                sourcePortIndex >= 0 && targetPortIndex >= 0)
            {
                connections.Add((sourceNodeId, sourcePortIndex, targetNodeId, targetPortIndex));
            }
        }
    }

    public override void Write(Utf8JsonWriter writer, NodeCanvas canvas, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        // 기본 속성
        writer.WriteNumber("Scale", canvas.Scale);
        writer.WriteNumber("OffsetX", canvas.OffsetX);
        writer.WriteNumber("OffsetY", canvas.OffsetY);

        // 노드 직렬화
        writer.WriteStartArray("Nodes");
        foreach (var node in canvas.Nodes)
        {
            writer.WriteStartObject();
            
            writer.WriteString("$type", node.GetType().AssemblyQualifiedName);
            writer.WriteString("Id", node.Id.ToString());
            writer.WriteNumber("X", node.X);
            writer.WriteNumber("Y", node.Y);

            // 노드의 추가 속성들 직렬화
            foreach (var prop in node.GetType().GetProperties())
            {
                if (prop.Name is "Id" or "X" or "Y" or "InputPorts" or "OutputPorts")
                    continue;

                var value = prop.GetValue(node);
                if (value != null)
                {
                    writer.WritePropertyName(prop.Name);
                    JsonSerializer.Serialize(writer, value, prop.PropertyType, options);
                }
            }

            writer.WriteEndObject();
        }
        writer.WriteEndArray();

        // 연결 직렬화
        writer.WriteStartArray("Connections");
        foreach (var connection in canvas.Connections)
        {
            writer.WriteStartObject();
            
            var sourceNode = (NodeBase)connection.Source.Node;
            var targetNode = (NodeBase)connection.Target.Node;
            
            var sourcePortIndex = sourceNode.OutputPorts.ToList().IndexOf(connection.Source);
            var targetPortIndex = targetNode.InputPorts.ToList().IndexOf(connection.Target);

            writer.WriteString("SourceNodeId", sourceNode.Id.ToString());
            writer.WriteNumber("SourcePortIndex", sourcePortIndex);
            writer.WriteString("TargetNodeId", targetNode.Id.ToString());
            writer.WriteNumber("TargetPortIndex", targetPortIndex);

            writer.WriteEndObject();
        }
        writer.WriteEndArray();

        writer.WriteEndObject();
    }
} 