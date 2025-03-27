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
/// IDynamicPortProvider 인터페이스를 구현하여 표준화된 포트 관리 방식을 제공합니다.
/// </summary>
public class DynamicNode : NodeBase
{
    private string              _category;
    private bool                _isInitialized     = false;
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

    /// <summary>
    /// 포트와 프로퍼티를 관리하는 빌더 클래스입니다.
    /// </summary>
    public class NodeBuilder
    {
        private readonly DynamicNode _node;
        private readonly List<IPort> _addedPorts = new();
        
        internal List<IPort> AddedPorts => _addedPorts;
        
        internal NodeBuilder(DynamicNode node)
        {
            _node = node;
            
            // 재구성 중인 경우 사용된 객체 추적 초기화
            if (_node._isReconfiguring)
            {
                _node._usedObjects.Clear();
                MarkAttributeBasedElements();
            }
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
        var port = CreateInputPort<T>(name);
        
        // 동적 포트 목록에 추가
        if (!_dynamicPorts.Contains(port))
        {
            _dynamicPorts.Add(port);
        }
        
        return port;
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
        var port = CreateInputPort(name, type);
        
        // 동적 포트 목록에 추가
        if (!_dynamicPorts.Contains(port))
        {
            _dynamicPorts.Add(port);
        }
        
        return port;
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
        var port = CreateOutputPort<T>(name);
        
        // 동적 포트 목록에 추가
        if (!_dynamicPorts.Contains(port))
        {
            _dynamicPorts.Add(port);
        }
        
        return port;
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
        var port = CreateOutputPort(name, type);
        
        // 동적 포트 목록에 추가
        if (!_dynamicPorts.Contains(port))
        {
            _dynamicPorts.Add(port);
        }
        
        return port;
    }

    public FlowInPort AddFlowInPort(string name)
    {
        // 이미 해당 이름의 Flow In 포트가 있는지 확인
        var existingPort = FlowInPorts.FirstOrDefault(p => p.Name == name);
        if (existingPort is FlowInPort flowInPort)
        {
            return flowInPort;
        }

        // 없으면 새로 생성
        var port = CreateFlowInPort(name);
        
        // 동적 포트 목록에 추가
        if (!_dynamicPorts.Contains(port))
        {
            _dynamicPorts.Add(port);
        }
        
        return port;
    }

    public FlowOutPort AddFlowOutPort(string name)
    {
        // 이미 해당 이름의 Flow Out 포트가 있는지 확인
        var existingPort = FlowOutPorts.FirstOrDefault(p => p.Name == name);
        if (existingPort is FlowOutPort flowOutPort)
        {
            return flowOutPort;
        }

        // 없으면 새로 생성
        var port = CreateFlowOutPort(name);
        
        // 동적 포트 목록에 추가
        if (!_dynamicPorts.Contains(port))
        {
            _dynamicPorts.Add(port);
        }
        
        return port;
    }

    public void RemoveFlowInPort(IFlowInPort port)
    {
        // 동적 포트 목록에서 제거
        _dynamicPorts.Remove(port);
        
        base.RemoveFlowInPort(port);
    }

    public void RemoveFlowOutPort(IFlowOutPort port)
    {
        // 동적 포트 목록에서 제거
        _dynamicPorts.Remove(port);
        
        base.RemoveFlowOutPort(port);
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
    
    public void Remove(IInputPort port)
    {
        // 동적 포트 목록에서 제거
        _dynamicPorts.Remove(port);
        
        RemoveInputPort(port);
    }
    
    public void Remove(IOutputPort port)
    {
        // 동적 포트 목록에서 제거
        _dynamicPorts.Remove(port);
        
        RemoveOutputPort(port);
    }
    
    public void Remove(INodeProperty property)
    {
        // 동적 프로퍼티 목록에서 제거
        _dynamicProperties.Remove(property);
        
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
    
    protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        Models.Execution.FlowExecutionContext? context,
        CancellationToken cancellationToken = default) {
        yield break;
    }

    /// <summary>
    /// 동적 포트 및 프로퍼티 정보를 직렬화합니다.
    /// 파생 클래스에서는 이 메서드를 오버라이드하여 추가 정보를 저장할 수 있습니다.
    /// </summary>
    public override void WriteJson(Utf8JsonWriter writer)
    {
        base.WriteJson(writer);

        var type = GetType();
        
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

    /// <summary>
    /// 동적 포트 및 프로퍼티 정보를 역직렬화합니다.
    /// 파생 클래스에서는 이 메서드를 오버라이드하여 추가 정보를 복원할 수 있습니다.
    /// </summary>
    public override void ReadJson(JsonElement element, JsonSerializerOptions options)
    {
        // 기본 속성 및 어트리뷰트 기반 프로퍼티 먼저 복원
        base.ReadJson(element, options);
        
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
            
            // 역직렬화 후 노드 초기화
            InitializeNode();
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "DynamicNode 포트/프로퍼티 복원 중 오류 발생");
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
                        
                        // 새 프로퍼티 생성
                        var property = AddProperty(name, displayName, type, format, canConnectToPort);

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
}
