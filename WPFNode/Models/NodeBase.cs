using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models.Properties;
using WPFNode.Models.Serialization;
using Microsoft.Extensions.Logging;

namespace WPFNode.Models;

public abstract class NodeBase : INode, INotifyPropertyChanged {
    private            string              _id   = string.Empty;
    private            string              _name = string.Empty;
    private            string              _category = string.Empty;
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

    // DynamicNode에서 통합된 필드
    private bool                _isReconfiguring   = false;
    private List<IPort>         _dynamicPorts      = new();
    private List<INodeProperty> _dynamicProperties = new();
    
    /// <summary>
    /// 현재 구성 중 사용된 포트와 프로퍼티를 추적합니다.
    /// </summary>
    private HashSet<object>     _usedObjects = new();

    /// <summary>
    /// 노드가 현재 재구성 중인지 여부를 나타냅니다.
    /// </summary>
    public bool IsReconfiguring => _isReconfiguring;

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
        return commandName switch {
            "EnableNode"  => !IsVisible,
            "DisableNode" => IsVisible,
            _                       => false
        };
    }

    public virtual void ExecuteCommand(string commandName, object? parameter = null) {
        switch (commandName) {
            case "EnableNode":
                IsVisible = true;
                break;
            case "DisableNode":
                IsVisible = false;
                break;
        }
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
        // 노드 타입 기록
        writer.WriteString("Type", GetType().AssemblyQualifiedName);
        
        // 기본 속성 기록
        writer.WriteString("Guid", Guid.ToString());
        writer.WriteString("Id", Id);
        writer.WriteString("Name", Name);
        writer.WriteString("Description", Description);
        writer.WriteNumber("X", X);
        writer.WriteNumber("Y", Y);
        writer.WriteBoolean("IsVisible", IsVisible);
        
        // 속성 값 직렬화
        WritePropertyValues(writer);
        
        // 동적 프로퍼티 정의 저장
        writer.WriteStartArray("DynamicProperties");
        foreach (var property in _dynamicProperties)
        {
            if (property is IJsonSerializable)
            {
                writer.WriteStartObject();
                writer.WriteString("Name", property.Name);
                writer.WriteString("DisplayName", property.DisplayName);
                writer.WriteString("Type", property.PropertyType.AssemblyQualifiedName);
                writer.WriteString("Format", property.Format);
                writer.WriteBoolean("CanConnectToPort", property.CanConnectToPort);
                
                // 값이 있는 경우에만 직렬화
                if (property.Value != null || property.PropertyType.IsValueType)
                {
                    writer.WritePropertyName("Value");
                    JsonSerializer.Serialize(writer, property.Value, property.PropertyType, NodeCanvasJsonConverter.SerializerOptions);
                }
                
                writer.WriteEndObject();
            }
        }
        writer.WriteEndArray();
        
        // 동적 포트 정의 저장 (Flow In)
        writer.WriteStartArray("DynamicFlowInPorts");
        foreach (var port in _dynamicPorts.OfType<FlowInPort>())
        {
            writer.WriteStartObject();
            writer.WriteString("Name", port.Name);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        
        // 동적 포트 정의 저장 (Flow Out)
        writer.WriteStartArray("DynamicFlowOutPorts");
        foreach (var port in _dynamicPorts.OfType<FlowOutPort>())
        {
            writer.WriteStartObject();
            writer.WriteString("Name", port.Name);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        
        // 동적 포트 정의 저장 (Input)
        writer.WriteStartArray("DynamicInputPorts");
        foreach (var port in _dynamicPorts.OfType<IInputPort>())
        {
            writer.WriteStartObject();
            writer.WriteString("Name", port.Name);
            writer.WriteString("Type", port.DataType.AssemblyQualifiedName);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        
        // 동적 포트 정의 저장 (Output)
        writer.WriteStartArray("DynamicOutputPorts");
        foreach (var port in _dynamicPorts.OfType<IOutputPort>().Where(p => !(p is FlowOutPort)))
        {
            writer.WriteStartObject();
            writer.WriteString("Name", port.Name);
            writer.WriteString("Type", port.DataType.AssemblyQualifiedName);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }

    protected void WritePropertyValues(Utf8JsonWriter writer) {
        writer.WriteStartArray("PropertyValues");
        
        foreach (var property in Properties) {
            if (property.Value == null && !property.PropertyType.IsValueType)
                continue;
                
            writer.WriteStartObject();
            writer.WriteString("Name", property.Name);
            
            // 값 직렬화
            writer.WritePropertyName("Value");
            JsonSerializer.Serialize(writer, property.Value, property.PropertyType, NodeCanvasJsonConverter.SerializerOptions);
            
            writer.WriteEndObject();
        }
        
        writer.WriteEndArray();
    }

    public virtual void ReadJson(JsonElement element, JsonSerializerOptions options) {
        // 재구성 플래그 설정 - 속성 값 복원 과정에서 ReconfigurePorts 호출 방지
        _isReconfiguring = true;
        
        try
        {
            // 기본 ID 속성 복원
            string? id = null;
            
            if (element.TryGetProperty("Id", out var idElement)) {
                id = idElement.GetString();
                Id = id ?? string.Empty;
            }
            
            // 기본 속성 복원
            if (element.TryGetProperty("Name", out var nameElement))
                Name = nameElement.GetString() ?? string.Empty;
            
            if (element.TryGetProperty("Description", out var descElement))
                Description = descElement.GetString() ?? string.Empty;
                
            if (element.TryGetProperty("X", out var xElement))
                X = xElement.GetDouble();
                
            if (element.TryGetProperty("Y", out var yElement))
                Y = yElement.GetDouble();
                
            if (element.TryGetProperty("IsVisible", out var visibleElement))
                IsVisible = visibleElement.GetBoolean();
            
            // 동적 포트/프로퍼티 제거
            ClearDynamicPorts();
            
            try
            {
                // 동적 프로퍼티 복원
                if (element.TryGetProperty("DynamicProperties", out var dynamicPropsElement))
                {
                    RestoreDynamicProperties(dynamicPropsElement, options);
                }
                
                // Flow In 포트 복원
                if (element.TryGetProperty("DynamicFlowInPorts", out var flowInPortsElement))
                {
                    RestoreFlowInPorts(flowInPortsElement);
                }
                
                // Flow Out 포트 복원
                if (element.TryGetProperty("DynamicFlowOutPorts", out var flowOutPortsElement))
                {
                    RestoreFlowOutPorts(flowOutPortsElement);
                }
                
                // 입력 포트 복원
                if (element.TryGetProperty("DynamicInputPorts", out var inputPortsElement))
                {
                    RestoreInputPorts(inputPortsElement);
                }
                
                // 출력 포트 복원
                if (element.TryGetProperty("DynamicOutputPorts", out var outputPortsElement))
                {
                    RestoreOutputPorts(outputPortsElement);
                }
                
                // 속성 값 복원 - 마지막에 수행하여 이벤트 핸들러가 한 번만 발생하도록 함
                ReadPropertyValues(element, options);
                
                // 역직렬화 후 노드 초기화
                _isInitialized = false;
                InitializeNode();
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "노드 포트/프로퍼티 복원 중 오류 발생");
            }
        }
        finally
        {
            // 재구성 플래그 해제
            _isReconfiguring = false;
        }
    }
    
    // 동적 프로퍼티 복원
    private void RestoreDynamicProperties(JsonElement element, JsonSerializerOptions options)
    {
        foreach (var propElement in element.EnumerateArray())
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
                        var displayName = propElement.GetProperty("DisplayName").GetString() ?? name;
                        var format = propElement.GetProperty("Format").GetString();
                        var canConnectToPort = propElement.GetProperty("CanConnectToPort").GetBoolean();
                        
                        // 새 프로퍼티 생성 - 값은 나중에 설정
                        AddProperty(name, displayName, type, format, canConnectToPort);
                        
                        // 값 복원은 ReadPropertyValues에서 나중에 수행
                    }
                }
            }
        }
    }

    // Flow In 포트 복원
    private void RestoreFlowInPorts(JsonElement element)
    {
        foreach (var portElement in element.EnumerateArray())
        {
            if (portElement.TryGetProperty("Name", out var nameElement))
            {
                var name = nameElement.GetString();
                if (!string.IsNullOrEmpty(name))
                {
                    AddFlowInPort(name);
                }
            }
        }
    }
    
    // Flow Out 포트 복원
    private void RestoreFlowOutPorts(JsonElement element)
    {
        foreach (var portElement in element.EnumerateArray())
        {
            if (portElement.TryGetProperty("Name", out var nameElement))
            {
                var name = nameElement.GetString();
                if (!string.IsNullOrEmpty(name))
                {
                    AddFlowOutPort(name);
                }
            }
        }
    }
    
    // 입력 포트 복원
    private void RestoreInputPorts(JsonElement element)
    {
        foreach (var portElement in element.EnumerateArray())
        {
            if (portElement.TryGetProperty("Name", out var nameElement) &&
                portElement.TryGetProperty("Type", out var typeElement))
            {
                var name = nameElement.GetString();
                var typeName = typeElement.GetString();

                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(typeName))
                {
                    var type = Type.GetType(typeName);
                    if (type != null)
                    {
                        AddInputPort(name, type);
                    }
                }
            }
        }
    }
    
    // 출력 포트 복원
    private void RestoreOutputPorts(JsonElement element)
    {
        foreach (var portElement in element.EnumerateArray())
        {
            if (portElement.TryGetProperty("Name", out var nameElement) &&
                portElement.TryGetProperty("Type", out var typeElement))
            {
                var name = nameElement.GetString();
                var typeName = typeElement.GetString();

                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(typeName))
                {
                    var type = Type.GetType(typeName);
                    if (type != null)
                    {
                        AddOutputPort(name, type);
                    }
                }
            }
        }
    }

    protected void ReadPropertyValues(JsonElement element, JsonSerializerOptions options) {
        if (!element.TryGetProperty("PropertyValues", out var propertyValuesElement))
            return;
            
        var elementsByName = new Dictionary<string, JsonElement>();
        
        // 모든 속성을 이름 기준으로 딕셔너리에 저장
        foreach (var propElement in propertyValuesElement.EnumerateArray()) {
            if (propElement.TryGetProperty("Name", out var nameElement)) {
                var name = nameElement.GetString();
                if (!string.IsNullOrEmpty(name)) {
                    elementsByName[name] = propElement;
                }
            }
        }
        
        // 로컬 변수로 프로퍼티 복사 - enumerate 중 변경 방지
        var propertiesToProcess = Properties.ToList();
        
        // 속성에 값 복원
        foreach (var property in propertiesToProcess) {
            if (elementsByName.TryGetValue(property.Name, out var propElement) &&
                propElement.TryGetProperty("Value", out var valueElement)) {
                
                try {
                    // 재구성 플래그가 설정되어 있어 ReconfigurePorts 호출 방지됨
                    property.Value = JsonSerializer.Deserialize(
                        valueElement.GetRawText(),
                        property.PropertyType,
                        options);
                }
                catch (Exception ex) {
                    Logger?.LogError(ex, $"{GetType().Name} 노드의 {property.Name} 속성 복원 중 오류");
                }
            }
        }
        
        // 모든 프로퍼티가 복원된 후 한 번만 ReconfigurePorts 호출
        ReconfigurePorts();
    }

    private void InitializeFromAttributes() {
        var type = GetType();
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        
        // 노드 어트리뷰트 처리
        var nameAttr = type.GetCustomAttribute<NodeNameAttribute>();
        if (nameAttr != null) {
            _name = nameAttr.Name;
        }
        else {
            _name = type.Name;
        }

        // 카테고리 어트리뷰트 처리
        var categoryAttr = type.GetCustomAttribute<NodeCategoryAttribute>();
        if (categoryAttr != null) {
            _category = categoryAttr.Category;
        }

        // 설명 어트리뷰트 처리
        var descAttr = type.GetCustomAttribute<NodeDescriptionAttribute>();
        if (descAttr != null) {
            _description = descAttr.Description;
        }

        // 포트 어트리뷰트 처리
        foreach (var prop in props) {
            var inputAttr = prop.GetCustomAttribute<NodeInputAttribute>();
            var outputAttr = prop.GetCustomAttribute<NodeOutputAttribute>();
            var flowInAttr = prop.GetCustomAttribute<NodeFlowInAttribute>();
            var flowOutAttr = prop.GetCustomAttribute<NodeFlowOutAttribute>();
            var propertyAttr = prop.GetCustomAttribute<NodePropertyAttribute>();
            
            if (inputAttr != null) {
                InitializeInputPort(prop, inputAttr);
            }
            else if (outputAttr != null) {
                InitializeOutputPort(prop, outputAttr);
            }
            else if (flowInAttr != null) {
                InitializeFlowInPort(prop, flowInAttr);
            }
            else if (flowOutAttr != null) {
                InitializeFlowOutPort(prop, flowOutAttr);
            }
            else if (propertyAttr != null) {
                InitializeNodeProperty(prop, propertyAttr);
            }
        }
    }
    
    private void InitializeInputPort(PropertyInfo prop, NodeInputAttribute attr)
    {
        // InputPort<T> 타입 확인 및 생성
        var propType = prop.PropertyType;
        if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(InputPort<>))
        {
            var valueType = propType.GetGenericArguments()[0];
            var portIndex = _inputPorts.Count;
            
            // Activator.CreateInstance를 사용하여 포트 생성
            var port = Activator.CreateInstance(
                propType, 
                attr.DisplayName ?? prop.Name,
                this,
                portIndex) as IInputPort;
                
            if (port != null)
            {
                RegisterInputPort(port);
                prop.SetValue(this, port); // 프로퍼티에 설정
            }
        }
    }
    
    private void InitializeOutputPort(PropertyInfo prop, NodeOutputAttribute attr)
    {
        var propType = prop.PropertyType;
        if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(OutputPort<>))
        {
            var valueType = propType.GetGenericArguments()[0];
            var portIndex = _outputPorts.Count;
            
            var port = Activator.CreateInstance(
                propType, 
                attr.DisplayName ?? prop.Name,
                this,
                portIndex) as IOutputPort;
                
            if (port != null)
            {
                RegisterOutputPort(port);
                prop.SetValue(this, port);
            }
        }
    }
    
    private void InitializeFlowInPort(PropertyInfo prop, NodeFlowInAttribute attr)
    {
        if (prop.PropertyType == typeof(FlowInPort) || prop.PropertyType == typeof(IFlowInPort))
        {
            var portIndex = _flowInPorts.Count;
            var port = new FlowInPort(attr.DisplayName ?? prop.Name, this, portIndex);
            
            RegisterFlowInPort(port);
            prop.SetValue(this, port);
        }
    }
    
    private void InitializeFlowOutPort(PropertyInfo prop, NodeFlowOutAttribute attr)
    {
        if (prop.PropertyType == typeof(FlowOutPort) || prop.PropertyType == typeof(IFlowOutPort))
        {
            var portIndex = _flowOutPorts.Count;
            var port = new FlowOutPort(attr.DisplayName ?? prop.Name, this, portIndex);
            
            RegisterFlowOutPort(port);
            prop.SetValue(this, port);
        }
    }
    
    private void InitializeNodeProperty(PropertyInfo prop, NodePropertyAttribute attr)
    {
        var propType = prop.PropertyType;
        if (!propType.IsGenericType || propType.GetGenericTypeDefinition() != typeof(NodeProperty<>))
            return;
        
        var valueType = propType.GetGenericArguments()[0];
            
        // 프로퍼티 생성

        if (Activator.CreateInstance(
                propType,
                prop.Name,
                attr.DisplayName ?? prop.Name,
                this,
                _inputPorts.Count,
                attr.Format,
                attr.CanConnectToPort) is not INodeProperty property)
            return;
            
        _properties.Add(property);
        prop.SetValue(this, property);
                
        // OnValueChanged 처리
        if (!string.IsNullOrEmpty(attr.OnValueChanged))
        {
            var method = GetType().GetMethod(attr.OnValueChanged, 
                                             BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        
            if (method != null)
            {
                property.PropertyChanged += (s, e) => {
                    if (e.PropertyName == nameof(INodeProperty.Value))
                    {
                        method.Invoke(this, []);
                    }
                };
            }
        }
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
            // 노드 구성 (이벤트 핸들러, 포트 설정 등)
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
    protected void ConfigureNode()
    {
        // 빌더 패턴을 사용한 포트 구성
        var builder = new NodeBuilder(this);
        Configure(builder);
    }
    
    /// <summary>
    /// 빌더 패턴을 사용하여 노드를 구성합니다.
    /// 파생 클래스에서 이 메서드를 오버라이드하여 포트와 프로퍼티를 구성할 수 있습니다.
    /// </summary>
    /// <param name="builder">노드 빌더 인스턴스</param>
    protected virtual void Configure(NodeBuilder builder)
    {
        // 기본 구현은 비어있음 - 파생 클래스에서 오버라이드
    }
    
    /// <summary>
    /// 노드의 동적 포트를 재구성합니다.
    /// 속성 변경 등으로 포트 구성이 변경되어야 할 때 호출합니다.
    /// </summary>
    protected void ReconfigurePorts()
    {
        // 이미 재구성 중이면 리턴
        if (_isReconfiguring)
            return;

        try
        {
            _isReconfiguring = true;
            
            // 노드 빌더를 통해 포트와 프로퍼티 구성
            var builder = new NodeBuilder(this);
            Configure(builder);
            
            // 사용되지 않은 포트와 프로퍼티 제거
            CleanupUnusedElements();
            
            _isInitialized = true;
        }
        finally
        {
            _isReconfiguring = false;
        }
    }
    
    /// <summary>
    /// 사용되지 않은 포트와 프로퍼티를 제거합니다.
    /// </summary>
    private void CleanupUnusedElements()
    {
        // 제거할 동적 프로퍼티 목록 생성
        var propsToRemove = _dynamicProperties
            .Where(p => !_usedObjects.Contains(p))
            .ToList();
            
        // 동적 프로퍼티 제거
        foreach (var prop in propsToRemove)
        {
            RemoveProperty(prop);
        }
        
        // 제거할 동적 입력 포트 목록 생성
        var inputPortsToRemove = _dynamicPorts
            .OfType<IInputPort>()
            .Where(p => !_usedObjects.Contains(p))
            .ToList();
            
        // 동적 입력 포트 제거
        foreach (var port in inputPortsToRemove)
        {
            RemoveInputPort(port);
        }
        
        // 제거할 동적 출력 포트 목록 생성
        var outputPortsToRemove = _dynamicPorts
            .OfType<IOutputPort>()
            .Where(p => !_usedObjects.Contains(p))
            .ToList();
            
        // 동적 출력 포트 제거
        foreach (var port in outputPortsToRemove)
        {
            RemoveOutputPort(port);
        }
        
        // 제거할 동적 FlowIn 포트 목록 생성
        var flowInPortsToRemove = _dynamicPorts
            .OfType<FlowInPort>()
            .Where(p => !_usedObjects.Contains(p))
            .ToList();
            
        // 동적 FlowIn 포트 제거
        foreach (var port in flowInPortsToRemove)
        {
            RemoveFlowInPort(port);
        }
        
        // 제거할 동적 FlowOut 포트 목록 생성
        var flowOutPortsToRemove = _dynamicPorts
            .OfType<FlowOutPort>()
            .Where(p => !_usedObjects.Contains(p))
            .ToList();
            
        // 동적 FlowOut 포트 제거
        foreach (var port in flowOutPortsToRemove)
        {
            RemoveFlowOutPort(port);
        }
        
        // 동적 포트 및 프로퍼티 목록 정리
        _dynamicPorts.RemoveAll(p => !_usedObjects.Contains(p));
        _dynamicProperties.RemoveAll(p => !_usedObjects.Contains(p));
    }

    /// <summary>
    /// 포트와 프로퍼티를 관리하는 빌더 클래스입니다.
    /// </summary>
    public class NodeBuilder
    {
        private readonly NodeBase _node;
        private readonly List<IPort> _addedPorts = new();
        
        internal List<IPort> AddedPorts => _addedPorts;
        
        internal NodeBuilder(NodeBase node)
        {
            _node = node;
            
            // 재구성 중인 경우 사용된 객체 추적 초기화
            _node._usedObjects.Clear();
            MarkAttributeBasedElements();
        }
        
        /// <summary>
        /// 어트리뷰트로 정의된 포트와 프로퍼티를 사용된 것으로 표시합니다.
        /// </summary>
        private void MarkAttributeBasedElements()
        {
            var type = _node.GetType();
            
            foreach (var prop in type.GetProperties())
            {
                // 어트리뷰트로 정의된 프로퍼티 표시
                if (prop.GetCustomAttribute<NodePropertyAttribute>() != null)
                {
                    var nodeProperty = _node.Properties.FirstOrDefault(p => p.Name == prop.Name);
                    if (nodeProperty != null)
                    {
                        _node._usedObjects.Add(nodeProperty);
                    }
                }
                
                // 어트리뷰트로 정의된 포트 표시
                var hasPortAttr = prop.GetCustomAttribute<NodeInputAttribute>() != null ||
                                  prop.GetCustomAttribute<NodeOutputAttribute>() != null ||
                                  prop.GetCustomAttribute<NodeFlowInAttribute>() != null ||
                                  prop.GetCustomAttribute<NodeFlowOutAttribute>() != null;
                               
                if (hasPortAttr)
                {
                    var value = prop.GetValue(_node);
                    if (value != null)
                    {
                        _node._usedObjects.Add(value);
                    }
                }
            }
        }
        
        /// <summary>
        /// 제네릭 타입의 입력 포트를 추가합니다.
        /// </summary>
        public InputPort<T> Input<T>(string name)
        {
            // 기존 포트 검색
            var existingPort = _node.InputPorts.OfType<InputPort<T>>().FirstOrDefault(p => p.Name == name);
            
            if (existingPort != null)
            {
                // 기존 포트가 있으면 사용된 것으로 표시하고 반환
                _node._usedObjects.Add(existingPort);
                
                if (!_addedPorts.Contains(existingPort))
                    _addedPorts.Add(existingPort);
                    
                return existingPort;
            }
            
            // 없으면 새로 생성하고 사용된 것으로 표시
            var port = _node.AddInputPort<T>(name);
            _node._usedObjects.Add(port);
            _addedPorts.Add(port);
            return port;
        }
        
        /// <summary>
        /// 특정 타입의 입력 포트를 추가합니다.
        /// </summary>
        public IInputPort Input(string name, Type type)
        {
            // 기존 포트 검색
            var existingPort = _node.InputPorts.FirstOrDefault(p => p.Name == name && p.DataType == type);
            
            if (existingPort != null)
            {
                // 기존 포트가 있으면 사용된 것으로 표시하고 반환
                _node._usedObjects.Add(existingPort);
                
                if (!_addedPorts.Contains(existingPort))
                    _addedPorts.Add(existingPort);
                    
                return existingPort;
            }
            
            // 없으면 새로 생성하고 사용된 것으로 표시
            var port = _node.AddInputPort(name, type);
            _node._usedObjects.Add(port);
            _addedPorts.Add(port);
            return port;
        }
        
        /// <summary>
        /// 제네릭 타입의 출력 포트를 추가합니다.
        /// </summary>
        public OutputPort<T> Output<T>(string name)
        {
            // 기존 포트 검색
            var existingPort = _node.OutputPorts.OfType<OutputPort<T>>().FirstOrDefault(p => p.Name == name);
            
            if (existingPort != null)
            {
                // 기존 포트가 있으면 사용된 것으로 표시하고 반환
                _node._usedObjects.Add(existingPort);
                
                if (!_addedPorts.Contains(existingPort))
                    _addedPorts.Add(existingPort);
                    
                return existingPort;
            }
            
            // 없으면 새로 생성하고 사용된 것으로 표시
            var port = _node.AddOutputPort<T>(name);
            _node._usedObjects.Add(port);
            _addedPorts.Add(port);
            return port;
        }
        
        /// <summary>
        /// 특정 타입의 출력 포트를 추가합니다.
        /// </summary>
        public IOutputPort Output(string name, Type type)
        {
            // 기존 포트 검색
            var existingPort = _node.OutputPorts.FirstOrDefault(p => p.Name == name && p.DataType == type);
            
            if (existingPort != null)
            {
                // 기존 포트가 있으면 사용된 것으로 표시하고 반환
                _node._usedObjects.Add(existingPort);
                
                if (!_addedPorts.Contains(existingPort))
                    _addedPorts.Add(existingPort);
                    
                return existingPort;
            }
            
            // 없으면 새로 생성하고 사용된 것으로 표시
            var port = _node.AddOutputPort(name, type);
            _node._usedObjects.Add(port);
            _addedPorts.Add(port);
            return port;
        }
        
        /// <summary>
        /// Flow 입력 포트를 추가합니다.
        /// </summary>
        public FlowInPort FlowIn(string name)
        {
            // 기존 포트 검색
            var existingPort = _node.FlowInPorts.OfType<FlowInPort>().FirstOrDefault(p => p.Name == name);
            
            if (existingPort != null)
            {
                // 기존 포트가 있으면 사용된 것으로 표시하고 반환
                _node._usedObjects.Add(existingPort);
                
                if (!_addedPorts.Contains(existingPort))
                    _addedPorts.Add(existingPort);
                    
                return existingPort;
            }
            
            // 없으면 새로 생성하고 사용된 것으로 표시
            var port = _node.AddFlowInPort(name);
            _node._usedObjects.Add(port);
            _addedPorts.Add(port);
            return port;
        }
        
        /// <summary>
        /// Flow 출력 포트를 추가합니다.
        /// </summary>
        public FlowOutPort FlowOut(string name)
        {
            // 기존 포트 검색
            var existingPort = _node.FlowOutPorts.OfType<FlowOutPort>().FirstOrDefault(p => p.Name == name);
            
            if (existingPort != null)
            {
                // 기존 포트가 있으면 사용된 것으로 표시하고 반환
                _node._usedObjects.Add(existingPort);
                
                if (!_addedPorts.Contains(existingPort))
                    _addedPorts.Add(existingPort);
                    
                return existingPort;
            }
            
            // 없으면 새로 생성하고 사용된 것으로 표시
            var port = _node.AddFlowOutPort(name);
            _node._usedObjects.Add(port);
            _addedPorts.Add(port);
            return port;
        }
        
        /// <summary>
        /// 프로퍼티를 추가합니다.
        /// </summary>
        public NodeProperty<T> Property<T>(string name, string displayName, 
                                          string? format = null, 
                                          bool canConnectToPort = false,
                                          Action<T>? onValueChanged = null)
        {
            // 기존 프로퍼티 검색
            var existingProp = _node.Properties.OfType<NodeProperty<T>>().FirstOrDefault(p => p.Name == name);
            
            if (existingProp != null)
            {
                // 기존 프로퍼티의 값을 저장
                var existingValue = existingProp.Value;
                var existingCanConnectToPort = existingProp.CanConnectToPort;
                
                // 값 변경 이벤트 핸들러 업데이트
                if (onValueChanged != null)
                {
                    // 기존 이벤트 핸들러 제거
                    existingProp.PropertyChanged -= (s, e) => {
                        if (e.PropertyName == nameof(NodeProperty<T>.Value) && !_node._isReconfiguring)
                            onValueChanged(existingProp.Value);
                    };
                    
                    // 새로운 이벤트 핸들러 추가
                    existingProp.PropertyChanged += (s, e) => {
                        if (e.PropertyName == nameof(NodeProperty<T>.Value) && !_node._isReconfiguring)
                            onValueChanged(existingProp.Value);
                    };
                }
                
                // 기존 값을 복원
                existingProp.Value = existingValue;
                existingProp.CanConnectToPort = canConnectToPort;
                
                // 사용된 것으로 표시
                _node._usedObjects.Add(existingProp);
                return existingProp;
            }
            
            // 새로 생성하고 사용된 것으로 표시
            var prop = _node.AddProperty<T>(name, displayName, format, canConnectToPort);
            _node._usedObjects.Add(prop);
            
            // 값 변경 이벤트 연결 - 재구성 중이 아닐 때만 콜백 실행
            if (onValueChanged != null)
            {
                prop.PropertyChanged += (s, e) => {
                    if (e.PropertyName == nameof(NodeProperty<T>.Value) && !_node._isReconfiguring)
                        onValueChanged(prop.Value);
                };
            }
            
            return prop;
        }
        
        public INodeProperty Property(string name, string displayName, 
                                          Type type, string? format = null, 
                                          bool canConnectToPort = false)
        {
            // 기존 프로퍼티 검색
            var existingProp = _node.Properties.FirstOrDefault(p => p.Name == name && p.PropertyType == type);
            
            if (existingProp != null)
            {
                // 사용된 것으로 표시
                _node._usedObjects.Add(existingProp);
                return existingProp;
            }
            
            // 새로 생성하고 사용된 것으로 표시
            var prop = _node.AddProperty(name, displayName, type, format, canConnectToPort);
            _node._usedObjects.Add(prop);
            return prop;
        }
        
        /// <summary>
        /// 프로퍼티를 가져옵니다.
        /// </summary>
        public NodeProperty<T>? GetProperty<T>(string name)
        {
            return _node.Properties.OfType<NodeProperty<T>>().FirstOrDefault(p => p.Name == name);
        }
        
        /// <summary>
        /// 포트를 가져옵니다.
        /// </summary>
        public T? GetPort<T>(string name) where T : class, IPort
        {
            var allPorts = new List<IPort>();
            allPorts.AddRange(_node.InputPorts);
            allPorts.AddRange(_node.OutputPorts);
            allPorts.AddRange(_node.FlowInPorts);
            allPorts.AddRange(_node.FlowOutPorts);
            
            return allPorts.OfType<T>().FirstOrDefault(p => p.Name == name);
        }
        
        /// <summary>
        /// 노드의 모든 프로퍼티에 접근합니다.
        /// </summary>
        public IEnumerable<INodeProperty> Properties => _node.Properties;
    }

    public InputPort<T> AddInputPort<T>(string name)
    {
        var port = CreateInputPort<T>(name);
        _dynamicPorts.Add(port);
        return port;
    }

    public IInputPort AddInputPort(string name, Type type)
    {
        var port = CreateInputPort(name, type);
        _dynamicPorts.Add(port);
        return port;
    }

    public OutputPort<T> AddOutputPort<T>(string name)
    {
        var port = CreateOutputPort<T>(name);
        _dynamicPorts.Add(port);
        return port;
    }

    public IOutputPort AddOutputPort(string name, Type type)
    {
        var port = CreateOutputPort(name, type);
        _dynamicPorts.Add(port);
        return port;
    }

    public FlowInPort AddFlowInPort(string name)
    {
        var port = CreateFlowInPort(name);
        _dynamicPorts.Add(port);
        return port;
    }

    public FlowOutPort AddFlowOutPort(string name)
    {
        var port = CreateFlowOutPort(name);
        _dynamicPorts.Add(port);
        return port;
    }

    public NodeProperty<T> AddProperty<T>(
        string name,
        string displayName,
        string? format = null,
        bool canConnectToPort = false)
    {
        var property = (NodeProperty<T>)AddProperty(name, displayName, typeof(T), format, canConnectToPort);
        
        // 동적 프로퍼티 목록에 추가
        if (!_dynamicProperties.Contains(property))
        {
            _dynamicProperties.Add(property);
        }
        
        return property;
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
            
            // 이미 존재하는 프로퍼티도 동적 프로퍼티 목록에 추가
            if (!_dynamicProperties.Contains(property))
            {
                _dynamicProperties.Add(property);
            }
            
            return property;
        }

        property = CreateProperty(name, displayName, type, format, canConnectToPort);
        
        // 동적 프로퍼티 목록에 추가
        if (!_dynamicProperties.Contains(property))
        {
            _dynamicProperties.Add(property);
        }
        
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
        
        // FlowOut 포트 제거
        var flowOutPortsToRemove = FlowOutPorts.ToList();
        foreach (var port in flowOutPortsToRemove)
        {
            RemoveFlowOutPort(port);
        }
        
        // FlowIn 포트 제거
        var flowInPortsToRemove = FlowInPorts.ToList();
        foreach (var port in flowInPortsToRemove)
        {
            RemoveFlowInPort(port);
        }
        
        // 동적 포트 및 프로퍼티 목록 초기화
        _dynamicPorts.Clear();
        _dynamicProperties.Clear();
    }
    
    /// <summary>
    /// 동적으로 추가된 포트와 프로퍼티만 제거합니다.
    /// 어트리뷰트로 정의된 포트와 프로퍼티는 유지됩니다.
    /// </summary>
    public void ClearDynamicPorts()
    {
        var type = GetType();
        
        // 제거할 동적 포트 목록 생성
        var dynamicPortsToRemove = _dynamicPorts.ToList();
        
        // 어트리뷰트로 정의된 포트는 유지하도록 필터링
        dynamicPortsToRemove = dynamicPortsToRemove
            .Where(port => 
            {
                // 어트리뷰트가 있는 포트는 제외
                var propertyInfo = type.GetProperties()
                    .FirstOrDefault(p => 
                        (p.GetCustomAttribute<NodeInputAttribute>() != null && 
                         p.GetValue(this) == port) ||
                        (p.GetCustomAttribute<NodeOutputAttribute>() != null && 
                         p.GetValue(this) == port) ||
                        (p.GetCustomAttribute<NodeFlowInAttribute>() != null && 
                         p.GetValue(this) == port) ||
                        (p.GetCustomAttribute<NodeFlowOutAttribute>() != null && 
                         p.GetValue(this) == port));
                
                // propertyInfo가 null이면 어트리뷰트가 없는 포트이므로 제거
                return propertyInfo == null;
            })
            .ToList();
        
        // 동적 포트 제거
        foreach (var port in dynamicPortsToRemove)
        {
            if (port is IInputPort inputPort)
                Remove(inputPort);
            else if (port is IOutputPort outputPort)
                Remove(outputPort);
            else if (port is FlowInPort flowInPort)
                RemoveFlowInPort(flowInPort);
            else if (port is FlowOutPort flowOutPort)
                RemoveFlowOutPort(flowOutPort);
        }
        
        // 제거할 동적 프로퍼티 목록 생성
        var dynamicPropsToRemove = _dynamicProperties
            .Where(prop => 
            {
                // 어트리뷰트가 있는 프로퍼티는 제외
                var propertyInfo = type.GetProperty(prop.Name);
                return propertyInfo?.GetCustomAttribute<NodePropertyAttribute>() == null;
            })
            .ToList();
        
        // 동적 프로퍼티 제거
        foreach (var prop in dynamicPropsToRemove)
        {
            Remove(prop);
        }
        
        // 동적 포트 및 프로퍼티 목록 초기화
        _dynamicPorts.Clear();
        _dynamicProperties.Clear();
    }

    public void Remove(IInputPort port)
    {
        // 메서드 내용 유지
    }

    public void Remove(IOutputPort port)
    {
        // 메서드 내용 유지
    }

    public void Remove(INodeProperty property)
    {
        // 메서드 내용 유지
    }
}
