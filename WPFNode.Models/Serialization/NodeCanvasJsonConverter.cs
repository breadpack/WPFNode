using System.Text.Json;
using System.Text.Json.Serialization;
using WPFNode.Interfaces;

namespace WPFNode.Models.Serialization;

public partial class NodeCanvasJsonConverter : JsonConverter<NodeCanvas>
{
    [System.Text.RegularExpressions.GeneratedRegex(@"^([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})(?::(in|out)\[(.+?)\]|\|\|\|(in|out)\|\|\|(.+?)\|\|\|(\d+))$")]
    private static partial System.Text.RegularExpressions.Regex PortIdRegex();
    
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
            
            // 결과 객체 생성
            var result = new DeserializationResult(canvas);
            
            // 순서대로 복원: 노드 -> 연결 -> 그룹
            ReadNodes(canvasData, canvas, nodesById, options, result);
            ReadConnections(canvasData, canvas, nodesById, result);
            ReadGroups(canvasData, canvas, nodesById, result);
            
            // 오류가 있으면 로그에 기록
            if (result.HasErrors)
            {
                Console.WriteLine(result.GetErrorSummary());
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"[{error.ElementType}] {error.ElementId}: {error.Message}");
                }
            }
            
            return canvas;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"JSON 파싱 오류: {ex.Message}");
            throw; // JSON 구문 자체가 잘못된 경우는 여전히 실패로 처리
        }
        catch (Exception ex)
        {
            Console.WriteLine($"예상치 못한 오류: {ex.Message}");
            throw new JsonException("노드 캔버스 역직렬화 중 오류 발생", ex);
        }
    }

    // JSON 문자열에서 부분 오류를 허용하며 NodeCanvas를 복원하는 정적 메서드
    public static DeserializationResult DeserializeWithPartialErrors(string json)
    {
        try
        {
            var document = JsonDocument.Parse(json);
            var canvas = NodeCanvas.Create();
            var nodesById = new Dictionary<Guid, INode>();
            
            var result = new DeserializationResult(canvas);
            
            // 순서대로 복원
            ReadNodes(document.RootElement, canvas, nodesById, _serializerOptions, result);
            ReadConnections(document.RootElement, canvas, nodesById, result);
            ReadGroups(document.RootElement, canvas, nodesById, result);
            
            return result;
        }
        catch (JsonException ex)
        {
            // JSON 구문 자체가 잘못된 경우
            var canvas = NodeCanvas.Create();
            var result = new DeserializationResult(canvas);
            result.AddError("Canvas", "Root", "JSON 구문 오류", ex.Message, ex);
            return result;
        }
        catch (Exception ex)
        {
            // 기타 예상치 못한 오류
            var canvas = NodeCanvas.Create();
            var result = new DeserializationResult(canvas);
            result.AddError("Canvas", "Root", "예상치 못한 오류", ex.Message, ex);
            return result;
        }
    }

    private static void ReadNodes(JsonElement canvasData, NodeCanvas canvas, Dictionary<Guid, INode> nodesById, JsonSerializerOptions options, DeserializationResult result)
    {
        if (!canvasData.TryGetProperty("Nodes", out var nodesElement)) return;

        foreach (var nodeElement in nodesElement.EnumerateArray())
        {
            try
            {
                // 노드 정보 추출
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

                // 노드 생성
                var nodeGuidStr = nodeElement.GetProperty("Guid").GetString() ?? Guid.NewGuid().ToString();
                var nodeGuid = Guid.Parse(nodeGuidStr);
                var x = nodeElement.GetProperty("X").GetDouble();
                var y = nodeElement.GetProperty("Y").GetDouble();
                var node = canvas.CreateNodeWithGuid(nodeGuid, nodeType, x, y);
                
                if (node == null)
                {
                    throw new JsonException($"노드 생성에 실패했습니다: {nodeType.Name}");
                }
                
                nodesById[node.Guid] = node;

                // 노드 상태 복원
                if (node is IJsonSerializable serializable)
                {
                    serializable.ReadJson(nodeElement, options);
                }
            }
            catch (Exception ex)
            {
                var errorDetails = GetDetailedErrorMessage(ex, nodeElement);
                var nodeId = nodeElement.TryGetProperty("Guid", out var guidElement) 
                    ? guidElement.GetString() ?? "알 수 없음" 
                    : "알 수 없음";
                var nodeName = nodeElement.TryGetProperty("Name", out var nameElement)
                    ? nameElement.GetString() ?? "알 수 없음"
                    : "알 수 없음";
                
                result.AddError("Node", nodeId, $"노드 '{nodeName}' 복원 실패", errorDetails, ex);
                // 이 노드는 건너뛰고 다음 노드로 계속 진행
            }
        }
    }

    private static string GetDetailedErrorMessage(Exception ex, JsonElement nodeElement)
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
        }
        catch (Exception)
        {
            details.Add("노드 데이터 파싱 중 추가 오류 발생");
        }

        return string.Join("\n", details);
    }

    private static void ReadConnections(JsonElement canvasData, NodeCanvas canvas, Dictionary<Guid, INode> nodesById, DeserializationResult result)
    {
        if (!canvasData.TryGetProperty("Connections", out var connectionsElement)) return;

        foreach (var connectionElement in connectionsElement.EnumerateArray())
        {
            try
            {
                // 연결 ID 파싱
                var connectionIdStr = connectionElement.GetProperty("Guid").GetString() ?? Guid.NewGuid().ToString();
                var connectionId = Guid.Parse(connectionIdStr);
                
                var (sourcePort, targetPort) = FindInputOutputPorts(nodesById, connectionElement);

                // 포트를 찾지 못한 경우 예외 발생
                if (sourcePort == null || targetPort == null)
                {
                    throw new JsonException($"포트를 찾을 수 없습니다. 소스 포트 또는 타겟 포트가 존재하지 않습니다.");
                }

                canvas.ConnectWithId(connectionId, sourcePort, targetPort);
            }
            catch (Exception ex)
            {
                var connectionId = connectionElement.TryGetProperty("Guid", out var guidElement)
                    ? guidElement.GetString() ?? "알 수 없음"
                    : "알 수 없음";
                
                result.AddError("Connection", connectionId, "연결 복원 실패", ex.Message, ex);
                // 이 연결은 건너뛰고 다음 연결로 계속 진행
            }
        }
    }

    private static (IPort? sourcePort, IPort? targetPort) FindInputOutputPorts(Dictionary<Guid, INode> nodesById, JsonElement connectionElement) {
        // 포트 검색을 위한 변수
        IPort? sourcePort = null;
        IPort? targetPort = null;
                
        // 1. 새로운 방식: Name 기반 포트 찾기 시도
        if (connectionElement.TryGetProperty("SourceNodeId", out var sourceNodeIdElement) &&
            connectionElement.TryGetProperty("SourcePortName", out var sourcePortNameElement) &&
            connectionElement.TryGetProperty("SourceIsInput", out var sourceIsInputElement) &&
            connectionElement.TryGetProperty("TargetNodeId", out var targetNodeIdElement) &&
            connectionElement.TryGetProperty("TargetPortName", out var targetPortNameElement) &&
            connectionElement.TryGetProperty("TargetIsInput", out var targetIsInputElement))
        {
            var sourceNodeIdStr = sourceNodeIdElement.GetString() ?? string.Empty;
            var sourceNodeId    = Guid.Parse(sourceNodeIdStr);
            var sourceIsInput   = bool.Parse(sourceIsInputElement.GetString() ?? "false");
            var sourcePortName  = sourcePortNameElement.GetString() ?? string.Empty;
                    
            var targetNodeIdStr = targetNodeIdElement.GetString() ?? string.Empty;
            var targetNodeId    = Guid.Parse(targetNodeIdStr);
            var targetIsInput   = bool.Parse(targetIsInputElement.GetString() ?? "true");
            var targetPortName  = targetPortNameElement.GetString() ?? string.Empty;
                    
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
                sourcePort = sourceNode.OutputPorts
                                       .Cast<IPort>()
                                       .Concat(sourceNode.FlowOutPorts)
                                       .FirstOrDefault(p => p.Name == sourcePortName) as IOutputPort;
            }
                    
            if (targetIsInput)
            {
                targetPort = targetNode.InputPorts
                                       .Cast<IPort>()
                                        .Concat(targetNode.FlowInPorts)
                                       .FirstOrDefault(p => p.Name == targetPortName) as IInputPort;
            }
        }
                
        // 2. 기존 방식(PortId 기반)으로 포트 찾기 시도 (Name 기반 검색이 실패한 경우)
        if (sourcePort == null || targetPort == null)
        {
            if (!connectionElement.TryGetProperty("SourcePortId", out var sourcePortIdElement) ||
                !connectionElement.TryGetProperty("TargetPortId", out var targetPortIdElement))
            {
                throw new JsonException("포트 ID 정보가 누락되었습니다.");
            }

            var sourcePortIdStr = sourcePortIdElement.GetString() ?? string.Empty;
            var targetPortIdStr = targetPortIdElement.GetString() ?? string.Empty;

            // 포트 ID 정규식 패턴
            var sourceMatch = PortIdRegex().Match(sourcePortIdStr);
            var targetMatch = PortIdRegex().Match(targetPortIdStr);

            if (!sourceMatch.Success || !targetMatch.Success)
            {
                throw new JsonException($"잘못된 포트 ID 형식입니다. 소스: {sourcePortIdStr}, 타겟: {targetPortIdStr}");
            }

            // 소스 포트 정보 추출
            var sourceNodeIdStr = sourceMatch.Groups[1].Value;
            if (!Guid.TryParse(sourceNodeIdStr, out var sourceNodeId))
            {
                throw new JsonException($"잘못된 소스 노드 ID 형식: {sourceNodeIdStr}");
            }

            // 기존 패턴과 새로운 패턴 구분
            var sourceIsInput   = sourceMatch.Groups[2].Success ? sourceMatch.Groups[2].Value == "in" : sourceMatch.Groups[4].Value == "in";
            var sourcePortName  = sourceMatch.Groups[2].Success ? sourceMatch.Groups[3].Value : sourceMatch.Groups[5].Value;
            var sourcePortIndex = sourceMatch.Groups[2].Success ? 0 : int.Parse(sourceMatch.Groups[6].Value);

            // 타겟 포트 정보 추출
            var targetNodeIdStr = targetMatch.Groups[1].Value;
            if (!Guid.TryParse(targetNodeIdStr, out var targetNodeId))
            {
                throw new JsonException($"잘못된 타겟 노드 ID 형식: {targetNodeIdStr}");
            }

            // 기존 패턴과 새로운 패턴 구분
            var targetIsInput   = targetMatch.Groups[2].Success ? targetMatch.Groups[2].Value == "in" : targetMatch.Groups[4].Value == "in";
            var targetPortName  = targetMatch.Groups[2].Success ? targetMatch.Groups[3].Value : targetMatch.Groups[5].Value;
            var targetPortIndex = targetMatch.Groups[2].Success ? 0 : int.Parse(targetMatch.Groups[6].Value);

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
                                       .Cast<IPort>()
                                       .Concat(sourceNode.FlowOutPorts)
                                       .FirstOrDefault(p => p.Name == sourcePortName);
            }

            // 타겟 포트 찾기
            if (targetIsInput) // 입력 포트에서 찾기
            {
                // 먼저 이름으로 찾기 시도
                targetPort = targetNode.InputPorts
                                       .Cast<IPort>()
                                       .Concat(targetNode.FlowInPorts)
                                       .FirstOrDefault(p => p.Name == targetPortName);
            }
        }
        
        return (sourcePort, targetPort);
    }

    private static void ReadGroups(JsonElement canvasData, NodeCanvas canvas, Dictionary<Guid, INode> nodesById, DeserializationResult result)
    {
        if (!canvasData.TryGetProperty("Groups", out var groupsElement)) return;

        foreach (var groupElement in groupsElement.EnumerateArray())
        {
            try
            {
                // 1. 그룹 생성에 필요한 기본 정보 파싱
                var groupIdStr = groupElement.GetProperty("Id").GetString() ?? Guid.NewGuid().ToString();
                var groupId = Guid.Parse(groupIdStr);
                var name = groupElement.GetProperty("Name").GetString() ?? "New Group";
                
                var nodeIds = new List<Guid>();
                if (groupElement.TryGetProperty("NodeIds", out var nodeIdsElement))
                {
                    nodeIds = nodeIdsElement.EnumerateArray()
                        .Select(e => Guid.Parse(e.GetString() ?? Guid.Empty.ToString()))
                        .Where(id => id != Guid.Empty)
                        .ToList();
                }

                // 2. 노드 찾기 - 존재하는 노드만 포함
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
            }
            catch (Exception ex)
            {
                var groupId = groupElement.TryGetProperty("Id", out var idElement)
                    ? idElement.GetString() ?? "알 수 없음" 
                    : "알 수 없음";
                var groupName = groupElement.TryGetProperty("Name", out var nameElement)
                    ? nameElement.GetString() ?? "알 수 없음"
                    : "알 수 없음";
                
                result.AddError("Group", groupId, $"그룹 '{groupName}' 복원 실패", ex.Message, ex);
                // 이 그룹은 건너뛰고 다음 그룹으로 계속 진행
            }
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
