using System.Text.Json;
using System.Text.Json.Serialization;
using WPFNode.Core.Services;
using System.Reflection;
using WPFNode.Core.Interfaces;

namespace WPFNode.Core.Models.Serialization;

public class NodeCanvasJsonConverter : JsonConverter<NodeCanvas>
{
    private readonly INodePluginService _pluginService;
    private readonly List<NodeBase> _nodes = new();

    public NodeCanvasJsonConverter() {
        _pluginService = NodeServices.PluginService;
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
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected PropertyName token");
            }

            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName?.ToLower())
            {
                case "connections":
                    ReadConnections(ref reader, connections);
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        // 연결 생성
        foreach (var (sourceNodeId, sourcePortIndex, targetNodeId, targetPortIndex) in connections)
        {
            if (nodeDict.TryGetValue(sourceNodeId, out var sourceNode) &&
                nodeDict.TryGetValue(targetNodeId, out var targetNode))
            {
                var sourcePort = sourceNode.OutputPorts.ElementAtOrDefault(sourcePortIndex);
                var targetPort = targetNode.InputPorts.ElementAtOrDefault(targetPortIndex);

                if (sourcePort != null && targetPort != null)
                {
                    canvas.Connect(sourcePort, targetPort);
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

            string? typeString = null;
            Guid nodeId = Guid.Empty;
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

                switch (propertyName?.ToLower())
                {
                    case "$type":
                        typeString = reader.GetString();
                        break;
                    case "id":
                        nodeId = Guid.Parse(reader.GetString() ?? throw new JsonException("Node Id cannot be null"));
                        break;
                    case "x":
                        x = reader.GetDouble();
                        break;
                    case "y":
                        y = reader.GetDouble();
                        break;
                    default:
                        properties[propertyName ?? string.Empty] = JsonElement.ParseValue(ref reader);
                        break;
                }
            }

            if (string.IsNullOrEmpty(typeString))
            {
                throw new JsonException("Node type information is missing");
            }

            var nodeType = Type.GetType(typeString);
            if (nodeType == null)
            {
                throw new JsonException($"Could not find type {typeString}");
            }

            try
            {
                var node = (NodeBase)canvas.CreateNode(nodeType, x, y);
                node.Id = nodeId;

                // Value 속성을 먼저 설정
                if (properties.TryGetValue("value", out var valueElement))
                {
                    var valueProperty = nodeType.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
                    if (valueProperty != null)
                    {
                        try
                        {
                            var value = JsonSerializer.Deserialize(valueElement.GetRawText(), valueProperty.PropertyType, options);
                            valueProperty.SetValue(node, value);
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
                            property.SetValue(node, value);
                        }
                        catch (JsonException ex)
                        {
                            throw new JsonException($"Failed to set property {prop.Key} on node type {nodeType.Name}", ex);
                        }
                    }
                }

                // 포트 정보 복원
                if (properties.TryGetValue("InputPorts", out var inputPortsElement))
                {
                    var inputPorts = JsonSerializer.Deserialize<List<PortInfo>>(inputPortsElement.GetRawText(), options);
                    if (inputPorts != null)
                    {
                        foreach (var portInfo in inputPorts)
                        {
                            var port = node.InputPorts.FirstOrDefault(p => p.Id == portInfo.Id);
                            if (port != null)
                            {
                                port.Name = portInfo.Name;
                            }
                        }
                    }
                }

                if (properties.TryGetValue("OutputPorts", out var outputPortsElement))
                {
                    var outputPorts = JsonSerializer.Deserialize<List<PortInfo>>(outputPortsElement.GetRawText(), options);
                    if (outputPorts != null)
                    {
                        foreach (var portInfo in outputPorts)
                        {
                            var port = node.OutputPorts.FirstOrDefault(p => p.Id == portInfo.Id);
                            if (port != null)
                            {
                                port.Name = portInfo.Name;
                            }
                        }
                    }
                }

                nodeDict[nodeId] = node;
            }
            catch (Exception ex)
            {
                throw new JsonException($"Failed to create node of type {nodeType.Name}", ex);
            }
        }
    }

    private class PortInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
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
        foreach (var node in canvas.SerializableNodes)
        {
            writer.WriteStartObject();
            
            writer.WriteString("$type", node.GetType().AssemblyQualifiedName);
            writer.WriteString("Id", node.Id.ToString());
            writer.WriteNumber("X", node.X);
            writer.WriteNumber("Y", node.Y);

            // 입력 포트 직렬화
            writer.WriteStartArray("InputPorts");
            foreach (var port in node.InputPorts)
            {
                writer.WriteStartObject();
                writer.WriteString("Id", port.Id.ToString());
                writer.WriteString("Name", port.Name);
                writer.WriteString("DataType", port.DataType.AssemblyQualifiedName);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            // 출력 포트 직렬화
            writer.WriteStartArray("OutputPorts");
            foreach (var port in node.OutputPorts)
            {
                writer.WriteStartObject();
                writer.WriteString("Id", port.Id.ToString());
                writer.WriteString("Name", port.Name);
                writer.WriteString("DataType", port.DataType.AssemblyQualifiedName);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            // 노드의 추가 속성들 직렬화
            foreach (var prop in node.GetType().GetProperties())
            {
                // Id, X, Y, InputPorts, OutputPorts는 이미 별도로 처리됨
                if (prop.Name is "Id" or "X" or "Y" or "InputPorts" or "OutputPorts")
                    continue;

                // InputPort<T>, OutputPort<T> 타입의 프로퍼티는 제외
                if (prop.PropertyType.IsGenericType && 
                    (prop.PropertyType.GetGenericTypeDefinition() == typeof(InputPort<>) ||
                     prop.PropertyType.GetGenericTypeDefinition() == typeof(OutputPort<>)))
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