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
    private static JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true,
        Converters = 
        {
            new TypeJsonConverter()
        }
    };

    public static JsonSerializerOptions SerializerOptions
    {
        get => _serializerOptions;
        set
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            _serializerOptions = value;
        }
    }

    /// <summary>
    /// JsonSerializerOptions를 초기화합니다.
    /// </summary>
    /// <param name="configure">JsonSerializerOptions를 구성하는 Action</param>
    public static void ConfigureSerializerOptions(Action<JsonSerializerOptions> configure)
    {
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        var options = new JsonSerializerOptions(_serializerOptions);
        configure(options);
        _serializerOptions = options;
    }

    /// <summary>
    /// JsonConverter를 추가합니다.
    /// </summary>
    /// <param name="converter">추가할 JsonConverter</param>
    public static void AddConverter(JsonConverter converter)
    {
        if (converter == null)
            throw new ArgumentNullException(nameof(converter));

        _serializerOptions.Converters.Add(converter);
    }

    /// <summary>
    /// JsonConverter를 제거합니다.
    /// </summary>
    /// <param name="converter">제거할 JsonConverter</param>
    public static bool RemoveConverter(JsonConverter converter)
    {
        if (converter == null)
            throw new ArgumentNullException(nameof(converter));

        return _serializerOptions.Converters.Remove(converter);
    }

    /// <summary>
    /// 특정 타입의 JsonConverter를 제거합니다.
    /// </summary>
    /// <typeparam name="T">제거할 JsonConverter 타입</typeparam>
    public static void RemoveConverter<T>() where T : JsonConverter
    {
        var converterToRemove = _serializerOptions.Converters.FirstOrDefault(c => c is T);
        if (converterToRemove != null)
        {
            _serializerOptions.Converters.Remove(converterToRemove);
        }
    }

    public override NodeCanvas? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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
                var nodeGuid = Guid.Parse(nodeElement.GetProperty("Guid").GetString()!);
                var x = nodeElement.GetProperty("X").GetDouble();
                var y = nodeElement.GetProperty("Y").GetDouble();
                var node = canvas.CreateNodeWithGuid(nodeGuid, nodeType, x, y);
                
                if (node == null)
                {
                    throw new JsonException($"노드 생성에 실패했습니다: {nodeType.Name}");
                }
                
                nodesById[node.Guid] = node;

                // 3. 노드 상태 복원
                if (node is IJsonSerializable serializable)
                {
                    serializable.ReadJson(nodeElement, options);
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
        try {
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
                // 연결 ID 파싱
                var connectionId = Guid.Parse(connectionElement.GetProperty("Guid").GetString()!);
                
                // 포트 검색을 위한 변수
                IOutputPort? sourcePort = null;
                IInputPort? targetPort = null;
                
                // 1. 새로운 방식: Name 기반 포트 찾기 시도
                if (connectionElement.TryGetProperty("SourceNodeId", out var sourceNodeIdElement) &&
                    connectionElement.TryGetProperty("SourcePortName", out var sourcePortNameElement) &&
                    connectionElement.TryGetProperty("SourceIsInput", out var sourceIsInputElement) &&
                    connectionElement.TryGetProperty("TargetNodeId", out var targetNodeIdElement) &&
                    connectionElement.TryGetProperty("TargetPortName", out var targetPortNameElement) &&
                    connectionElement.TryGetProperty("TargetIsInput", out var targetIsInputElement))
                {
                    // Name 기반으로 포트 찾기
                    var sourceNodeId = Guid.Parse(sourceNodeIdElement.GetString()!);
                    var sourceIsInput = bool.Parse(sourceIsInputElement.GetString()!);
                    var sourcePortName = sourcePortNameElement.GetString()!;
                    
                    var targetNodeId = Guid.Parse(targetNodeIdElement.GetString()!);
                    var targetIsInput = bool.Parse(targetIsInputElement.GetString()!);
                    var targetPortName = targetPortNameElement.GetString()!;
                    
                    // 소스 노드와 타겟 노드 찾기
                    if (!nodesById.TryGetValue(sourceNodeId, out var sourceNode))
                    {
                        throw new JsonException($"소스 노드를 찾을 수 없습니다: {sourceNodeId}");
                    }

                    if (!nodesById.TryGetValue(targetNodeId, out var targetNode))
                    {
                        throw new JsonException($"타겟 노드를 찾을 수 없습니다: {targetNodeId}");
                    }
                    
                    // Name으로 포트 찾기
                    if (!sourceIsInput)
                    {
                        sourcePort = sourceNode.OutputPorts.FirstOrDefault(p => p.Name == sourcePortName) as IOutputPort;
                    }
                    
                    if (targetIsInput)
                    {
                        targetPort = targetNode.InputPorts.FirstOrDefault(p => p.Name == targetPortName) as IInputPort;
                    }

                    if (sourcePort != null && targetPort != null)
                    {
                        Console.WriteLine($"Name 기반으로 포트 찾음 - 소스: {sourcePortName}, 타겟: {targetPortName}");
                    }
                }
                
                // 2. 기존 방식(PortId 기반)으로 포트 찾기 시도 (Name 기반 검색이 실패한 경우)
                if (sourcePort == null || targetPort == null)
                {
                    Console.WriteLine("Name 기반 포트 검색 실패. PortId 기반으로 시도합니다.");
                    
                    var sourcePortIdStr = connectionElement.GetProperty("SourcePortId").GetString()!;
                    var targetPortIdStr = connectionElement.GetProperty("TargetPortId").GetString()!;

                    // 포트 ID 문자열 파싱
                    var sourcePortIdParts = sourcePortIdStr.Split(new[] { ':', '[', ']' }, StringSplitOptions.None);
                    var targetPortIdParts = targetPortIdStr.Split(new[] { ':', '[', ']' }, StringSplitOptions.None);

                    if (sourcePortIdParts.Length < 4 || targetPortIdParts.Length < 4)
                    {
                        throw new JsonException($"잘못된 포트 ID 형식입니다. 소스: {sourcePortIdStr}, 타겟: {targetPortIdStr}");
                    }

                    var sourceNodeId = Guid.Parse(sourcePortIdParts[0]);
                    var sourceIsInput = sourcePortIdParts[1] == "in";
                    var sourceIndexOrName = sourcePortIdParts[2]; // 인덱스 또는 이름일 수 있음

                    var targetNodeId = Guid.Parse(targetPortIdParts[0]);
                    var targetIsInput = targetPortIdParts[1] == "in";
                    var targetIndexOrName = targetPortIdParts[2]; // 인덱스 또는 이름일 수 있음

                    // 소스 노드와 타겟 노드 찾기
                    if (!nodesById.TryGetValue(sourceNodeId, out var sourceNode))
                    {
                        throw new JsonException($"소스 노드를 찾을 수 없습니다: {sourceNodeId}");
                    }

                    if (!nodesById.TryGetValue(targetNodeId, out var targetNode))
                    {
                        throw new JsonException($"타겟 노드를 찾을 수 없습니다: {targetNodeId}");
                    }

                    // 소스 포트 찾기
                    if (!sourceIsInput) // 출력 포트에서 찾기
                    {
                        // 먼저 이름으로 찾기 시도
                        sourcePort = sourceNode.OutputPorts
                                               .Concat(sourceNode.FlowOutPorts)
                                               .FirstOrDefault(p => p.Name == sourceIndexOrName) as IOutputPort;
                        
                        // 이름으로 찾지 못한 경우, 인덱스로 시도 (이전 버전 호환성)
                        if (sourcePort == null && int.TryParse(sourceIndexOrName, out var sourceIndex))
                        {
                            sourcePort = sourceNode.OutputPorts
                                                   .Concat(sourceNode.FlowOutPorts)
                                                   .FirstOrDefault(p => p.GetPortIndex() == sourceIndex) as IOutputPort;
                        }
                    }

                    // 타겟 포트 찾기
                    if (targetIsInput) // 입력 포트에서 찾기
                    {
                        // 먼저 이름으로 찾기 시도
                        targetPort = targetNode.InputPorts
                                               .Concat(targetNode.FlowInPorts)
                                               .FirstOrDefault(p => p.Name == targetIndexOrName) as IInputPort;
                        
                        // 이름으로 찾지 못한 경우, 인덱스로 시도 (이전 버전 호환성)
                        if (targetPort == null && int.TryParse(targetIndexOrName, out var targetIndex))
                        {
                            targetPort = targetNode.InputPorts
                                                   .Concat(targetNode.FlowInPorts)
                                                   .FirstOrDefault(p => p.GetPortIndex() == targetIndex) as IInputPort;
                        }
                    }
                }

                // 포트를 찾지 못한 경우 예외 발생
                if (sourcePort == null || targetPort == null)
                {
                    Console.WriteLine("모든 방법으로 포트를 찾을 수 없습니다.");
                    throw new JsonException($"포트를 찾을 수 없습니다. 소스 포트 또는 타겟 포트가 존재하지 않습니다.");
                }

                // 3. 연결 생성
                var connection = canvas.ConnectWithId(connectionId, sourcePort, targetPort);
                if (connection == null)
                {
                    Console.WriteLine("연결 생성 실패");
                    throw new JsonException("연결 생성에 실패했습니다.");
                }
                else
                {
                    Console.WriteLine($"연결 생성 성공: {connection.Guid}");
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
                $"- 연결 복원 실패: {f.Element.GetProperty("Guid").GetString() ?? "알 수 없는 ID"}, 오류: {f.Error.Message}"));
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
                    .Where(nodesById.ContainsKey)
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
                    serializable.ReadJson(groupElement, _serializerOptions);
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
            
            WriteNodes(writer, value, _serializerOptions);
            WriteConnections(writer, value, _serializerOptions);
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

    private void WriteConnections(Utf8JsonWriter writer, NodeCanvas canvas, JsonSerializerOptions options)
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
                    throw new JsonException($"연결 {connection.Guid}이(가) IJsonSerializable을 구현하지 않았습니다.");
                }
            }
            catch (Exception ex)
            {
                throw new JsonException($"연결 {connection.Guid} 직렬화 중 오류 발생", ex);
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
