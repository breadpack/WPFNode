using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using System.Threading.Tasks.Sources;
using WPFNode.Attributes;
using WPFNode.Constants;
using WPFNode.Interfaces;
using WPFNode.Models.Properties;
using WPFNode.Models.Serialization;
using System.Windows;
using Microsoft.Extensions.Logging;

namespace WPFNode.Models;

public abstract class NodeBase : INode, INotifyPropertyChanged {
    private            string              _id   = string.Empty;
    private            string              _name = string.Empty;
    private readonly   string              _category;
    private            string              _description = string.Empty;
    private            double              _x;
    private            double              _y;
    private            bool                _isVisible   = true;
    private readonly   List<IInputPort>    _inputPorts  = new();
    private readonly   List<IOutputPort>   _outputPorts = new();
    private readonly   List<IFlowInPort>   _flowInPorts = new();
    private readonly   List<IFlowOutPort>  _flowOutPorts = new();
    private readonly   List<INodeProperty> _properties  = new();
    private            bool                _isInitialized;
    private readonly   INodeCanvas         _canvas;
    protected readonly ILogger?            Logger;

    [JsonConstructor]
    protected NodeBase(INodeCanvas canvas, Guid guid, ILogger? logger = null) {
        _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
        Guid    = guid;
        Logger  = logger;

        // 어트리뷰트에서 직접 값을 가져옴
        var type            = GetType();
        var nameAttr        = type.GetCustomAttribute<NodeNameAttribute>();
        var categoryAttr    = type.GetCustomAttribute<NodeCategoryAttribute>();
        var descriptionAttr = type.GetCustomAttribute<NodeDescriptionAttribute>();

        _name        = nameAttr?.Name ?? type.Name;
        _category    = categoryAttr?.Category ?? "Basic";
        _description = descriptionAttr?.Description ?? string.Empty;

        InitializeFromAttributes();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public Guid Guid { get; private set; }

    public string Id {
        get => _id;
        set => SetField(ref _id, value);
    }

    public string Name {
        get => _name;
        set => SetField(ref _name, value);
    }

    public virtual string Category => _category;

    public string Description {
        get => _description;
        set => SetField(ref _description, value);
    }

    public double X {
        get => _x;
        set => SetField(ref _x, value);
    }

    public double Y {
        get => _y;
        set => SetField(ref _y, value);
    }

    public bool IsVisible {
        get => _isVisible;
        set => SetField(ref _isVisible, value);
    }

    public bool IsOutputNode => GetType().GetCustomAttribute<OutputNodeAttribute>() != null;

    public IReadOnlyList<IInputPort>    InputPorts  => _inputPorts;
    public IReadOnlyList<IOutputPort>   OutputPorts => _outputPorts;
    public IReadOnlyList<IFlowInPort>   FlowInPorts => _flowInPorts;
    public IReadOnlyList<IFlowOutPort>  FlowOutPorts => _flowOutPorts;
    public IReadOnlyList<INodeProperty> Properties  => _properties;

    internal INodeCanvas Canvas => _canvas;

    internal InputPort<T> CreateInputPort<T>(string name) {
        var portIndex = _inputPorts.Count;
        var port      = new InputPort<T>(name, this, portIndex);
        RegisterInputPort(port);
        return port;
    }

    internal IInputPort CreateInputPort(string name, Type type) {
        var portIndex = _inputPorts.Count;
        var port = (IInputPort)Activator.CreateInstance(
            typeof(InputPort<>).MakeGenericType(type),
            name, this, portIndex)!;
        RegisterInputPort(port);
        return port;
    }

    internal OutputPort<T> CreateOutputPort<T>(string name) {
        var portIndex = _outputPorts.Count;
        var port      = new OutputPort<T>(name, this, portIndex);
        RegisterOutputPort(port);
        return port;
    }

    internal IOutputPort CreateOutputPort(string name, Type type) {
        var portIndex = _outputPorts.Count;
        var port = (IOutputPort)Activator.CreateInstance(
            typeof(OutputPort<>).MakeGenericType(type),
            name, this, portIndex)!;
        RegisterOutputPort(port);
        return port;
    }

    internal FlowInPort CreateFlowInPort(string name) {
        var portIndex = _flowInPorts.Count;
        var port = new FlowInPort(name, this, portIndex);
        RegisterFlowInPort(port);
        return port;
    }

    internal FlowOutPort CreateFlowOutPort(string name) {
        var portIndex = _flowOutPorts.Count;
        var port = new FlowOutPort(name, this, portIndex);
        RegisterFlowOutPort(port);
        return port;
    }

    internal NodeProperty<T> CreateProperty<T>(
        string  name,
        string  displayName,
        string? format           = null,
        bool    canConnectToPort = false
    ) {
        if(_properties.Exists(p => p.Name == name))
            throw new InvalidOperationException($"Property with name '{name}' already exists.");
        
        var property = new NodeProperty<T>(
            name,
            displayName,
            this,
            _inputPorts.Count,
            format,
            canConnectToPort);
        
        _properties.Add(property);

        // 프로퍼티 변경 이벤트 구독
        if (property is INotifyPropertyChanged notifyPropertyChanged) {
            notifyPropertyChanged.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(INodeProperty.CanConnectToPort)) {
                    UpdatePropertyPortVisibility(name, property);
                }
            };
        }

