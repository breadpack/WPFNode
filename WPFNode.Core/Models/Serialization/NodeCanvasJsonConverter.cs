using System.Text.Json;
using System.Text.Json.Serialization;
using WPFNode.Core.Services;
using System.Reflection;
using WPFNode.Core.Interfaces;
using WPFNode.Abstractions.Constants;
using WPFNode.Abstractions;

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
                case "OffsetX":
                case "OffsetY":
                    ReadCanvasProperty(ref reader, canvas, propertyName);
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        RestoreConnections(canvas, nodeDict, connections);
        return canvas;
    }

    private void ReadCanvasProperty(ref Utf8JsonReader reader, NodeCanvas canvas, string propertyName)
    {
        var value = reader.GetDouble();
        switch (propertyName)
        {
            case "Scale":
                canvas.Scale = value;
                break;
            case "OffsetX":
                canvas.OffsetX = value;
                break;
            case "OffsetY":
                canvas.OffsetY = value;
                break;
        }
    }

    private void RestoreConnections(
        NodeCanvas canvas,
        Dictionary<Guid, NodeBase> nodeDict,
        List<(Guid sourceNodeId, int sourcePortIndex, Guid targetNodeId, int targetPortIndex)> connections)
    {
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
            ReadNode(ref reader, canvas, nodeDict, options);
        }
    }

    private void ReadNode(
        ref Utf8JsonReader reader,
        NodeCanvas canvas,
        Dictionary<Guid, NodeBase> nodeDict,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject token for node");
        }

        var nodeInfo = ReadNodeInfo(ref reader);
        var node = CreateNode(canvas, nodeInfo, options);
        
        RestoreNodeProperties(node, nodeInfo.Properties, options);
        RestoreNodePorts(node, nodeInfo.Properties, options);
        
        nodeDict[node.Id] = node;
    }

    private (string? TypeString, Guid Id, double X, double Y, Dictionary<string, JsonElement> Properties) ReadNodeInfo(
        ref Utf8JsonReader reader)
    {
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
                case "properties":
                    properties["Properties"] = JsonElement.ParseValue(ref reader);
                    break;
                default:
                    properties[propertyName ?? string.Empty] = JsonElement.ParseValue(ref reader);
                    break;
            }
        }

        return (typeString, nodeId, x, y, properties);
    }

    private NodeBase CreateNode(
        NodeCanvas canvas,
        (string? TypeString, Guid Id, double X, double Y, Dictionary<string, JsonElement> Properties) nodeInfo,
        JsonSerializerOptions options)
    {
        if (string.IsNullOrEmpty(nodeInfo.TypeString))
        {
            throw new JsonException("Node type information is missing");
        }

        var nodeType = Type.GetType(nodeInfo.TypeString);
        if (nodeType == null)
        {
            throw new JsonException($"Could not find type {nodeInfo.TypeString}");
        }

        try
        {
            var node = (NodeBase)canvas.CreateNode(nodeType, nodeInfo.X, nodeInfo.Y);
            node.Id = nodeInfo.Id;

            // Value 속성을 먼저 설정
            if (nodeInfo.Properties.TryGetValue("value", out var valueElement))
            {
                var valueProperty = nodeType.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
                if (valueProperty != null)
                {
                    SetPropertyValue(node, valueProperty, valueElement, options);
                    nodeInfo.Properties.Remove("value");
                }
            }

            return node;
        }
        catch (Exception ex)
        {
            throw new JsonException($"Failed to create node of type {nodeType.Name}", ex);
        }
    }

    private void RestoreNodeProperties(NodeBase node, Dictionary<string, JsonElement> properties, JsonSerializerOptions options)
    {
        if (properties.TryGetValue("Properties", out var propertiesElement))
        {
            ReadProperties(propertiesElement, node, node.GetType(), options);
            properties.Remove("Properties");
        }

        // 나머지 속성 설정
        foreach (var prop in properties)
        {
            var property = node.GetType().GetProperty(prop.Key, BindingFlags.Public | BindingFlags.Instance);
            if (property != null && property.CanWrite)
            {
                SetPropertyValue(node, property, prop.Value, options);
            }
        }
    }

    private void RestoreNodePorts(NodeBase node, Dictionary<string, JsonElement> properties, JsonSerializerOptions options)
    {
        if (properties.TryGetValue("InputPorts", out var inputPortsElement))
        {
            ReadPorts<IInputPort>(inputPortsElement, node.InputPorts, options);
        }

        if (properties.TryGetValue("OutputPorts", out var outputPortsElement))
        {
            ReadPorts<IOutputPort>(outputPortsElement, node.OutputPorts, options);
        }
    }

    private void ReadPorts<T>(JsonElement portsElement, IReadOnlyList<T> ports, JsonSerializerOptions options) where T : IPort
    {
        var portInfos = JsonSerializer.Deserialize<List<PortInfo>>(portsElement.GetRawText(), options);
        if (portInfos != null)
        {
            foreach (var portInfo in portInfos)
            {
                var port = ports.FirstOrDefault(p => p.Id == portInfo.Id);
                if (port != null)
                {
                    port.Name = portInfo.Name;
                }
            }
        }
    }

    private void ReadProperties(JsonElement propertiesElement, NodeBase node, Type nodeType, JsonSerializerOptions options)
    {
        var nodePropertiesDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(propertiesElement.GetRawText(), options);
        if (nodePropertiesDict != null)
        {
            foreach (var nodeProp in nodePropertiesDict)
            {
                var propObj = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(nodeProp.Value.GetRawText(), options);
                if (propObj != null && node.Properties.ContainsKey(nodeProp.Key))
                {
                    var nodeProperty = node.Properties[nodeProp.Key];
                    if (propObj.TryGetValue("Value", out var propValueElement))
                    {
                        try
                        {
                            var propValue = JsonSerializer.Deserialize(propValueElement.GetRawText(), nodeProperty.GetType().GetProperty("Value")?.PropertyType ?? typeof(object), options);
                            nodeProperty.SetValue(propValue);
                        }
                        catch (JsonException ex)
                        {
                            throw new JsonException($"Failed to set value for property {nodeProp.Key} on node type {nodeType.Name}", ex);
                        }
                    }
                }
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

        WriteCanvasProperties(writer, canvas);
        WriteNodes(writer, canvas, options);
        WriteConnections(writer, canvas);

        writer.WriteEndObject();
    }

    private void WriteCanvasProperties(Utf8JsonWriter writer, NodeCanvas canvas)
    {
        writer.WriteNumber("Scale", canvas.Scale);
        writer.WriteNumber("OffsetX", canvas.OffsetX);
        writer.WriteNumber("OffsetY", canvas.OffsetY);
    }

    private void WriteNodes(Utf8JsonWriter writer, NodeCanvas canvas, JsonSerializerOptions options)
    {
        writer.WriteStartArray("Nodes");
        foreach (var node in canvas.SerializableNodes)
        {
            WriteNode(writer, node, options);
        }
        writer.WriteEndArray();
    }

    private void WriteNode(Utf8JsonWriter writer, NodeBase node, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        
        // 기본 정보
        writer.WriteString("$type", node.GetType().AssemblyQualifiedName);
        writer.WriteString("Id", node.Id.ToString());
        writer.WriteNumber("X", node.X);
        writer.WriteNumber("Y", node.Y);

        // Properties
        WriteNodeProperties(writer, node, options);

        // Ports
        WriteNodePorts(writer, node);

        writer.WriteEndObject();
    }

    private void WriteNodeProperties(Utf8JsonWriter writer, NodeBase node, JsonSerializerOptions options)
    {
        if (!node.Properties.Any()) return;

        writer.WritePropertyName("Properties");
        writer.WriteStartObject();
        
        foreach (var property in node.Properties)
        {
            writer.WritePropertyName(property.Key);
            writer.WriteStartObject();
            
            writer.WriteString("DisplayName", property.Value.DisplayName);
            writer.WriteNumber("ControlType", (int)property.Value.ControlType);
            
            if (property.Value.Format != null)
                writer.WriteString("Format", property.Value.Format);
                
            writer.WriteBoolean("CanConnectToPort", property.Value.CanConnectToPort);
            
            if (property.Value.GetValue() != null)
            {
                writer.WritePropertyName("Value");
                JsonSerializer.Serialize(writer, property.Value.GetValue(), options);
            }
            
            writer.WriteEndObject();
        }
        
        writer.WriteEndObject();
    }

    private void WriteNodePorts(Utf8JsonWriter writer, NodeBase node)
    {
        // 입력 포트
        writer.WriteStartArray("InputPorts");
        foreach (var port in node.InputPorts)
        {
            WritePort(writer, port);
        }
        writer.WriteEndArray();

        // 출력 포트
        writer.WriteStartArray("OutputPorts");
        foreach (var port in node.OutputPorts)
        {
            WritePort(writer, port);
        }
        writer.WriteEndArray();
    }

    private void WritePort(Utf8JsonWriter writer, IPort port)
    {
        writer.WriteStartObject();
        writer.WriteString("Id", port.Id.ToString());
        writer.WriteString("Name", port.Name);
        writer.WriteString("DataType", port.DataType.AssemblyQualifiedName);
        writer.WriteEndObject();
    }

    private void WriteConnections(Utf8JsonWriter writer, NodeCanvas canvas)
    {
        writer.WriteStartArray("Connections");
        foreach (var connection in canvas.Connections)
        {
            WriteConnection(writer, connection);
        }
        writer.WriteEndArray();
    }

    private void WriteConnection(Utf8JsonWriter writer, IConnection connection)
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

    /// <summary>
    /// JSON 요소를 안전하게 역직렬화하는 헬퍼 메서드
    /// </summary>
    private T? DeserializeElement<T>(JsonElement element, JsonSerializerOptions options)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(element.GetRawText(), options);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    /// <summary>
    /// 속성 값을 안전하게 설정하는 헬퍼 메서드
    /// </summary>
    private void SetPropertyValue(object target, PropertyInfo property, JsonElement element, JsonSerializerOptions options)
    {
        try
        {
            var value = JsonSerializer.Deserialize(element.GetRawText(), property.PropertyType, options);
            property.SetValue(target, value);
        }
        catch (JsonException ex)
        {
            throw new JsonException($"Failed to set property {property.Name} on type {target.GetType().Name}", ex);
        }
    }
} 