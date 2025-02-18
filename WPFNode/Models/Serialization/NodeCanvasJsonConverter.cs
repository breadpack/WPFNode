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
        try
        {
            var canvasData = JsonDocument.ParseValue(ref reader).RootElement;
            var canvas = NodeCanvas.Create();
            var nodesById = new Dictionary<Guid, INode>();
            
            // 순서대로 복원: 노드 -> 연결 -> 그룹
            ReadNodes(canvasData, canvas, nodesById, options);
            ReadConnections(canvasData, canvas, nodesById);
            ReadGroups(canvasData, canvas, nodesById);
            
            return canvas;
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new JsonException("노드 캔버스 역직렬화 중 오류 발생", ex);
        }
    }

    private void ReadNodes(JsonElement canvasData, NodeCanvas canvas, Dictionary<Guid, INode> nodesById, JsonSerializerOptions options)
    {
        if (!canvasData.TryGetProperty("Nodes", out var nodesElement)) return;

        var failedNodes = new List<(JsonElement Element, Exception Error, string Details)>();

        foreach (var nodeElement in nodesElement.EnumerateArray())
        {
            try
            {
                // 1. 노드 생성에 필요한 기본 정보 파싱
                if (!nodeElement.TryGetProperty("Type", out var typeElement))
                {
                    throw new JsonException("필수 노드 속성이 누락되었습니다.");
                }

                var nodeTypeString = typeElement.GetString();
                var nodeType = Type.GetType(nodeTypeString ?? string.Empty);
                
                if (nodeType == null)
                {
                    throw new JsonException($"노드 타입을 찾을 수 없습니다: {nodeTypeString}");
                }

                // 2. 노드 생성
                var nodeId = Guid.Parse(nodeElement.GetProperty("Id").GetString()!);
                var x = nodeElement.GetProperty("X").GetDouble();
                var y = nodeElement.GetProperty("Y").GetDouble();
                var node = canvas.CreateNodeWithId(nodeId, nodeType, x, y);
                
                if (node == null)
                {
                    throw new JsonException($"노드 생성에 실패했습니다: {nodeType.Name}");
                }
                
                nodesById[node.Id] = node;

                // 3. 노드 상태 복원
                if (node is IJsonSerializable serializable)
                {
                    serializable.ReadJson(nodeElement);
                }
            }
            catch (Exception ex)
            {
                var errorDetails = GetDetailedErrorMessage(ex, nodeElement);
                failedNodes.Add((nodeElement, ex, errorDetails));
            }
        }

        if (failedNodes.Any())
        {
            var errors = string.Join("\n", failedNodes.Select(f => 
                $"- 노드 복원 실패: {f.Element.GetProperty("Type").GetString() ?? "알 수 없는 타입"}\n" +
                $"  노드 정보: {f.Element.GetProperty("Name").GetString() ?? "알 수 없는 이름"} " +
                $"({f.Element.GetProperty("Type").GetString()?.Split(',')[0].Split('.').Last() ?? "알 수 없는 타입"})\n" +
                $"  오류 메시지: {f.Error.Message}\n" +
                $"  상세 정보: {f.Details}"));
            throw new JsonException($"일부 노드 복원 실패:\n{errors}");
        }
    }

    private string GetDetailedErrorMessage(Exception ex, JsonElement nodeElement)
    {
        var details = new List<string>();

        // 예외 체인 추적
        var currentEx = ex;
        while (currentEx != null)
        {
            details.Add($"[{currentEx.GetType().Name}] {currentEx.Message}");
            currentEx = currentEx.InnerException;
        }

        // 노드 정보 추가
        try
        {
            var nodeInfo = new List<string>();
            if (nodeElement.TryGetProperty("Id", out var idElement))
                nodeInfo.Add($"Id: {idElement.GetString()}");
            if (nodeElement.TryGetProperty("Name", out var nameElement))
                nodeInfo.Add($"Name: {nameElement.GetString()}");
            if (nodeElement.TryGetProperty("Type", out var typeElement))
                nodeInfo.Add($"Type: {typeElement.GetString()}");
            
            details.Add($"노드 데이터: {string.Join(", ", nodeInfo)}");
            
            // JSON 데이터 추가
            details.Add($"전체 JSON: {nodeElement.GetRawText()}");
        }
        catch (Exception)
        {
            details.Add("노드 데이터 파싱 중 추가 오류 발생");
        }

        return string.Join("\n  ", details);
    }

    private void ReadConnections(JsonElement canvasData, NodeCanvas canvas, Dictionary<Guid, INode> nodesById)
    {
        if (!canvasData.TryGetProperty("Connections", out var connectionsElement)) return;

        var failedConnections = new List<(JsonElement Element, Exception Error)>();

        foreach (var connectionElement in connectionsElement.EnumerateArray())
        {
            try
            {
                // 1. 연결 생성에 필요한 기본 정보 파싱
                var connectionId = Guid.Parse(connectionElement.GetProperty("Id").GetString()!);
                var sourceNodeId = Guid.Parse(connectionElement.GetProperty("SourceNodeId").GetString()!);
                var targetNodeId = Guid.Parse(connectionElement.GetProperty("TargetNodeId").GetString()!);
                var sourcePortIndex = connectionElement.GetProperty("SourcePortIndex").GetInt32();
                var targetPortIndex = connectionElement.GetProperty("TargetPortIndex").GetInt32();

                // 2. 노드와 포트 찾기
                if (!nodesById.TryGetValue(sourceNodeId, out var sourceNode))
                {
                    throw new JsonException($"소스 노드를 찾을 수 없습니다. (ID: {sourceNodeId})");
                }

                if (!nodesById.TryGetValue(targetNodeId, out var targetNode))
                {
                    throw new JsonException($"타겟 노드를 찾을 수 없습니다. (ID: {targetNodeId})");
                }

                var sourcePort = sourceNode.OutputPorts.ElementAtOrDefault(sourcePortIndex);
                var targetPort = targetNode.InputPorts.ElementAtOrDefault(targetPortIndex);

                if (sourcePort == null || targetPort == null)
                {
                    throw new JsonException(
                        $"포트를 찾을 수 없습니다. 소스: {sourceNode.Name}[{sourcePortIndex}], " +
                        $"타겟: {targetNode.Name}[{targetPortIndex}]");
                }

                // 3. 연결 생성
                var connection = canvas.ConnectWithId(connectionId, sourcePort, targetPort);
                if (connection == null)
                {
                    throw new JsonException("연결 생성에 실패했습니다.");
                }

                // 4. 연결 상태 복원
                if (connection is IJsonSerializable serializable)
                {
                    serializable.ReadJson(connectionElement);
                }
                else
                {
                    throw new JsonException($"연결 {connection.Id}이(가) IJsonSerializable을 구현하지 않았습니다.");
                }
            }
            catch (Exception ex)
            {
                failedConnections.Add((connectionElement, ex));
            }
        }

        if (failedConnections.Any())
        {
            var errors = string.Join("\n", failedConnections.Select(f => 
                $"- 연결 복원 실패: {f.Element.GetProperty("Id").GetString() ?? "알 수 없는 ID"}, 오류: {f.Error.Message}"));
            throw new JsonException($"일부 연결 복원 실패:\n{errors}");
        }
    }

    private void ReadGroups(JsonElement canvasData, NodeCanvas canvas, Dictionary<Guid, INode> nodesById)
    {
        if (!canvasData.TryGetProperty("Groups", out var groupsElement)) return;

        var failedGroups = new List<(JsonElement Element, Exception Error)>();

        foreach (var groupElement in groupsElement.EnumerateArray())
        {
            try
            {
                // 1. 그룹 생성에 필요한 기본 정보 파싱
                var groupId = Guid.Parse(groupElement.GetProperty("Id").GetString()!);
                var name = groupElement.GetProperty("Name").GetString() ?? "New Group";
                var nodeIds = groupElement.GetProperty("NodeIds")
                    .EnumerateArray()
                    .Select(e => Guid.Parse(e.GetString()!))
                    .ToList();

                // 2. 노드 찾기
                var nodes = nodeIds
                    .Where(id => nodesById.ContainsKey(id))
                    .Select(id => nodesById[id])
                    .Cast<NodeBase>()
                    .ToList();

                if (!nodes.Any())
                {
                    throw new JsonException("그룹에 포함된 유효한 노드가 없습니다.");
                }

                // 3. 그룹 생성
                var group = canvas.CreateGroupWithId(groupId, nodes, name);
                if (group == null)
                {
                    throw new JsonException("그룹 생성에 실패했습니다.");
                }

                // 4. 그룹 상태 복원
                if (group is IJsonSerializable serializable)
                {
                    serializable.ReadJson(groupElement);
                }
                else
                {
                    throw new JsonException($"그룹 {group.Name}이(가) IJsonSerializable을 구현하지 않았습니다.");
                }
            }
            catch (Exception ex)
            {
                failedGroups.Add((groupElement, ex));
            }
        }

        if (failedGroups.Any())
        {
            var errors = string.Join("\n", failedGroups.Select(f => 
                $"- 그룹 복원 실패: {f.Element.GetProperty("Name").GetString() ?? "알 수 없는 이름"}, 오류: {f.Error.Message}"));
            throw new JsonException($"일부 그룹 복원 실패:\n{errors}");
        }
    }

    public override void Write(Utf8JsonWriter writer, NodeCanvas value, JsonSerializerOptions options)
    {
        try
        {
            writer.WriteStartObject();
            
            WriteNodes(writer, value, options);
            WriteConnections(writer, value);
            WriteGroups(writer, value);
            
            writer.WriteEndObject();
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new JsonException("노드 캔버스 직렬화 중 오류 발생", ex);
        }
    }

    private void WriteNodes(Utf8JsonWriter writer, NodeCanvas canvas, JsonSerializerOptions options)
    {
        writer.WriteStartArray("Nodes");
        foreach (var node in canvas.Nodes)
        {
            try
            {
                if (node is IJsonSerializable serializable)
                {
                    writer.WriteStartObject();
                    serializable.WriteJson(writer);
                    writer.WriteEndObject();
                }
                else
                {
                    throw new JsonException($"노드 {node.Name}이(가) IJsonSerializable을 구현하지 않았습니다.");
                }
            }
            catch (Exception ex)
            {
                throw new JsonException($"노드 {node.Name} 직렬화 중 오류 발생", ex);
            }
        }
        writer.WriteEndArray();
    }

    private void WriteConnections(Utf8JsonWriter writer, NodeCanvas canvas)
    {
        writer.WriteStartArray("Connections");
        foreach (var connection in canvas.Connections)
        {
            try
            {
                if (connection is IJsonSerializable serializable)
                {
                    writer.WriteStartObject();
                    serializable.WriteJson(writer);
                    writer.WriteEndObject();
                }
                else
                {
                    throw new JsonException($"연결 {connection.Id}이(가) IJsonSerializable을 구현하지 않았습니다.");
                }
            }
            catch (Exception ex)
            {
                throw new JsonException($"연결 {connection.Id} 직렬화 중 오류 발생", ex);
            }
        }
        writer.WriteEndArray();
    }

    private void WriteGroups(Utf8JsonWriter writer, NodeCanvas canvas)
    {
        writer.WriteStartArray("Groups");
        foreach (var group in canvas.Groups)
        {
            try
            {
                if (group is IJsonSerializable serializable)
                {
                    writer.WriteStartObject();
                    serializable.WriteJson(writer);
                    writer.WriteEndObject();
                }
                else
                {
                    throw new JsonException($"그룹 {group.Name}이(가) IJsonSerializable을 구현하지 않았습니다.");
                }
            }
            catch (Exception ex)
            {
                throw new JsonException($"그룹 {group.Name} 직렬화 중 오류 발생", ex);
            }
        }
        writer.WriteEndArray();
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