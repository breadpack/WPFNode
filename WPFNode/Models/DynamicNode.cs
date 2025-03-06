using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using WPFNode.Constants;
using WPFNode.Interfaces;
using WPFNode.Models.Properties;
using WPFNode.Models.Serialization;
using System.Reflection;
using WPFNode.Attributes;
using Microsoft.Extensions.Logging;

namespace WPFNode.Models;

/// <summary>
/// 런타임에 Port와 Property를 동적으로 정의할 수 있는 노드 클래스입니다.
/// 이 클래스는 Port와 Property 정보를 직렬화하고 역직렬화하는 기능을 제공합니다.
/// </summary>
public class DynamicNode : NodeBase
{
    private string _category;
    private readonly HashSet<string> _initializedProperties = new HashSet<string>();

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
    public DynamicNode(INodeCanvas canvas, Guid guid) 
        : base(canvas, guid)
    {
        _category = "Dynamic";
    }

    public DynamicNode(
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
    }

    public override string Category => _category;

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
        if (_initializedProperties.Contains(name) && Properties.TryGetValue(name, out var existingProperty))
        {
            return (NodeProperty<T>)existingProperty;
        }

        var property = CreateProperty<T>(name, displayName, format, canConnectToPort);
        _initializedProperties.Add(name);
        return property;
    }

    public INodeProperty AddProperty(
        string name,
        string displayName,
        Type type,
        string? format = null,
        bool canConnectToPort = false)
    {
        if (_initializedProperties.Contains(name) && Properties.TryGetValue(name, out var existingProperty))
        {
            return existingProperty;
        }

        var property = CreateProperty(name, displayName, type, format, canConnectToPort);
        _initializedProperties.Add(name);
        return property;
    }

    public InputPort<T>? GetInputPort<T>(string name)
    {
        return InputPorts.OfType<InputPort<T>>().FirstOrDefault(p => p.Name == name);
    }

    public OutputPort<T>? GetOutputPort<T>(string name)
    {
        return OutputPorts.OfType<OutputPort<T>>().FirstOrDefault(p => p.Name == name);
    }

    /// <summary>
    /// 모든 포트를 제거합니다. 단, 특정 속성은 제외할 수 있습니다.
    /// </summary>
    /// <param name="excludePropertyNames">제외할 속성 이름 목록</param>
    public void ClearPorts(params string[] excludePropertyNames)
    {
        // 제외할 속성 이름을 HashSet으로 변환
        var excludeSet = new HashSet<string>(excludePropertyNames);
        
        // 제거할 속성 목록 생성 (제외 목록에 없는 속성만)
        var propertiesToRemove = Properties
            .Where(p => !excludeSet.Contains(p.Key))
            .Select(p => p.Value)
            .ToList();
            
        // 속성 제거
        foreach (var prop in propertiesToRemove)
        {
            RemoveProperty(prop);
        }
        
        // 출력 포트 제거
        var outputPortsToRemove = OutputPorts.ToList();
        foreach (var port in outputPortsToRemove)
        {
            RemoveOutputPort(port);
        }
        
        // 입력 포트 제거 (속성이 아닌 입력 포트만)
        var inputPortsToRemove = InputPorts
            .Where(p => Properties.Values.All(prop => prop != p))
            .ToList();
            
        foreach (var port in inputPortsToRemove)
        {
            RemoveInputPort(port);
        }
    }

    protected override async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        // 기본 구현은 아무 작업도 수행하지 않습니다.
        // 파생 클래스에서 이 메서드를 재정의하여 실제 처리 로직을 구현해야 합니다.
        await Task.CompletedTask;
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
            if (propDef.TryGetProperty("Value", out var valueElement))
            {
                value = valueElement;
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
            _initializedProperties.Clear();

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
                            prop.Type,
                            options);
                    }
                    
                    _initializedProperties.Add(prop.Name);
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
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "DynamicNode 역직렬화 중 오류 발생");
            throw;
        }
    }
} 