using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using WPFNode.Attributes;
using WPFNode.Constants;
using WPFNode.Interfaces;
using WPFNode.Models.Properties;
using WPFNode.Models.Serialization;
using System.Windows;
using Microsoft.Extensions.Logging;

namespace WPFNode.Models;

public abstract class NodeBase : INode, INotifyPropertyChanged {
    private          string                            _id   = string.Empty;
    private          string                            _name = string.Empty;
    private readonly string                            _category;
    private          string                            _description = string.Empty;
    private          double                            _x;
    private          double                            _y;
    private          bool                              _isVisible   = true;
    private readonly List<IInputPort>                  _inputPorts  = new();
    private readonly List<IOutputPort>                 _outputPorts = new();
    private readonly Dictionary<string, INodeProperty> _properties  = new();
    private          bool                              _isInitialized;
    private readonly INodeCanvas                       _canvas;
    protected readonly ILogger? Logger;

    [JsonConstructor]
    protected NodeBase(INodeCanvas canvas, Guid guid, ILogger? logger = null)
    {
        _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
        Guid      = guid;
        Logger = logger;

        // 어트리뷰트에서 직접 값을 가져옴
        var type = GetType();
        var nameAttr = type.GetCustomAttribute<NodeNameAttribute>();
        var categoryAttr = type.GetCustomAttribute<NodeCategoryAttribute>();
        var descriptionAttr = type.GetCustomAttribute<NodeDescriptionAttribute>();

        _name = nameAttr?.Name ?? type.Name;
        _category = categoryAttr?.Category ?? "Basic";
        _description = descriptionAttr?.Description ?? string.Empty;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public Guid Guid { get; private set; }
    
    public string Id
    {
        get => _id;
        set => SetField(ref _id, value);
    }

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    public virtual string Category => _category;
    
    public string Description
    {
        get => _description;
        set => SetField(ref _description, value);
    }
    
    public double X
    {
        get => _x;
        set => SetField(ref _x, value);
    }
    
    public double Y
    {
        get => _y;
        set => SetField(ref _y, value);
    }

    public bool IsVisible
    {
        get => _isVisible;
        set => SetField(ref _isVisible, value);
    }

    public bool IsOutputNode => GetType().GetCustomAttribute<OutputNodeAttribute>() != null;

    public IReadOnlyList<IInputPort> InputPorts => _inputPorts;
    public IReadOnlyList<IOutputPort> OutputPorts => _outputPorts;
    public IReadOnlyDictionary<string, INodeProperty> Properties => _properties;

    internal INodeCanvas Canvas => _canvas;

    protected InputPort<T> CreateInputPort<T>(string name)
    {
        var portIndex = _inputPorts.Count;
        var port = new InputPort<T>(name, this, portIndex);
        RegisterInputPort(port);
        return port;
    }

    protected IInputPort CreateInputPort(string name, Type type)
    {
        var portIndex = _inputPorts.Count;
        var port = (IInputPort)Activator.CreateInstance(
            typeof(InputPort<>).MakeGenericType(type), 
            name, this, portIndex)!;
        RegisterInputPort(port);
        return port;
    }

    protected OutputPort<T> CreateOutputPort<T>(string name)
    {
        var portIndex = _outputPorts.Count;
        var port = new OutputPort<T>(name, this, portIndex);
        RegisterOutputPort(port);
        return port;
    }

    protected IOutputPort CreateOutputPort(string name, Type type)
    {
        var portIndex = _outputPorts.Count;
        var port = (IOutputPort)Activator.CreateInstance(
            typeof(OutputPort<>).MakeGenericType(type), 
            name, this, portIndex)!;
        RegisterOutputPort(port);
        return port;
    }

    protected NodeProperty<T> CreateProperty<T>(
        string name,
        string displayName,
        string? format = null,
        bool canConnectToPort = false)
    {
        var property = new NodeProperty<T>(
            name,
            displayName,
            this,
            _inputPorts.Count,
            format,
            canConnectToPort);
        _properties[name] = property;

        // 프로퍼티 변경 이벤트 구독
        if (property is INotifyPropertyChanged notifyPropertyChanged)
        {
            notifyPropertyChanged.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(INodeProperty.CanConnectToPort))
                {
                    UpdatePropertyPortVisibility(name, property);
                }
            };
        }
        
