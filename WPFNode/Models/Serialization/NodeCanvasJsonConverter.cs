using System.Text.Json;
using System.Text.Json.Serialization;
using WPFNode.Interfaces;

namespace WPFNode.Models.Serialization;

public class PropertySerializationData
{
    public string Name { get; set; } = string.Empty;
    public string? ValueType { get; set; }
    public string? SerializedValue { get; set; }
    public bool CanConnectToPort { get; set; }
}

public class NodeCanvasJsonConverter : JsonConverter<NodeCanvas>
{
    public override NodeCanvas Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var canvasData = JsonDocument.ParseValue(ref reader).RootElement;
        var canvas = NodeCanvas.Create();
        var nodesById = new Dictionary<Guid, INode>();
        
        ReadNodes(canvasData, canvas, nodesById, options);
        ReadConnections(canvasData, canvas, nodesById);
        ReadGroups(canvasData, canvas, nodesById);
        
        return canvas;
    }

    private void ReadNodes(JsonElement canvasData, NodeCanvas canvas, Dictionary<Guid, INode> nodesById, JsonSerializerOptions options)
    {
        if (!canvasData.TryGetProperty("Nodes", out var nodesElement)) return;

        foreach (var nodeElement in nodesElement.EnumerateArray())
        {
            var node = CreateNodeFromJson(nodeElement, canvas);
            if (node != null)
            {
                nodesById[node.Id] = node;
                ReadNodeProperties(nodeElement, node, options);
            }
        }
    }

    private INode? CreateNodeFromJson(JsonElement nodeElement, NodeCanvas canvas)
    {
        if (!nodeElement.TryGetProperty("Id", out var idElement) || 
            !nodeElement.TryGetProperty("Type", out var typeElement) ||
            !nodeElement.TryGetProperty("X", out var xElement) ||
            !nodeElement.TryGetProperty("Y", out var yElement))
        {
            throw new JsonException("필수 노드 속성이 누락되었습니다.");
        }

        var nodeId = Guid.Parse(idElement.GetString()!);
        var nodeTypeString = typeElement.GetString();
        var nodeType = Type.GetType(nodeTypeString ?? string.Empty);
        
        if (nodeType == null)
        {
            throw new JsonException($"노드 타입을 찾을 수 없습니다: {nodeTypeString}");
        }

        var x = xElement.GetDouble();
        var y = yElement.GetDouble();
        
        var node = canvas.CreateNodeWithId(nodeId, nodeType, x, y);
        
        // 이름 복원
        if (nodeElement.TryGetProperty("Name", out var nameElement))
        {
            node.Name = nameElement.GetString() ?? node.Name;
        }
        
        return node;
    }

    private void ReadNodeProperties(JsonElement nodeElement, INode node, JsonSerializerOptions options)
    {
        if (!nodeElement.TryGetProperty("Properties", out var propertiesElement)) return;

        foreach (var propertyElement in propertiesElement.EnumerateArray())
        {
            var propertyData = DeserializePropertyData(propertyElement, options);
            if (propertyData != null)
            {
                ApplyPropertyData(node, propertyData);
            }
        }
    }

    private PropertySerializationData? DeserializePropertyData(JsonElement propertyElement, JsonSerializerOptions options)
    {
        try
        {
            var propertyBytes = System.Text.Encoding.UTF8.GetBytes(propertyElement.GetRawText());
            var propertyReader = new Utf8JsonReader(propertyBytes);
            return JsonSerializer.Deserialize<PropertySerializationData>(ref propertyReader, options);
        }
        catch
        {
            return null;
        }
    }

    private void ApplyPropertyData(INode node, PropertySerializationData propertyData)
    {
        if (string.IsNullOrEmpty(propertyData.Name) || 
            propertyData.ValueType == null || 
            !node.Properties.TryGetValue(propertyData.Name, out var property))
            return;

        // 포트 연결 가능 여부 설정
        property.CanConnectToPort = propertyData.CanConnectToPort;
        
        // 값 설정
        if (!string.IsNullOrEmpty(propertyData.SerializedValue))
        {
            try
            {
                ApplyPropertyValue(property, propertyData.ValueType, propertyData.SerializedValue);
            }
            catch (Exception ex)
            {
                throw new JsonException($"속성 값 설정 중 오류 발생: {propertyData.Name}", ex);
            }
        }
    }

    private void ApplyPropertyValue(INodeProperty property, string valueType, string serializedValue)
    {
        try
        {
            var type = Type.GetType(valueType);
            if (type != null)
            {
                var value = JsonSerializer.Deserialize(serializedValue, type);
                property.Value = value;
            }
        }
        catch
        {
            // 값 복원 실패 시 기본값 유지
        }
    }

    private void ReadConnections(JsonElement canvasData, NodeCanvas canvas, Dictionary<Guid, INode> nodesById)
    {
        if (!canvasData.TryGetProperty("Connections", out var connectionsElement)) return;

        foreach (var connectionElement in connectionsElement.EnumerateArray())
        {
            var connection = CreateConnectionFromJson(connectionElement, nodesById, canvas);
            if (connection != null)
            {
                // Connection은 canvas.ConnectWithId에서 자동으로 추가됨
            }
        }
    }

    private IConnection? CreateConnectionFromJson(JsonElement connectionElement, Dictionary<Guid, INode> nodesById, NodeCanvas canvas)
    {
        try
        {
            var connectionId = Guid.Parse(connectionElement.GetProperty("Id").GetString()!);
            var sourceNodeId = Guid.Parse(connectionElement.GetProperty("SourceNodeId").GetString()!);
            var sourcePortIndex = connectionElement.GetProperty("SourcePortIndex").GetInt32();
            var targetNodeId = Guid.Parse(connectionElement.GetProperty("TargetNodeId").GetString()!);
            var targetPortIndex = connectionElement.GetProperty("TargetPortIndex").GetInt32();

            if (nodesById.TryGetValue(sourceNodeId, out var sourceNode) && 
                nodesById.TryGetValue(targetNodeId, out var targetNode))
            {
                var sourcePort = sourceNode.OutputPorts.ElementAtOrDefault(sourcePortIndex);
                var targetPort = targetNode.InputPorts.ElementAtOrDefault(targetPortIndex);

                if (sourcePort != null && targetPort != null)
                {
                    return canvas.ConnectWithId(connectionId, sourcePort, targetPort);
                }
            }
        }
        catch
        {
            // 연결 생성 실패 시 무시
        }
        return null;
    }

    private void ReadGroups(JsonElement canvasData, NodeCanvas canvas, Dictionary<Guid, INode> nodesById)
    {
        if (!canvasData.TryGetProperty("Groups", out var groupsElement)) return;

        foreach (var groupElement in groupsElement.EnumerateArray())
        {
            var group = CreateGroupFromJson(groupElement, nodesById, canvas);
            if (group != null)
            {
                // Group은 canvas.CreateGroupWithId에서 자동으로 추가됨
            }
        }
    }

    private NodeGroup? CreateGroupFromJson(JsonElement groupElement, Dictionary<Guid, INode> nodesById, NodeCanvas canvas)
    {
        try
        {
            var groupId = Guid.Parse(groupElement.GetProperty("Id").GetString()!);
            var name = groupElement.GetProperty("Name").GetString();
            var nodeIds = groupElement.GetProperty("NodeIds")
                .EnumerateArray()
                .Select(e => Guid.Parse(e.GetString()!))
                .ToList();
            
            var nodes = nodeIds
                .Where(id => nodesById.ContainsKey(id))
                .Select(id => nodesById[id])
                .Cast<NodeBase>()
                .ToList();
            
            if (nodes.Any())
            {
                return canvas.CreateGroupWithId(groupId, nodes, name ?? "New Group");
            }
        }
        catch
        {
            // 그룹 생성 실패 시 무시
        }
        return null;
    }

    public override void Write(Utf8JsonWriter writer, NodeCanvas value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        
        WriteNodes(writer, value, options);
        WriteConnections(writer, value);
        WriteGroups(writer, value);
        
        writer.WriteEndObject();
    }

    private void WriteNodes(Utf8JsonWriter writer, NodeCanvas canvas, JsonSerializerOptions options)
    {
        writer.WriteStartArray("Nodes");
        foreach (var node in canvas.Nodes)
        {
            WriteNode(writer, node, options);
        }
        writer.WriteEndArray();
    }

    private void WriteNode(Utf8JsonWriter writer, INode node, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("Id", node.Id.ToString());
        writer.WriteString("Type", node.GetType().AssemblyQualifiedName);
        writer.WriteString("Name", node.Name);
        writer.WriteNumber("X", node.X);
        writer.WriteNumber("Y", node.Y);
        WriteNodeProperties(writer, node, options);
        writer.WriteEndObject();
    }

    private void WriteNodeProperties(Utf8JsonWriter writer, INode node, JsonSerializerOptions options)
    {
        writer.WriteStartArray("Properties");
        foreach (var property in node.Properties.Where(p => HasChangedFromDefault(p.Value)))
        {
            WriteProperty(writer, property.Key, property.Value);
        }
        writer.WriteEndArray();
    }

    private void WriteProperty(Utf8JsonWriter writer, string key, INodeProperty property)
    {
        writer.WriteStartObject();
        writer.WriteString("Name", key);
        writer.WriteString("ValueType", property.PropertyType.AssemblyQualifiedName);
        writer.WriteString("SerializedValue", JsonSerializer.Serialize(property.Value, property.PropertyType));
        writer.WriteBoolean("CanConnectToPort", property.CanConnectToPort);
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
        writer.WriteString("Id", connection.Id.ToString());
        writer.WriteString("SourceNodeId", connection.Source.Node!.Id.ToString());
        writer.WriteNumber("SourcePortIndex", connection.Source.GetPortIndex());
        writer.WriteString("TargetNodeId", connection.Target.Node!.Id.ToString());
        writer.WriteNumber("TargetPortIndex", connection.Target.GetPortIndex());
        writer.WriteEndObject();
    }

    private void WriteGroups(Utf8JsonWriter writer, NodeCanvas canvas)
    {
        writer.WriteStartArray("Groups");
        foreach (var group in canvas.Groups)
        {
            WriteGroup(writer, group);
        }
        writer.WriteEndArray();
    }

    private void WriteGroup(Utf8JsonWriter writer, NodeGroup group)
    {
        writer.WriteStartObject();
        writer.WriteString("Id", group.Id.ToString());
        writer.WriteString("Name", group.Name);
        writer.WriteStartArray("NodeIds");
        foreach (var node in group.Nodes)
        {
            writer.WriteStringValue(node.Id.ToString());
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private bool HasChangedFromDefault(INodeProperty property)
    {
        // 포트로 사용되는 경우
        if (property.CanConnectToPort)
            return true;

        // 값이 변경된 경우
        if (property.Value != null)
        {
            var defaultValue = property.PropertyType.IsValueType ? 
                Activator.CreateInstance(property.PropertyType) : null;
            return !Equals(property.Value, defaultValue);
        }

        return false;
    }
} 