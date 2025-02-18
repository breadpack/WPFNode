using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using WPFNode.Constants;
using WPFNode.Interfaces;
using WPFNode.Models.Properties;

namespace WPFNode.Models;

public class DynamicNode : NodeBase
{
    private Func<DynamicNode, Task> _processLogic;
    private string _category;

    private record PortDefinition(string Name, Type Type, int Index, bool IsVisible);
    private record PropertyDefinition(
        string Name, 
        string DisplayName, 
        Type Type, 
        NodePropertyControlType ControlType, 
        string? Format, 
        bool CanConnectToPort, 
        int Index,
        JsonElement? Value = null);

    [JsonConstructor]
    public DynamicNode(INodeCanvas canvas, Guid id) 
        : base(canvas, id)
    {
        _processLogic = node => Task.CompletedTask;
        _category = "Dynamic";
    }

    public DynamicNode(
        INodeCanvas canvas, 
        Guid id,
        string name,
        string category,
        string description,
        Func<DynamicNode, Task> processLogic) 
        : base(canvas, id)
    {
        Name = name;
        _category = category;
        Description = description;
        _processLogic = processLogic;
    }

    public void SetProcessLogic(Func<DynamicNode, Task> processLogic)
    {
        _processLogic = processLogic;
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
        NodePropertyControlType controlType,
        string? format = null,
        bool canConnectToPort = false)
    {
        return CreateProperty<T>(name, displayName, controlType, format, canConnectToPort);
    }

    public INodeProperty AddProperty(
        string name,
        string displayName,
        Type type,
        NodePropertyControlType controlType,
        string? format = null,
        bool canConnectToPort = false)
    {
        return CreateProperty(name, displayName, type, controlType, format, canConnectToPort);
    }

    public InputPort<T>? GetInputPort<T>(string name)
    {
        return InputPorts.OfType<InputPort<T>>().FirstOrDefault(p => p.Name == name);
    }

    public OutputPort<T>? GetOutputPort<T>(string name)
    {
        return OutputPorts.OfType<OutputPort<T>>().FirstOrDefault(p => p.Name == name);
    }

    public override Task ProcessAsync()
    {
        return _processLogic(this);
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
            if (port.Value != null || port.DataType.IsValueType)
            {
                writer.WritePropertyName("Value");
                JsonSerializer.Serialize(writer, port.Value, port.DataType);
            }
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
            writer.WriteNumber("ControlType", (int)prop.Value.ControlType);
            writer.WriteString("Format", prop.Value.Format);
            writer.WriteBoolean("CanConnectToPort", prop.Value.CanConnectToPort);
            writer.WriteNumber("Index", ((IInputPort)prop.Value).GetPortIndex());
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
            var controlType = (NodePropertyControlType)propDef.GetProperty("ControlType").GetInt32();
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
                    name, displayName, type, controlType, format, 
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

            if (type != null)
            {
                yield return new PortDefinition(name, type, index, isVisible);
            }
        }
    }

    public override void ReadJson(JsonElement element)
    {
        base.ReadJson(element);

        try
        {
            ResetNode();

            var inputPorts = CollectInputPortDefinitions(element).ToList();
            var properties = CollectPropertyDefinitions(element).ToList();
            var outputPorts = CollectOutputPortDefinitions(element).ToList();

            // InputPort와 Property를 인덱스 순서대로 복원
            var orderedItems = inputPorts
                .Select(p => (Definition: p, IsProperty: false))
                .Concat(properties.Select(p => 
                    (Definition: new PortDefinition(p.Name, p.Type, p.Index, true), IsProperty: true)))
                .OrderBy(x => x.Definition.Index);

            foreach (var item in orderedItems)
            {
                if (item.IsProperty)
                {
                    var prop = properties.First(p => p.Name == item.Definition.Name);
                    var property = CreateProperty(
                        prop.Name,
                        prop.DisplayName,
                        prop.Type,
                        prop.ControlType,
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

            // 출력 포트 복원
            foreach (var portDef in outputPorts)
            {
                var port = CreateOutputPort(portDef.Name, portDef.Type);
                port.IsVisible = portDef.IsVisible;
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
            throw new JsonException("프로퍼티 정의 복원 중 오류 발생", ex);
        }
    }
} 