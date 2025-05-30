using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using WPFNode.Exceptions;
using WPFNode.Interfaces;
using WPFNode.Models.Execution;
using WPFNode.Models.Serialization;
using WPFNode.Services;

namespace WPFNode.Models;

public partial class NodeCanvas : INodeCanvas, INotifyPropertyChanged
{
    private readonly List<NodeBase> _nodes;
    private readonly List<IConnection> _connections;
    private readonly List<NodeGroup> _groups;
    private readonly INodeModelService _modelService;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<INode>? NodeAdded;
    public event EventHandler<INode>? NodeRemoved;
    public event EventHandler<IConnection>? ConnectionAdded;
    public event EventHandler<IConnection>? ConnectionRemoved;
    public event EventHandler<NodeGroup>? GroupAdded;
    public event EventHandler<NodeGroup>? GroupRemoved;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected virtual void OnNodeAdded(INode node)
    {
        NodeAdded?.Invoke(this, node);
    }

    protected virtual void OnNodeRemoved(INode node)
    {
        NodeRemoved?.Invoke(this, node);
    }

    protected virtual void OnConnectionAdded(IConnection connection)
    {
        ConnectionAdded?.Invoke(this, connection);
    }

    protected virtual void OnConnectionRemoved(IConnection connection)
    {
        ConnectionRemoved?.Invoke(this, connection);
    }

    protected virtual void OnGroupAdded(NodeGroup group)
    {
        GroupAdded?.Invoke(this, group);
    }

    protected virtual void OnGroupRemoved(NodeGroup group)
    {
        GroupRemoved?.Invoke(this, group);
    }

    [JsonIgnore]
    public IReadOnlyList<INode> Nodes => _nodes;
    
    [JsonIgnore]
    public IReadOnlyList<IConnection> Connections => _connections;
    
