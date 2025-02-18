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

namespace WPFNode.Models;

public abstract class NodeBase : INode
{
    private string _name = string.Empty;
    private readonly string _category;
    private string _description = string.Empty;
    private double _x;
    private double _y;
    private bool _isVisible = true;
    private readonly List<IInputPort> _inputPorts = new();
    private readonly List<IOutputPort> _outputPorts = new();
    private readonly Dictionary<string, INodeProperty> _properties = new();
    private bool _isInitialized;
    private readonly INodeCanvas _canvas;

    [JsonConstructor]
    protected NodeBase(INodeCanvas canvas, Guid id)
    {
        _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
        Id      = id;

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

    public Guid Id { get; internal set; }

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

    public bool IsInitialized => _isInitialized;

    internal INodeCanvas Canvas => _canvas;

    public virtual void Initialize()
    {
        if (_isInitialized)
            return;

        _isInitialized = true;
    }

    protected virtual InputPort<T> CreateInputPort<T>(string name)
    {
        var portIndex = _inputPorts.Count;
        var port = new InputPort<T>(name, this, portIndex);
        RegisterInputPort(port);
        return port;
    }

    protected virtual IInputPort CreateInputPort(string name, Type type)
    {
        var portIndex = _inputPorts.Count;
        var port = (IInputPort)Activator.CreateInstance(
            typeof(InputPort<>).MakeGenericType(type), 
            name, this, portIndex)!;
        RegisterInputPort(port);
        return port;
    }

    protected virtual OutputPort<T> CreateOutputPort<T>(string name)
    {
        var portIndex = _outputPorts.Count;
        var port = new OutputPort<T>(name, this, portIndex);
        RegisterOutputPort(port);
        return port;
    }

    protected virtual IOutputPort CreateOutputPort(string name, Type type)
    {
        var portIndex = _outputPorts.Count;
        var port = (IOutputPort)Activator.CreateInstance(
            typeof(OutputPort<>).MakeGenericType(type), 
            name, this, portIndex)!;
        RegisterOutputPort(port);
        return port;
    }

    protected virtual NodeProperty<T> CreateProperty<T>(
        string name,
        string displayName,
        NodePropertyControlType controlType,
        string? format = null,
        bool canConnectToPort = false)
    {
        var property = new NodeProperty<T>(
            displayName,
            controlType,
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
        }

        return property;
    }

    protected virtual INodeProperty CreateProperty(
        string name,
        string displayName,
        Type type,
        NodePropertyControlType controlType,
        string? format = null,
        bool canConnectToPort = false)
    {
        var property = (INodeProperty)Activator.CreateInstance(
            typeof(NodeProperty<>).MakeGenericType(type),
            displayName, controlType, this, _inputPorts.Count, format, canConnectToPort)!;
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
        }

        return property;
    }

    private void UpdatePropertyPortVisibility(string propertyName, INodeProperty property)
    {
        if (property is IInputPort inputPort)
        {
            OnPropertyChanged(nameof(InputPorts));
        }
    }

    protected void RemoveProperty(string name)
    {
        if (_properties.TryGetValue(name, out var property))
        {
            if (property is IInputPort inputPort && _inputPorts.Contains(inputPort))
            {
                if (property.IsConnectedToPort)
                {
                    property.DisconnectFromPort();
                }
                _inputPorts.Remove(inputPort);
                OnPropertyChanged(nameof(InputPorts));
            }
        }
        _properties.Remove(name);
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
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

    public virtual NodeBase CreateCopy(double offsetX = 20, double offsetY = 20)
    {
        var copy = (NodeBase)MemberwiseClone();
        copy.Id = Guid.NewGuid();
        copy.X += offsetX;
        copy.Y += offsetY;

        copy._inputPorts.Clear();
        copy._outputPorts.Clear();
        copy.Initialize();

        return copy;
    }

    private void RegisterInputPort(IInputPort port)
    {
        if (port == null)
            throw new ArgumentNullException(nameof(port));
        _inputPorts.Add(port);
    }

    private void RegisterOutputPort(IOutputPort port)
    {
        if (port == null)
            throw new ArgumentNullException(nameof(port));
        _outputPorts.Add(port);
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

    public abstract Task ProcessAsync();

    public virtual void WriteJson(Utf8JsonWriter writer)
    {
        writer.WriteString("Id", Id.ToString());
        writer.WriteString("Type", GetType().AssemblyQualifiedName);
        writer.WriteString("Name", Name);
        writer.WriteString("Category", Category);
        writer.WriteString("Description", Description);
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
                writer.WriteString("Name", property.Value.DisplayName);
                writer.WriteString("DisplayName", property.Value.DisplayName);
                writer.WriteString("Type", property.Value.PropertyType.AssemblyQualifiedName);
                writer.WriteNumber("ControlType", (int)property.Value.ControlType);
                writer.WriteString("Format", property.Value.Format);
                writer.WriteBoolean("CanConnectToPort", property.Value.CanConnectToPort);
                writer.WriteBoolean("IsVisible", property.Value.IsVisible);
                
                // 값이 있는 경우에만 직렬화
                if (property.Value.Value != null || property.Value.PropertyType.IsValueType)
                {
                    writer.WritePropertyName("Value");
                    JsonSerializer.Serialize(writer, property.Value.Value, property.Value.PropertyType);
                }
                
                writer.WriteEndObject();
            }
        }
        writer.WriteEndArray();
    }

    public virtual void ReadJson(JsonElement element)
    {
        try
        {
            // 기본 속성 복원
            if (element.TryGetProperty("Name", out var nameElement))
                Name = nameElement.GetString() ?? Name;
            if (element.TryGetProperty("Description", out var descElement))
                Description = descElement.GetString() ?? Description;
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
                            serializable.ReadJson(propertyElement);
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
} 