        // 항상 InputPort로 등록
        if (property is IInputPort inputPort) {
            _inputPorts.Add(inputPort);
            OnPropertyChanged(nameof(InputPorts));
        }

        OnPropertyChanged(nameof(Properties));
        return property;
    }

    internal INodeProperty CreateProperty(
        string  name,
        string  displayName,
        Type    type,
        string? format           = null,
        bool    canConnectToPort = false
    ) {
        if(_properties.Exists(p => p.Name == name && p.PropertyType == type))
            throw new InvalidOperationException($"Property with name '{name}' already exists.");
        
        var property = (INodeProperty)Activator.CreateInstance(
            typeof(NodeProperty<>).MakeGenericType(type),
            name, displayName, this, _inputPorts.Count, format, canConnectToPort)!;
        
        _properties.Add(property);

        // 프로퍼티 변경 이벤트 구독
        if (property is INotifyPropertyChanged notifyPropertyChanged) {
            notifyPropertyChanged.PropertyChanged += (s, e) => {
                if (e.PropertyName == nameof(INodeProperty.CanConnectToPort)) {
                    UpdatePropertyPortVisibility(name, property);
                }
            };
        }

        // 항상 InputPort로 등록
        if (property is IInputPort inputPort) {
            _inputPorts.Add(inputPort);
            OnPropertyChanged(nameof(InputPorts));
        }

        OnPropertyChanged(nameof(Properties));
        return property;
    }

    private void UpdatePropertyPortVisibility(string propertyName, INodeProperty property) {
        if (property is IInputPort inputPort) {
            OnPropertyChanged(nameof(InputPorts));
        }
    }

    internal void RemoveInputPort(IInputPort port) {
        if (_inputPorts.Contains(port)) {
            port.Disconnect();
            _inputPorts.Remove(port);
            OnPropertyChanged(nameof(InputPorts));
        }
    }

    internal void RemoveOutputPort(IOutputPort port) {
        if (_outputPorts.Contains(port)) {
            port.Disconnect();
            _outputPorts.Remove(port);
            OnPropertyChanged(nameof(OutputPorts));
        }
    }

    internal void RemoveProperty(string name) {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Property name cannot be null or empty.", nameof(name));
        
        // 프로퍼티가 존재하지 않으면 아무것도 하지 않음
        var property = _properties.FirstOrDefault(x => x.Name == name);
        if (property == null)
            return;

        RemoveProperty(property);
        OnPropertyChanged(nameof(Properties));
    }

    internal void RemoveProperty(INodeProperty property) {
        if (property == null)
            throw new ArgumentNullException(nameof(property));
        
        // 프로퍼티가 존재하지 않으면 아무것도 하지 않음
        if (!_properties.Contains(property))
            return;
        
        // 프로퍼티가 InputPort인 경우 연결 해제
        if (property is IInputPort inputPort && _inputPorts.Contains(inputPort)) {
            if (inputPort.IsConnected) {
                inputPort.Disconnect();
            }

            _inputPorts.Remove(inputPort);
            OnPropertyChanged(nameof(InputPorts));
        }
        
        _properties.Remove(property);
        OnPropertyChanged(nameof(Properties));
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null) {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    public virtual bool CanExecuteCommand(string commandName, object? parameter = null) {
        return false;
    }

    public virtual void ExecuteCommand(string commandName, object? parameter = null) {
        throw new NotSupportedException($"명령 {commandName}을(를) 처리할 수 없습니다.");
    }

    public NodeBase CreateCopy(double offsetX = 20, double offsetY = 20) {
        var copy = (NodeBase)MemberwiseClone();
        copy.Guid =  Guid.NewGuid();
        copy.X    += offsetX;
        copy.Y    += offsetY;

        copy._inputPorts.Clear();
        copy._outputPorts.Clear();

        return copy;
    }

    private void RegisterInputPort(IInputPort port) {
        if (port == null)
            throw new ArgumentNullException(nameof(port));
        _inputPorts.Add(port);
        OnPropertyChanged(nameof(InputPorts));
    }

    private void RegisterOutputPort(IOutputPort port) {
        if (port == null)
            throw new ArgumentNullException(nameof(port));
        _outputPorts.Add(port);
        OnPropertyChanged(nameof(OutputPorts));
    }

    internal void RemoveFlowInPort(IFlowInPort port) {
        if (_flowInPorts.Contains(port)) {
            port.Disconnect();
            _flowInPorts.Remove(port);
            OnPropertyChanged(nameof(FlowInPorts));
        }
    }

    internal void RemoveFlowOutPort(IFlowOutPort port) {
        if (_flowOutPorts.Contains(port)) {
            port.Disconnect();
            _flowOutPorts.Remove(port);
            OnPropertyChanged(nameof(FlowOutPorts));
        }
    }

    private void RegisterFlowInPort(IFlowInPort port) {
        if (port == null)
            throw new ArgumentNullException(nameof(port));
        _flowInPorts.Add(port);
        OnPropertyChanged(nameof(FlowInPorts));
    }

    private void RegisterFlowOutPort(IFlowOutPort port) {
        if (port == null)
            throw new ArgumentNullException(nameof(port));
        _flowOutPorts.Add(port);
        OnPropertyChanged(nameof(FlowOutPorts));
    }

    protected void ClearPorts() {
        _inputPorts.Clear();
        _outputPorts.Clear();
        _flowInPorts.Clear();
        _flowOutPorts.Clear();
        OnPropertyChanged(nameof(InputPorts));
        OnPropertyChanged(nameof(OutputPorts));
        OnPropertyChanged(nameof(FlowInPorts));
        OnPropertyChanged(nameof(FlowOutPorts));
    }

    protected void ClearProperties() {
        foreach (var prop in Properties) {
            RemoveProperty(prop);
        }
    }

    protected void ResetNode() {
        ClearPorts();
        ClearProperties();
    }

    /// <summary>
    /// 노드의 실제 처리 로직을 구현합니다.
    /// 상속받은 클래스에서 반드시 구현해야 합니다.
    /// 이 메서드는 실행 중에 활성화할 FlowOutPort를 순차적으로 yield return해야 합니다.
    /// </summary>
    public abstract IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        Models.Execution.FlowExecutionContext? context,
        CancellationToken cancellationToken = default);

    public virtual void WriteJson(Utf8JsonWriter writer) {
        writer.WriteString("Guid", Guid.ToString());
        writer.WriteString("Id", Id);
        writer.WriteString("Type", GetType().AssemblyQualifiedName);
        writer.WriteNumber("X", X);
        writer.WriteNumber("Y", Y);
        writer.WriteBoolean("IsVisible", IsVisible);

        // 프로퍼티 정보를 배열로 저장
        writer.WriteStartArray("Properties");
        foreach (var property in Properties) {
            if (property is IJsonSerializable serializable) {
                writer.WriteStartObject();
                writer.WriteString("Key", property.Name);
                writer.WriteString("DisplayName", property.DisplayName);
                writer.WriteString("Type", property.PropertyType.AssemblyQualifiedName);
                writer.WriteString("Format", property.Format);
                writer.WriteBoolean("CanConnectToPort", property.CanConnectToPort);
                writer.WriteBoolean("IsVisible", property.IsVisible);

                // 값이 있는 경우에만 직렬화
                if (property.Value != null || property.PropertyType.IsValueType) {
                    writer.WritePropertyName("Value");
                    JsonSerializer.Serialize(writer, property.Value, property.PropertyType, NodeCanvasJsonConverter.SerializerOptions);
                }

                writer.WriteEndObject();
            }
        }

        writer.WriteEndArray();
    }

    public virtual void ReadJson(JsonElement element, JsonSerializerOptions options) {
        try {
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
            if (element.TryGetProperty("Properties", out var propertiesElement)) {
                foreach (var propertyElement in propertiesElement.EnumerateArray()) {
                    if (propertyElement.TryGetProperty("Key", out var keyElement)) {
                        var key = keyElement.GetString();
                        if (string.IsNullOrEmpty(key))
                            continue;
                        
                        // 프로퍼티 이름으로 프로퍼티 찾기
                        var property = Properties.FirstOrDefault(p => p.Name == key);
                        if (property == null)
                            continue;
                        
                        if (property is IJsonSerializable serializable) {
                            serializable.ReadJson(propertyElement, options);
                        }
                    }
                }
            }
        }
        catch (JsonException) {
            throw;
        }
        catch (Exception ex) {
            throw new JsonException($"노드 {Name} ({GetType().Name}) 역직렬화 중 오류 발생", ex);
        }
    }

    private void InitializeFromAttributes() {
        var type = GetType();
        InitializeNodeProperties(type);
        InitializeInputPorts(type);
        InitializeOutputPorts(type);
        InitializeFlowInPorts(type);
        InitializeFlowOutPorts(type);
    }

    private void InitializeFlowInPorts(Type type) {
        var flowInPorts = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                             .Where(p => p.GetCustomAttribute<NodeFlowInAttribute>() != null)
                             .Where(p => p.GetValue(this) == null);

        foreach (var property in flowInPorts) {
            // 이미 초기화된 속성은 건너뜀
            if (property.GetValue(this) != null)
                continue;

            var flowInAttr = property.GetCustomAttribute<NodeFlowInAttribute>();
            if (flowInAttr != null) {
                var port = CreateFlowInPort(flowInAttr.DisplayName ?? property.Name);

                // 멤버 프로퍼티에 할당
                if (property.CanWrite) {
                    property.SetValue(this, port);
                }
            }
        }
    }

    private void InitializeFlowOutPorts(Type type) {
        var flowOutPorts = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                             .Where(p => p.GetCustomAttribute<NodeFlowOutAttribute>() != null)
                             .Where(p => p.GetValue(this) == null);

        foreach (var property in flowOutPorts) {
            // 이미 초기화된 속성은 건너뜀
            if (property.GetValue(this) != null)
                continue;

            var flowOutAttr = property.GetCustomAttribute<NodeFlowOutAttribute>();
            if (flowOutAttr != null) {
                var port = CreateFlowOutPort(flowOutAttr.DisplayName ?? property.Name);

                // 멤버 프로퍼티에 할당
                if (property.CanWrite) {
                    property.SetValue(this, port);
                }
            }
        }
    }

    private void InitializeOutputPorts(Type type) {
        var outputPorts = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                              .Where(p => p.GetCustomAttribute<NodeOutputAttribute>() != null)
                              .Where(p => p.GetValue(this) == null);
        // OutputPort 속성 처리
        foreach (var property in outputPorts) {
            // 이미 초기화된 속성은 건너뜀
            if (property.GetValue(this) != null)
                continue;

            var outputAttr = property.GetCustomAttribute<NodeOutputAttribute>();
            if (outputAttr != null) {
                var port = CreateOutputPort(
                    outputAttr.DisplayName ?? property.Name,
                    property.PropertyType.GenericTypeArguments[0]);

                // 멤버 프로퍼티에 할당
                if (property.CanWrite) {
                    property.SetValue(this, port);
                }
            }
        }
    }

    private void InitializeInputPorts(Type type) {
        var inputPorts = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                             .Where(p => p.GetCustomAttribute<NodeInputAttribute>() != null)
                             .Where(p => p.GetValue(this) == null);
        // InputPort 속성 처리
        foreach (var property in inputPorts) {
            // 이미 초기화된 속성은 건너뜀
            if (property.GetValue(this) != null)
                continue;

            var inputAttr = property.GetCustomAttribute<NodeInputAttribute>();
            if (inputAttr != null) {
                var port = CreateInputPort(
                    inputAttr.DisplayName ?? property.Name,
                    property.PropertyType.GenericTypeArguments[0]);

                // 멤버 프로퍼티에 할당
                if (property.CanWrite) {
                    property.SetValue(this, port);
                }
            }
        }
    }

    private void InitializeNodeProperties(Type type) {
        var nodeProperties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                 .Where(p => p.GetCustomAttribute<NodePropertyAttribute>() != null)
                                 .Where(p => p.GetValue(this) == null);

        foreach (var property in nodeProperties) {
            // 이미 초기화된 속성은 건너뜀
            if (property.GetValue(this) != null)
                continue;

            // NodeProperty 속성 처리
            var propAttr = property.GetCustomAttribute<NodePropertyAttribute>();
            if (propAttr != null) {
                var nodeProp = CreateProperty(
                    property.Name,
                    propAttr.DisplayName ?? property.Name,
                    property.PropertyType.GenericTypeArguments[0],
                    propAttr.Format,
                    propAttr.CanConnectToPort);

                // OnValueChanged 메서드 연결
                if (!string.IsNullOrEmpty(propAttr.OnValueChanged)) {
                    var method = type.GetMethod(propAttr.OnValueChanged,
                                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    if (method != null) {
                        if (nodeProp is INotifyPropertyChanged notifyPropertyChanged) {
                            notifyPropertyChanged.PropertyChanged += (s, e) => {
                                if (e.PropertyName == nameof(INodeProperty.Value)) {
                                    method.Invoke(this, null);
                                }
                            };
                        }
                    }
                    else {
                        Logger?.LogWarning("Method {MethodName} not found in {NodeType}",
                                           propAttr.OnValueChanged, GetType().Name);
                    }
                }

                // 멤버 프로퍼티에 할당
                if (property.CanWrite) {
                    property.SetValue(this, nodeProp);
                }
            }
        }
    }
}
