using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using WPFNode.Constants;
using WPFNode.Interfaces;
using WPFNode.Models.Properties;
using WPFNode.Models.Serialization;
using System.Reflection;
using WPFNode.Attributes;

namespace WPFNode.Models;

public class SubCanvasNode : NodeBase
{
    private NodeCanvas _innerCanvas;
    private string _category;
    private readonly Dictionary<string, INode> _inputs;
    private readonly Dictionary<string, INode> _outputs;
    private readonly Dictionary<string, INodeProperty> _properties;

    private record PortDefinition(string Name, Type Type, int Index, bool IsVisible)
    {
        public JsonElement? Value { get; init; }
    }
    private record PropertyDefinition(
        string Name, 
        string DisplayName, 
        Type Type, 
        string? Format, 
        bool CanConnectToPort, 
        int Index,
        JsonElement? Value = null);

    [JsonConstructor]
    public SubCanvasNode(INodeCanvas canvas, Guid guid) 
        : base(canvas, guid)
    {
        _innerCanvas = NodeCanvas.Create();
        _category = "Dynamic";
        _inputs = new Dictionary<string, INode>();
        _outputs = new Dictionary<string, INode>();
    }

    public SubCanvasNode(
        INodeCanvas canvas,
        Guid guid,
        string name,
        string category,
        string description)
        : base(canvas, guid)
    {
        Name = name;
        _category = category;
        Description = description;
        _innerCanvas = NodeCanvas.Create();
        _inputs = new Dictionary<string, INode>();
        _outputs = new Dictionary<string, INode>();
        _properties = new Dictionary<string, INodeProperty>();
    }

    public override string Category => _category;

    public NodeCanvas InnerCanvas => _innerCanvas;

    public GraphInputNode<T> CreateGraphInput<T>(string name)
    {
        var inputPort = CreateInputPort<T>(name);
        var inputNode = new GraphInputNode<T>(_innerCanvas, Guid.NewGuid(), (InputPort<T>)inputPort);
        inputNode.Name = name;
        _innerCanvas.SerializableNodes.Add(inputNode);
        
        _inputs[name] = inputNode;
        return inputNode;
    }

    public GraphOutputNode<T> CreateGraphOutput<T>(string name)
    {
        var outputPort = CreateOutputPort<T>(name);
        var outputNode = new GraphOutputNode<T>(_innerCanvas, Guid.NewGuid(), (OutputPort<T>)outputPort);
        outputNode.Name = name;
        _innerCanvas.SerializableNodes.Add(outputNode);
        
        // 출력 노드의 _parentOutput 필드 설정
        var field = typeof(GraphOutputNode<T>).GetField("_parentOutput", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(outputNode, outputPort);
        }
        
        _outputs[name] = outputNode;
        return outputNode;
    }