        // 항상 InputPort로 등록
        if (property is IInputPort inputPort)
        {
            _inputPorts.Add(inputPort);
            OnPropertyChanged(nameof(InputPorts));
        }

        OnPropertyChanged(nameof(Properties));
        return property;
    }

    protected INodeProperty CreateProperty(
        string name,
        string displayName,
        Type type,
        string? format = null,
        bool canConnectToPort = false)
    {
        var property = (INodeProperty)Activator.CreateInstance(
            typeof(NodeProperty<>).MakeGenericType(type),
            name, displayName, this, _inputPorts.Count, format, canConnectToPort)!;
        _properties[name] = property;

        // 프로퍼티 변경 이벤트 구독
        if (property is INotifyPropertyChanged notifyPropertyChanged)
        {
            notifyPropertyChanged.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(INodeProperty.CanConnectToPort))
                {
                    UpdatePropertyPortVisibility(name, property);
                }
            };
        }
        
        // 항상 InputPort로 등록
        if (property is IInputPort inputPort)
        {
            _inputPorts.Add(inputPort);
            OnPropertyChanged(nameof(InputPorts));
        }

        OnPropertyChanged(nameof(Properties));
        return property;
    }

    private void UpdatePropertyPortVisibility(string propertyName, INodeProperty property)
    {
        if (property is IInputPort inputPort)
        {
            OnPropertyChanged(nameof(InputPorts));
        }
    }
    
    protected void RemoveInputPort(IInputPort port) 
    {
        if (_inputPorts.Contains(port)) {
            port.Disconnect();
            _inputPorts.Remove(port);
            OnPropertyChanged(nameof(InputPorts));
        }
    }
    
    protected void RemoveOutputPort(IOutputPort port) 
    {
        if (_outputPorts.Contains(port))
        {
            _outputPorts.Remove(port);
            OnPropertyChanged(nameof(OutputPorts));
        }
    }

    protected void RemoveProperty(string name) {
        if (!_properties.TryGetValue(name, out var property))
            return;
        
        if (property is IInputPort inputPort && _inputPorts.Contains(inputPort))
        {
            if (inputPort.IsConnected)
            {
                inputPort.Disconnect();
            }
            _inputPorts.Remove(inputPort);
            OnPropertyChanged(nameof(InputPorts));
        }
        
        _properties.Remove(name);
        OnPropertyChanged(nameof(Properties));
    }
    
    protected void RemoveProperty(INodeProperty property)
    {
        if (property == null)
            throw new ArgumentNullException(nameof(property));
        
        var entry = _properties.FirstOrDefault(x => x.Value == property);
        if (!string.IsNullOrEmpty(entry.Key))
        {
            RemoveProperty(entry.Key);
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    public virtual bool CanExecuteCommand(string commandName, object? parameter = null)
    {
        return false;
    }

    public virtual void ExecuteCommand(string commandName, object? parameter = null)
    {
        throw new NotSupportedException($"명령 {commandName}을(를) 처리할 수 없습니다.");
    }

    public NodeBase CreateCopy(double offsetX = 20, double offsetY = 20)
    {
        var copy = (NodeBase)MemberwiseClone();
        copy.Guid = Guid.NewGuid();
        copy.X += offsetX;
        copy.Y += offsetY;

        copy._inputPorts.Clear();
        copy._outputPorts.Clear();

        return copy;
    }

    private void RegisterInputPort(IInputPort port)
    {
        if (port == null)
            throw new ArgumentNullException(nameof(port));
        _inputPorts.Add(port);
        OnPropertyChanged(nameof(InputPorts));
    }

    private void RegisterOutputPort(IOutputPort port)
    {
        if (port == null)
            throw new ArgumentNullException(nameof(port));
        _outputPorts.Add(port);
        OnPropertyChanged(nameof(OutputPorts));
    }

    protected void ClearPorts()
    {
        _inputPorts.Clear();
        _outputPorts.Clear();
        OnPropertyChanged(nameof(InputPorts));
        OnPropertyChanged(nameof(OutputPorts));
    }

    protected void ClearProperties()
    {
        foreach (var prop in Properties.Keys.ToList())
        {
            RemoveProperty(prop);
        }
    }

    protected void ResetNode()
    {
        ClearPorts();
        ClearProperties();
    }

    public virtual async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        Logger?.LogDebug("Executing node {NodeName}", Name);
        await ProcessAsync(cancellationToken);
    }

    protected abstract Task ProcessAsync(CancellationToken cancellationToken = default);

    public virtual void WriteJson(Utf8JsonWriter writer)
    {
        writer.WriteString("Guid", Guid.ToString());
        writer.WriteString("Id", Id);
        writer.WriteString("Type", GetType().AssemblyQualifiedName);
        writer.WriteNumber("X", X);
        writer.WriteNumber("Y", Y);
        writer.WriteBoolean("IsVisible", IsVisible);
        
        // 프로퍼티 정보를 배열로 저장
        writer.WriteStartArray("Properties");
        foreach (var property in Properties)
        {
            if (property.Value is IJsonSerializable serializable)
            {
                writer.WriteStartObject();
                writer.WriteString("Key", property.Key);
                writer.WriteString("DisplayName", property.Value.DisplayName);
                writer.WriteString("Type", property.Value.PropertyType.AssemblyQualifiedName);
                writer.WriteString("Format", property.Value.Format);
                writer.WriteBoolean("CanConnectToPort", property.Value.CanConnectToPort);
                writer.WriteBoolean("IsVisible", property.Value.IsVisible);
                
                // 값이 있는 경우에만 직렬화
                if (property.Value.Value != null || property.Value.PropertyType.IsValueType)
                {
                    writer.WritePropertyName("Value");
                    JsonSerializer.Serialize(writer, property.Value.Value, property.Value.PropertyType, NodeCanvasJsonConverter.SerializerOptions);
                }
                
                writer.WriteEndObject();
            }
        }
        writer.WriteEndArray();
    }

    public virtual void ReadJson(JsonElement element, JsonSerializerOptions options)
    {
        try
        {
            // 기본 속성 복원
            if (element.TryGetProperty("Id", out var idElement))
                Id = idElement.GetString() ?? string.Empty;
            if (element.TryGetProperty("X", out var xElement))
                X = xElement.GetDouble();
            if (element.TryGetProperty("Y", out var yElement))
                Y = yElement.GetDouble();
            if (element.TryGetProperty("IsVisible", out var visibleElement))
                IsVisible = visibleElement.GetBoolean();

            // 프로퍼티 상태 복원
            if (element.TryGetProperty("Properties", out var propertiesElement))
            {
                foreach (var propertyElement in propertiesElement.EnumerateArray())
                {
                    if (propertyElement.TryGetProperty("Key", out var keyElement))
                    {
                        var key = keyElement.GetString();
                        if (key != null && Properties.TryGetValue(key, out var property) && 
                            property is IJsonSerializable serializable)
                        {
                            serializable.ReadJson(propertyElement, options);
                        }
                    }
                }
            }
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new JsonException($"노드 {Name} ({GetType().Name}) 역직렬화 중 오류 발생", ex);
        }
    }

    public virtual async Task SetParameterAsync(object parameter)
    {
        // 기본 구현은 아무것도 하지 않습니다.
        // 파생 클래스에서 필요한 경우 이 메서드를 재정의하여 파라미터를 처리할 수 있습니다.
        await Task.CompletedTask;
    }
} 