    [JsonIgnore]
    public IReadOnlyList<NodeGroup> Groups => _groups;
    
    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null,
        PropertyNameCaseInsensitive = true
    };

    static NodeCanvas()
    {
        DefaultJsonOptions.Converters.Add(new NodeCanvasJsonConverter());
        DefaultJsonOptions.Converters.Add(new TypeJsonConverter());
    }

    [JsonConstructor]
    public NodeCanvas()
    {
        _modelService = NodeServices.ModelService ?? throw new ArgumentNullException("Services.ModelService");
        _nodes = new();
        _connections = new();
        _groups = new();
    }

    public List<NodeBase> SerializableNodes
    {
        get => _nodes;
        set
        {
            _nodes.Clear();
            if (value != null)
            {
                foreach (var node in value)
                {
                    AddNodeInternal(node);
                }
            }
        }
    }
    
    public List<IConnection> SerializableConnections => _connections;
    
    public List<NodeGroup> SerializableGroups => _groups;

    public INode CreateNode(Type nodeType, double x = 0, double y = 0)
    {
        if (!typeof(NodeBase).IsAssignableFrom(nodeType))
            throw new NodeValidationException($"노드 타입은 NodeBase를 상속해야 합니다: {nodeType.Name}");

        // 생성자에 Canvas를 전달하여 노드 생성
        var node = (NodeBase)Activator.CreateInstance(nodeType, this, Guid.NewGuid())!;
        node.X = x;
        node.Y = y;
        
        // 모든 노드에 대해 초기화 메서드 호출
        node.InitializeNode();
        
        AddNodeInternal(node);
        return node;
    }
    
    public T? Q<T>(string id) where T : INode
    {
        return _nodes.OfType<T>().FirstOrDefault(n => n.Id == id);
    }

    // 특정 타입의 노드를 찾아서 반환하는 유틸리티 메서드 추가
    public IEnumerable<T> FindNodesByType<T>() where T : INode
    {
        return _nodes.OfType<T>();
    }

    // 특정 타입의 첫 번째 노드를 찾아서 반환하는 유틸리티 메서드 추가
    public T? Q<T>() where T : INode
    {
        return _nodes.OfType<T>().FirstOrDefault();
    }

    private void AddNodeInternal(NodeBase node)
    {
        if (node == null)
            throw new NodeValidationException("노드가 null입니다.");

        _nodes.Add(node);
        OnPropertyChanged(nameof(Nodes));
        OnNodeAdded(node);
    }

    public void RemoveNode(INode node)
    {
        if (node == null) 
            throw new NodeValidationException("노드가 null입니다.");
        if (node is not NodeBase nodeBase) 
            throw new NodeValidationException("노드가 NodeBase 타입이 아닙니다.");
        if (!_nodes.Contains(nodeBase)) 
            throw new NodeValidationException($"노드가 캔버스에 존재하지 않습니다: {nodeBase.Id}");

        // 노드와 관련된 모든 연결 제거
        var connectionsToRemove = _connections
            .Where(c => node.InputPorts.Contains(c.Target) || 
                       node.OutputPorts.Contains(c.Source))
            .ToList();
        
        foreach (var connection in connectionsToRemove)
        {
            Disconnect(connection);
        }

        _nodes.Remove(nodeBase);
        OnPropertyChanged(nameof(Nodes));
        OnNodeRemoved(node);
    }

    public IConnection Connect(IPort source, IPort target) {
        if(source is IOutputPort outputPort && target is IInputPort inputPort) {
            return Connect(outputPort, inputPort);
        }
        if(source is IFlowOutPort flowOutPort && target is IFlowInPort flowInPort) {
            return Connect(flowOutPort, flowInPort);
        }
        throw new NodeConnectionException("소스와 타겟 포트가 서로 호환되지 않습니다.", source, target);
    }

    public IConnection Connect(IOutputPort source, IInputPort target)
    {
        if (source == null || target == null)
            throw new NodeConnectionException("소스 또는 타겟 포트가 null입니다.");

        if (!source.CanConnectTo(target))
            throw new NodeConnectionException("포트 간 연결이 불가능합니다.", source, target);

        var connection = new Connection(this, source, target);
        source.AddConnection(connection);
        target.AddConnection(connection);
        
        // 캔버스의 연결 목록에 추가
        _connections.Add(connection);
        OnConnectionAdded(connection);
        OnPropertyChanged(nameof(Connections));
        
        return connection;
    }

    public IConnection Connect(IFlowOutPort source, IFlowInPort target) {
        if (source == null || target == null)
            throw new NodeConnectionException("소스 또는 타겟 포트가 null입니다.");

        var connection = new Connection(this, source, target);
        source.AddConnection(connection);
        target.AddConnection(connection);
        
        // 캔버스의 연결 목록에 추가
        _connections.Add(connection);
        OnConnectionAdded(connection);
        OnPropertyChanged(nameof(Connections));
        
        return connection;
    }

    public void Disconnect(IConnection connection)
    {
        if (connection == null) 
            throw new NodeValidationException("연결이 null입니다.");
            
        if (!_connections.Contains(connection)) 
            throw new NodeConnectionException($"연결이 캔버스에 존재하지 않습니다: {connection}");
        
        // Canvas의 Connections 컬렉션에서 제거
        _connections.Remove(connection);
        
        // 포트에서 연결 제거
        connection.Source.RemoveConnection(connection);
        connection.Target.RemoveConnection(connection);
        
        OnPropertyChanged(nameof(Connections));
        OnConnectionRemoved(connection);
    }

    public NodeGroup CreateGroup(IEnumerable<NodeBase> nodes, string name = "New Group")
    {
        if (nodes == null)
            throw new NodeValidationException("노드 목록이 null입니다.");
        
        var group = new NodeGroup(Guid.NewGuid(), name);
        foreach (var node in nodes)
        {
            if (_nodes.Contains(node))
            {
                group.Add(node);
            }
        }
        _groups.Add(group);
        return group;
    }

    public void DeleteGroup(NodeGroup group)
    {
        if (group == null)
            throw new NodeValidationException("그룹이 null입니다.");
        if (!_groups.Contains(group))
            throw new NodeValidationException("존재하지 않는 그룹입니다.");
        
        _groups.Remove(group);
    }

    public void AddGroup(NodeGroup group)
    {
        if (group == null)
            throw new NodeValidationException("그룹이 null입니다.");
        if (_groups.Contains(group))
            throw new NodeValidationException("이미 존재하는 그룹입니다.");
        
        _groups.Add(group);
        OnGroupAdded(group);
    }

    public void RemoveGroup(NodeGroup group)
    {
        if (group == null)
            throw new NodeValidationException("그룹이 null입니다.");
        if (!_groups.Contains(group))
            throw new NodeValidationException("존재하지 않는 그룹입니다.");
        
        _groups.Remove(group);
        OnGroupRemoved(group);
    }

    /// <summary>
    /// Flow 기반 실행 엔진을 사용하여 캔버스의 노드들을 실행합니다.
    /// </summary>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var flowExecutionEngine = new Execution.FlowExecutionEngine();
        await flowExecutionEngine.ExecuteAsync(_nodes, _connections, cancellationToken);
    }

    /// <summary>
    /// 지정된 Flow 기반 실행 엔진을 사용하여 캔버스의 노드들을 실행합니다.
    /// </summary>
    public async Task ExecuteAsync(Execution.FlowExecutionEngine executionEngine, CancellationToken cancellationToken = default)
    {
        await executionEngine.ExecuteAsync(_nodes, _connections, cancellationToken);
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize(this, DefaultJsonOptions);
    }

    public static NodeCanvas FromJson(string json)
    {
        var canvas = JsonSerializer.Deserialize<NodeCanvas>(json, DefaultJsonOptions);
        if (canvas == null)
            throw new JsonException("Failed to deserialize NodeCanvas from JSON");
        return canvas;
    }

    public static NodeCanvas Create()
    {
        return new NodeCanvas();
    }

    /// <summary>
    /// 빌더 패턴을 사용하여 노드를 생성하고 캔버스에 추가한 후 노드를 반환합니다.
    /// </summary>
    /// <typeparam name="T">생성할 노드 타입</typeparam>
    /// <param name="x">노드 X 위치</param>
    /// <param name="y">노드 Y 위치</param>
    /// <returns>생성된 노드</returns>
    public T CreateNode<T>(double x = 0, double y = 0) where T : INode {
        return (T)CreateNode(typeof(T), x, y);
    }

    /// <summary>
    /// 노드를 쉽게 찾고 접근할 수 있는 메서드입니다.
    /// 캔버스에서 첫번째로 발견된 지정 타입의 노드를 반환합니다.
    /// </summary>
    /// <typeparam name="T">찾을 노드 타입</typeparam>
    /// <returns>발견된 노드 또는 null</returns>
    public T? FindNode<T>() where T : INode
    {
        return _nodes.OfType<T>().FirstOrDefault();
    }

    /// <summary>
    /// 캔버스에서 지정된 타입의 모든 노드를 반환합니다.
    /// </summary>
    /// <typeparam name="T">찾을 노드 타입</typeparam>
    /// <returns>발견된 노드 컬렉션</returns>
    public IEnumerable<T> FindNodes<T>() where T : INode
    {
        return _nodes.OfType<T>();
    }

    /// <summary>
    /// 캔버스에 포함된 모든 노드를 재설정합니다.
    /// </summary>
    public void ResetAllNodes()
    {
        foreach (var node in _nodes)
        {
            // 노드에 ResetNode 메서드가 있다면 호출
            if (node.GetType().GetMethod("ResetNode") != null)
            {
                node.GetType().GetMethod("ResetNode")!.Invoke(node, null);
            }
        }
    }

    /// <summary>
    /// 모든 노드와 연결을 제거합니다.
    /// </summary>
    public void Clear()
    {
        // 모든 연결 제거
        foreach (var connection in _connections.ToList())
        {
            Disconnect(connection);
        }

        // 모든 노드 제거
        foreach (var node in _nodes.ToList())
        {
            RemoveNode(node);
        }

        // 모든 그룹 제거
        foreach (var group in _groups.ToList())
        {
            RemoveGroup(group);
        }
    }

    public T CreateNodeWithGuid<T>(Guid guid, double x = 0, double y = 0) where T : INode
    {
        var nodeType = typeof(T);
        var node = (T)CreateNodeWithGuid(guid, nodeType, x, y);
        return node;
    }

    public INode CreateNodeWithGuid(Guid guid, Type nodeType, double x = 0, double y = 0)
    {
        if (!typeof(NodeBase).IsAssignableFrom(nodeType))
            throw new NodeValidationException($"노드 타입은 NodeBase를 상속해야 합니다: {nodeType.Name}");

        // 생성자에 Canvas와 Id를 전달하여 노드 생성
        var node = (NodeBase)Activator.CreateInstance(nodeType, this, guid)!;
        node.X = x;
        node.Y = y;
        
        // 모든 노드에 대해 초기화 메서드 호출
        node.InitializeNode();
        
        AddNodeInternal(node);
        return node;
    }

    public IConnection ConnectWithId(Guid id, IPort source, IPort target) {
        if(source is IOutputPort outputPort && target is IInputPort inputPort) {
            return ConnectWithId(id, outputPort, inputPort);
        }
        if(source is IFlowOutPort flowOutPort && target is IFlowInPort flowInPort) {
            return ConnectWithId(id, flowOutPort, flowInPort);
        }
        throw new NodeConnectionException("소스와 타겟 포트가 서로 호환되지 않습니다.", source, target);
    }

    public IConnection ConnectWithId(Guid id, IOutputPort source, IInputPort target) {
        if (!source.CanConnectTo(target))
            throw new NodeConnectionException("포트를 연결할 수 없습니다.", source, target);

        return _ConnectionWithId(id, source, target);
    }

    public IConnection ConnectWithId(Guid id, IFlowOutPort source, IFlowInPort target) {
        return _ConnectionWithId(id, source, target);
    }

    private IConnection _ConnectionWithId(Guid id, IPort source, IPort target) {
        if (source.Node == null || target.Node == null)
            throw new NodeConnectionException("포트는 노드에 연결되어 있어야 합니다.", source, target);

        // 중복 연결 체크
        var connection = FindConnection(source.Id, target.Id);
        if (connection != null) {
            return connection;
        }

        connection = new Connection(id, this, source, target);
        source.AddConnection(connection);
        target.AddConnection(connection);
        _connections.Add(connection);
        OnConnectionAdded(connection);
        OnPropertyChanged(nameof(Connections));
        return connection;
    }

    public NodeGroup CreateGroupWithId(Guid id, IEnumerable<NodeBase> nodes, string name = "New Group")
    {
        if (nodes == null) throw new ArgumentNullException(nameof(nodes));
        
        var group = new NodeGroup(id, name);
        foreach (var node in nodes)
        {
            if (_nodes.Contains(node))
            {
                group.Add(node);
            }
        }
        _groups.Add(group);
        return group;
    }

    // 포트 ID로 연결을 찾는 유틸리티 메서드
    private IConnection? FindConnection(PortId sourcePortId, PortId targetPortId)
    {
        return _connections.FirstOrDefault(c => 
            c.SourcePortId == sourcePortId && 
            c.TargetPortId == targetPortId);
    }

    public SubCanvasNode CreateDynamicNode(
        string name,
        string category,
        string description,
        double x = 0,
        double y = 0)
    {
        var node = new SubCanvasNode(this, Guid.NewGuid(), name, category, description);
        node.X = x;
        node.Y = y;
        SerializableNodes.Add(node);
        return node;
    }

    public async Task SaveToFileAsync(string filePath)
    {
        var json = JsonSerializer.Serialize(this, DefaultJsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task LoadFromFileAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath);
        var canvas = JsonSerializer.Deserialize<NodeCanvas>(json, DefaultJsonOptions);
        if (canvas == null) throw new Exception("Failed to deserialize canvas");

        // 현재 캔버스의 상태를 로드된 상태로 업데이트
        _nodes.Clear();
        _connections.Clear();
        _groups.Clear();

        foreach (var node in canvas.SerializableNodes)
        {
            AddNodeInternal(node);
        }

        foreach (var connection in canvas.SerializableConnections)
        {
            _connections.Add(connection);
            OnConnectionAdded(connection);
        }

        foreach (var group in canvas.SerializableGroups)
        {
            _groups.Add(group);
            OnGroupAdded(group);
        }
    }

    private INode CreateNode(Type nodeType)
    {
        if (nodeType == null)
            throw new ArgumentNullException(nameof(nodeType));

        try
        {
            var node = _modelService.CreateNode(nodeType);
            return node;
        }
        catch (Exception ex)
        {
            throw new NodeCreationException($"노드 생성 실패: {nodeType.Name}", nodeType, ex);
        }
    }
}

// NodeExecutionException을 ExecutionPlan.cs로 이동
