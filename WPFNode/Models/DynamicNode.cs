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
    private bool _isInitialized = false;

    private record PortDefinition(string Name, Type Type, int Index, bool IsVisible)
    {
        public JsonElement? Value { get; init; }
    }

    [JsonConstructor]
    public DynamicNode(INodeCanvas canvas, Guid guid) 
        : base(canvas, guid)
    {
        _category = "Dynamic";
    }

    /// <summary>
    /// 노드를 초기화합니다. 이 메서드는 생성 및 역직렬화 시 자동으로 호출됩니다.
    /// </summary>
    public void InitializeNode()
    {
        // 이미 초기화되었으면 건너뜀
        if (_isInitialized)
            return;

        try
        {
            // 2. 노드 구성 (이벤트 핸들러, 포트 설정 등)
            ConfigureNode();
            
            // 초기화 완료 표시
            _isInitialized = true;
            
            Logger?.LogDebug($"{GetType().Name} 노드 초기화 완료");
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, $"{GetType().Name} 노드 초기화 중 오류 발생");
            throw;
        }
    }

    /// <summary>
    /// 노드를 구성합니다. 파생 클래스에서 오버라이드하여 포트 설정, 이벤트 연결 등을 수행할 수 있습니다.
    /// </summary>
    protected virtual void ConfigureNode()
    {
        // 기본 구현은 비어있음 - 파생 클래스에서 오버라이드
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
        // 이미 해당 이름과 타입의 입력 포트가 있는지 확인
        var existingPort = InputPorts.OfType<InputPort<T>>().FirstOrDefault(p => p.Name == name);
        if (existingPort != null)
        {
            return existingPort;
        }

        // 없으면 새로 생성
        return CreateInputPort<T>(name);
    }

    public IInputPort AddInputPort(string name, Type type)
    {
        // 이미 해당 이름과 타입의 입력 포트가 있는지 확인
        var existingPort = InputPorts.FirstOrDefault(p => p.Name == name && p.DataType == type);
        if (existingPort != null)
        {
            return existingPort;
        }

        // 없으면 새로 생성
        return CreateInputPort(name, type);
    }

    public OutputPort<T> AddOutputPort<T>(string name)
    {
        // 이미 해당 이름과 타입의 출력 포트가 있는지 확인
        var existingPort = OutputPorts.OfType<OutputPort<T>>().FirstOrDefault(p => p.Name == name);
        if (existingPort != null)
        {
            return existingPort;
        }

        // 없으면 새로 생성
        return CreateOutputPort<T>(name);
    }

    public IOutputPort AddOutputPort(string name, Type type)
    {
        // 이미 해당 이름과 타입의 출력 포트가 있는지 확인
        var existingPort = OutputPorts.FirstOrDefault(p => p.Name == name && p.DataType == type);
        if (existingPort != null)
        {
            return existingPort;
        }

        // 없으면 새로 생성
        return CreateOutputPort(name, type);
    }

    public NodeProperty<T> AddProperty<T>(
        string name,
        string displayName,
        string? format = null,
        bool canConnectToPort = false)
    {
        return (NodeProperty<T>)AddProperty(name, displayName, typeof(T), format, canConnectToPort);
    }

    public INodeProperty AddProperty(
        string name,
        string displayName,
        Type type,
        string? format = null,
        bool canConnectToPort = false)
    {
        var property = Properties
            .FirstOrDefault(p => p.Name == name && p.PropertyType == type);

        if (property != null) {
            property.CanConnectToPort = canConnectToPort;
            return property;
        }

        property = CreateProperty(name, displayName, type, format, canConnectToPort);
        return property;
    }
    
    public void Remove(IInputPort port)
    {
        RemoveInputPort(port);
    }
    
    public void Remove(IOutputPort port)
    {
        RemoveOutputPort(port);
    }
    
    public void Remove(INodeProperty property)
    {
        RemoveProperty(property);
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
            .Where(p => !excludeSet.Contains(p.Name))
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
            .Where(p => Properties.All(prop => prop != p))
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

        var type = GetType();

        // 동적 프로퍼티 정의 저장
        writer.WriteStartArray("DynamicProperties");
        foreach (var property in Properties)
        {
            // 어트리뷰트로 정의되지 않은 프로퍼티만 저장
            var propertyInfo = type.GetProperty(property.Name);
            if (propertyInfo?.GetCustomAttribute<NodePropertyAttribute>() == null && 
                property is IJsonSerializable)
            {
                writer.WriteStartObject();
                writer.WriteString("Name", property.Name);
                writer.WriteString("DisplayName", property.DisplayName);
                writer.WriteString("Type", property.PropertyType.AssemblyQualifiedName);
                writer.WriteString("Format", property.Format);
                writer.WriteBoolean("CanConnectToPort", property.CanConnectToPort);
                writer.WriteBoolean("IsVisible", property.IsVisible);
                
                // 값이 있는 경우에만 직렬화
                if (property.Value != null || property.PropertyType.IsValueType)
                {
                    writer.WritePropertyName("Value");
                    JsonSerializer.Serialize(writer, property.Value, property.PropertyType, NodeCanvasJsonConverter.SerializerOptions);
                }
                
                // 옵션 정보 저장
                if (property.Options.Any())
                {
                    writer.WriteStartArray("Options");
                    foreach (var option in property.Options)
                    {
                        writer.WriteStartObject();
                        writer.WriteString("OptionType", option.OptionType);
                        
                        if (option is IJsonSerializable jsonSerializable)
                        {
                            jsonSerializable.WriteJson(writer);
                        }
                        
                        writer.WriteEndObject();
                    }
                    writer.WriteEndArray();
                }
                
                writer.WriteEndObject();
            }
        }
        writer.WriteEndArray();

        // 동적으로 생성된 입력 포트 저장
        writer.WriteStartArray("DynamicInputPorts");
        foreach (var port in InputPorts)
        {
            // NodeProperty이거나 어트리뷰트로 정의된 포트는 제외
            if (Properties.Any(prop => prop.Value == port))
                continue;

            var propertyInfo = type.GetProperties()
                .FirstOrDefault(p => p.GetCustomAttribute<NodeInputAttribute>() != null && 
                                   p.GetValue(this) == port);
            
            if (propertyInfo == null && port is IJsonSerializable serializable)
            {
                writer.WriteStartObject();
                writer.WriteString("Name", port.Name);
                writer.WriteString("Type", port.DataType.AssemblyQualifiedName);
                writer.WriteNumber("Index", port.GetPortIndex());
                writer.WriteBoolean("IsVisible", port.IsVisible);
                writer.WriteEndObject();
            }
        }
        writer.WriteEndArray();

        // 동적으로 생성된 출력 포트 저장
        writer.WriteStartArray("DynamicOutputPorts");
        foreach (var port in OutputPorts)
        {
            var propertyInfo = type.GetProperties()
                .FirstOrDefault(p => p.GetCustomAttribute<NodeOutputAttribute>() != null && 
                                   p.GetValue(this) == port);
            
            if (propertyInfo == null && port is IJsonSerializable serializable)
            {
                writer.WriteStartObject();
                writer.WriteString("Name", port.Name);
                writer.WriteString("Type", port.DataType.AssemblyQualifiedName);
                writer.WriteNumber("Index", port.GetPortIndex());
                writer.WriteBoolean("IsVisible", port.IsVisible);
                writer.WriteEndObject();
            }
        }
        writer.WriteEndArray();
    }

    private IEnumerable<PortDefinition> CollectInputPortDefinitions(JsonElement element)
    {
        if (!element.TryGetProperty("DynamicInputPorts", out var inputPortDefinitions))
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

    private IEnumerable<PortDefinition> CollectOutputPortDefinitions(JsonElement element)
    {
        if (!element.TryGetProperty("DynamicOutputPorts", out var outputPortDefinitions))
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
        // 기본 속성 및 어트리뷰트 기반 프로퍼티 먼저 복원
        base.ReadJson(element, options);

        // 동적 프로퍼티 복원 시 기존 값 보존
        if (element.TryGetProperty("DynamicProperties", out var dynamicPropsElement))
        {
            foreach (var propElement in dynamicPropsElement.EnumerateArray())
            {
                if (propElement.TryGetProperty("Name", out var nameElement) &&
                    propElement.TryGetProperty("Type", out var typeElement))
                {
                    var name = nameElement.GetString();
                    var typeName = typeElement.GetString();

                    if (name != null && typeName != null)
                    {
                        var type = Type.GetType(typeName);
                        if (type != null)
                        {
                            // 기존 프로퍼티 확인
                            var existingProperty = Properties.FirstOrDefault(p => p.Name == name && p.PropertyType == type);
                            
                            // 프로퍼티 생성 또는 업데이트
                            var displayName = propElement.GetProperty("DisplayName").GetString() ?? name;
                            var format = propElement.GetProperty("Format").GetString();
                            var canConnectToPort = propElement.GetProperty("CanConnectToPort").GetBoolean();
                            
                            INodeProperty property;
                            if (existingProperty != null)
                            {
                                // 기존 프로퍼티 업데이트
                                existingProperty.CanConnectToPort = canConnectToPort;
                                property = existingProperty;
                            }
                            else
                            {
                                // 새 프로퍼티 생성
                                property = AddProperty(name, displayName, type, format, canConnectToPort);
                            }

                            // 값 복원
                            if (propElement.TryGetProperty("Value", out var valueElement))
                            {
                                property.Value = JsonSerializer.Deserialize(
                                    valueElement.GetRawText(),
                                    type,
                                    options);
                            }
                        }
                    }
                }
            }
        }

        // 포트 정의 복원
        RestorePortDefinitions(element, options);
    }

    private void RestorePortDefinitions(JsonElement element, JsonSerializerOptions options)
    {
        try
        {
            // 기존 포트와 프로퍼티 정의 복원
            var inputPorts = CollectInputPortDefinitions(element).ToList();
            var outputPorts = CollectOutputPortDefinitions(element).ToList();

            // 이미 존재하는 포트 이름 수집
            var existingPortNames = InputPorts.Select(p => p.Name)
                .Concat(OutputPorts.Select(p => p.Name))
                .ToHashSet();

            // 새로운 포트만 추가
            foreach (var portDef in inputPorts)
            {
                if (!existingPortNames.Contains(portDef.Name))
                {
                    var port = CreateInputPort(portDef.Name, portDef.Type);
                    port.IsVisible = portDef.IsVisible;
                }
            }

            foreach (var portDef in outputPorts)
            {
                if (!existingPortNames.Contains(portDef.Name))
                {
                    var port = CreateOutputPort(portDef.Name, portDef.Type);
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

            // 역직렬화 후 노드 초기화 - 필수 속성 추가 및 구성
            InitializeNode();
            
            Logger?.LogDebug($"{GetType().Name} 노드 역직렬화 및 초기화 완료");
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "DynamicNode 포트 정의 복원 중 오류 발생");
            throw;
        }
    }
}