    protected override async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        await _innerCanvas.ExecuteAsync(cancellationToken);
    }

    public InputPort<T> AddInputPort<T>(string name)
    {
        return CreateInputPort<T>(name);
    }

    public IInputPort AddInputPort(string name, Type type)
    {
        return CreateInputPort(name, type);
    }

    public OutputPort<T> AddOutputPort<T>(string name)
    {
        return CreateOutputPort<T>(name);
    }

    public IOutputPort AddOutputPort(string name, Type type)
    {
        return CreateOutputPort(name, type);
    }

    public NodeProperty<T> AddProperty<T>(
        string name,
        string displayName,
        string? format = null,
        bool canConnectToPort = false)
    {
        return CreateProperty<T>(name, displayName, format, canConnectToPort);
    }

    public INodeProperty AddProperty(
        string name,
        string displayName,
        Type type,
        string? format = null,
        bool canConnectToPort = false)
    {
        return CreateProperty(name, displayName, type, format, canConnectToPort);
    }

    public InputPort<T>? GetInputPort<T>(string name)
    {
        return InputPorts.OfType<InputPort<T>>().FirstOrDefault(p => p.Name == name);
    }

    public OutputPort<T>? GetOutputPort<T>(string name)
    {
        return OutputPorts.OfType<OutputPort<T>>().FirstOrDefault(p => p.Name == name);
    }

    public override void WriteJson(Utf8JsonWriter writer)
    {
        base.WriteJson(writer);

        // 입력 포트 정의 저장 (일반 InputPort와 NodeProperty 모두 포함)
        writer.WriteStartArray("InputPortDefinitions");
        foreach (var port in InputPorts.OrderBy(p => p.GetPortIndex()))
        {
            // NodeProperty인 경우 건너뛰기 (PropertyDefinitions에서 처리)
            if (Properties.Any(prop => prop.Value == port))
                continue;

            writer.WriteStartObject();
            writer.WriteString("Name", port.Name);
            writer.WriteString("Type", port.DataType.AssemblyQualifiedName);
            writer.WriteNumber("Index", port.GetPortIndex());
            writer.WriteBoolean("IsVisible", port.IsVisible);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();

        // 출력 포트 정의 저장
        writer.WriteStartArray("OutputPortDefinitions");
        foreach (var port in OutputPorts)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", port.Name);
            writer.WriteString("Type", port.DataType.AssemblyQualifiedName);
            writer.WriteNumber("Index", port.GetPortIndex());
            writer.WriteBoolean("IsVisible", port.IsVisible);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();

        // 프로퍼티 정의 저장
        writer.WriteStartArray("PropertyDefinitions");
        foreach (var prop in Properties.OrderBy(p => ((IInputPort)p.Value).GetPortIndex()))
        {
            writer.WriteStartObject();
            writer.WriteString("Name", prop.Key);
            writer.WriteString("DisplayName", prop.Value.DisplayName);
            writer.WriteString("Type", prop.Value.PropertyType.AssemblyQualifiedName);
            writer.WriteString("Format", prop.Value.Format);
            writer.WriteBoolean("CanConnectToPort", prop.Value.CanConnectToPort);
            writer.WriteNumber("Index", ((IInputPort)prop.Value).GetPortIndex());
            if(prop.Value.Value != null)
            {
                writer.WritePropertyName("Value");
                JsonSerializer.Serialize(writer, prop.Value.Value, prop.Value.PropertyType, NodeCanvasJsonConverter.SerializerOptions);
            }
            writer.WriteEndObject();
        }
        writer.WriteEndArray();

        // 내부 그래프 상태 저장
        writer.WritePropertyName("InnerCanvas");
        JsonSerializer.Serialize(writer, _innerCanvas, new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new NodeCanvasJsonConverter() }
        });

        // 입출력 노드 매핑 정보 저장
        writer.WriteStartObject("Mappings");
        
        writer.WriteStartObject("Inputs");
        foreach (var input in _inputs)
        {
            writer.WriteString(input.Key, input.Value.Guid.ToString());
        }
        writer.WriteEndObject();
        
        writer.WriteStartObject("Outputs");
        foreach (var output in _outputs)
        {
            writer.WriteString(output.Key, output.Value.Guid.ToString());
        }
        writer.WriteEndObject();
        
        writer.WriteEndObject();
    }

    private IEnumerable<PortDefinition> CollectInputPortDefinitions(JsonElement element)
    {
        if (!element.TryGetProperty("InputPortDefinitions", out var inputPortDefinitions))
            yield break;

        foreach (var portDef in inputPortDefinitions.EnumerateArray())
        {
            var name = portDef.GetProperty("Name").GetString()!;
            var typeName = portDef.GetProperty("Type").GetString()!;
            var type = Type.GetType(typeName);
            var index = portDef.GetProperty("Index").GetInt32();
            var isVisible = portDef.GetProperty("IsVisible").GetBoolean();

            if (type != null)
            {
                yield return new PortDefinition(name, type, index, isVisible);
            }
        }
    }

    private IEnumerable<PropertyDefinition> CollectPropertyDefinitions(JsonElement element)
    {
        if (!element.TryGetProperty("PropertyDefinitions", out var propertyDefinitions))
            yield break;

        foreach (var propDef in propertyDefinitions.EnumerateArray())
        {
            var name = propDef.GetProperty("Name").GetString()!;
            var displayName = propDef.GetProperty("DisplayName").GetString()!;
            var typeName = propDef.GetProperty("Type").GetString()!;
            var type = Type.GetType(typeName);
            var format = propDef.GetProperty("Format").GetString();
            var canConnectToPort = propDef.GetProperty("CanConnectToPort").GetBoolean();
            var index = propDef.GetProperty("Index").GetInt32();

            JsonElement? value = null;
            if (element.TryGetProperty("Properties", out var properties))
            {
                foreach (var prop in properties.EnumerateArray())
                {
                    if (prop.GetProperty("Key").GetString() == name && 
                        prop.TryGetProperty("Value", out var valueElement))
                    {
                        value = valueElement;
                        break;
                    }
                }
            }

            if (type != null)
            {
                yield return new PropertyDefinition(
                    name, displayName, type, format, 
                    canConnectToPort, index, value);
            }
        }
    }

    private IEnumerable<PortDefinition> CollectOutputPortDefinitions(JsonElement element)
    {
        if (!element.TryGetProperty("OutputPortDefinitions", out var outputPortDefinitions))
            yield break;

        foreach (var portDef in outputPortDefinitions.EnumerateArray())
        {
            var name = portDef.GetProperty("Name").GetString()!;
            var typeName = portDef.GetProperty("Type").GetString()!;
            var type = Type.GetType(typeName);
            var index = portDef.GetProperty("Index").GetInt32();
            var isVisible = portDef.GetProperty("IsVisible").GetBoolean();
            JsonElement? value = portDef.TryGetProperty("Value", out var valueElement) ? valueElement : null;

            if (type != null)
            {
                yield return new PortDefinition(name, type, index, isVisible) with 
                { 
                    Value = value 
                };
            }
        }
    }

    public override void ReadJson(JsonElement element, JsonSerializerOptions options)
    {
        base.ReadJson(element, options);

        try
        {
            ResetNode();

            // Category 값 복원
            if (element.TryGetProperty("Category", out var categoryElement))
            {
                _category = categoryElement.GetString() ?? "Dynamic";
            }

            // 기본 포트와 프로퍼티 복원
            var inputPorts = CollectInputPortDefinitions(element).ToList();
            var properties = CollectPropertyDefinitions(element).ToList();
            var outputPorts = CollectOutputPortDefinitions(element).ToList();

            // InputPort와 Property를 인덱스 순서대로 복원
            var orderedItems = inputPorts
                .Select(p => (Definition: p, IsProperty: false))
                .Concat(properties.Select(p => 
                    (Definition: new PortDefinition(p.Name, p.Type, p.Index, true), IsProperty: true)))
                .OrderBy(x => x.Definition.Index);

            // 포트와 프로퍼티 생성
            foreach (var item in orderedItems)
            {
                if (item.IsProperty)
                {
                    var prop = properties.First(p => p.Name == item.Definition.Name);
                    var property = CreateProperty(
                        prop.Name,
                        prop.DisplayName,
                        prop.Type,
                        prop.Format,
                        prop.CanConnectToPort);

                    if (prop.Value.HasValue)
                    {
                        property.Value = JsonSerializer.Deserialize(
                            prop.Value.Value.GetRawText(),
                            prop.Type);
                    }
                }
                else
                {
                    var port = CreateInputPort(item.Definition.Name, item.Definition.Type);
                    port.IsVisible = item.Definition.IsVisible;
                }
            }

            // 출력 포트 생성 및 값 복원
            foreach (var portDef in outputPorts)
            {
                var port = CreateOutputPort(portDef.Name, portDef.Type);
                
                if (port != null)
                {
                    port.IsVisible = portDef.IsVisible;
                    
                    if (portDef.Value.HasValue)
                    {
                        var value = JsonSerializer.Deserialize(portDef.Value.Value.GetRawText(), portDef.Type);
                        if (value != null)
                        {
                            port.Value = value;
                        }
                    }
                }
            }

            // 내부 그래프 복원
            if (element.TryGetProperty("InnerCanvas", out var innerCanvasElement))
            {
                // 먼저 입력/출력 노드 매핑 정보를 수집
                var inputMappings = new Dictionary<Guid, (string Name, IInputPort Port, JsonElement? Value)>();
                var outputMappings = new Dictionary<Guid, (string Name, IOutputPort Port)>();
                
                if (element.TryGetProperty("Mappings", out var mappings))
                {
                    // 입력 매핑 수집
                    if (mappings.TryGetProperty("Inputs", out var inputs))
                    {
                        foreach (var inputMapping in inputs.EnumerateObject())
                        {
                            var nodeId = Guid.Parse(inputMapping.Value.GetString()!);
                            var inputPort = InputPorts.FirstOrDefault(p => p.Name == inputMapping.Name);
                            if (inputPort != null)
                            {
                                // 입력 포트의 값 찾기
                                JsonElement? value = null;
                                if (element.TryGetProperty("InputPortDefinitions", out var inputPortDefs))
                                {
                                    foreach (var portDef in inputPortDefs.EnumerateArray())
                                    {
                                        if (portDef.GetProperty("Name").GetString() == inputMapping.Name &&
                                            portDef.TryGetProperty("Value", out var valueElement))
                                        {
                                            value = valueElement;
                                            break;
                                        }
                                    }
                                }
                                inputMappings[nodeId] = (inputMapping.Name, inputPort, value);
                            }
                        }
                    }

                    // 출력 매핑 수집
                    if (mappings.TryGetProperty("Outputs", out var outputs))
                    {
                        foreach (var outputMapping in outputs.EnumerateObject())
                        {
                            var nodeId = Guid.Parse(outputMapping.Value.GetString()!);
                            var outputPort = OutputPorts.FirstOrDefault(p => p.Name == outputMapping.Name);
                            if (outputPort != null)
                            {
                                outputMappings[nodeId] = (outputMapping.Name, outputPort);
                            }
                        }
                    }
                }

                // 내부 그래프 복원 전에 노드 생성 이벤트 핸들러 등록
                var originalNodes = new List<INode>();
                void OnNodeCreated(object? sender, INode node)
                {
                    // 입력 노드 처리
                    if (inputMappings.TryGetValue(node.Guid, out var inputMapping))
                    {
                        var nodeType = node.GetType();
                        if (nodeType.IsGenericType && nodeType.GetGenericTypeDefinition() == typeof(GraphInputNode<>))
                        {
                            // _parentInput 필드 설정
                            var field = nodeType.GetField("_parentInput", BindingFlags.NonPublic | BindingFlags.Instance);
                            if (field != null)
                            {
                                field.SetValue(node, inputMapping.Port);
                            }
                            _inputs[inputMapping.Name] = node;
                        }
                    }

                    // 출력 노드 처리
                    if (outputMappings.TryGetValue(node.Guid, out var outputMapping))
                    {
                        var nodeType = node.GetType();
                        if (nodeType.IsGenericType && nodeType.GetGenericTypeDefinition() == typeof(GraphOutputNode<>))
                        {
                            var field = nodeType.GetField("_parentOutput", BindingFlags.NonPublic | BindingFlags.Instance);
                            if (field != null)
                            {
                                field.SetValue(node, outputMapping.Port);
                            }
                            _outputs[outputMapping.Name] = node;
                        }
                    }

                    originalNodes.Add(node);
                }

                _innerCanvas.NodeAdded += OnNodeCreated;

                // 내부 그래프 복원
                var deserializedCanvas = JsonSerializer.Deserialize<NodeCanvas>(
                    innerCanvasElement.GetRawText(),
                    new JsonSerializerOptions
                    {
                        Converters = { new NodeCanvasJsonConverter() }
                    })!;

                // 기존 캔버스의 노드와 연결을 모두 제거
                foreach (var node in _innerCanvas.Nodes.ToList())
                {
                    _innerCanvas.RemoveNode(node);
                }

                // 역직렬화된 캔버스의 노드와 연결을 기존 캔버스로 복사
                _innerCanvas.SerializableNodes = deserializedCanvas.SerializableNodes;
                foreach (var connection in deserializedCanvas.SerializableConnections)
                {
                    _innerCanvas.SerializableConnections.Add(connection);
                }

                // 이벤트 핸들러 제거
                _innerCanvas.NodeAdded -= OnNodeCreated;
            }

            // 프로퍼티 포트의 가시성 복원
            if (element.TryGetProperty("Properties", out var propsVisibility))
            {
                foreach (var prop in propsVisibility.EnumerateArray())
                {
                    var key = prop.GetProperty("Key").GetString()!;
                    if (Properties.TryGetValue(key, out var property) && 
                        property is IInputPort inputPort)
                    {
                        inputPort.IsVisible = prop.GetProperty("IsVisible").GetBoolean();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new JsonException("DynamicNode 복원 중 오류 발생", ex);
        }
    }
} 