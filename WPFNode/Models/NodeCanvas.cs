using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using WPFNode.Commands;
using WPFNode.Exceptions;
using WPFNode.Interfaces;
using WPFNode.Models.Serialization;
using WPFNode.Resources;
using WPFNode.Services;

namespace WPFNode.Models;

public class NodeCanvas : INodeCanvas, INotifyPropertyChanged
{
    private readonly List<NodeBase> _nodes;
    private readonly List<IConnection> _connections;
    private readonly List<NodeGroup> _groups;
    private readonly INodePluginService _pluginService;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<INode>? NodeCreated;
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

    protected virtual void OnNodeCreated(INode node)
    {
        NodeCreated?.Invoke(this, node);
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
    
    [JsonIgnore]
    public CommandManager CommandManager { get; }

    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null,
        PropertyNameCaseInsensitive = true
    };

    static NodeCanvas()
    {
        DefaultJsonOptions.Converters.Add(new NodeCanvasJsonConverter());
    }

    [JsonConstructor]
    public NodeCanvas()
    {
        _pluginService = NodeServices.PluginService ?? throw new ArgumentNullException("Services.PluginService");
        _nodes = new();
        _connections = new();
        _groups = new();
        CommandManager = new CommandManager();
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

    public T CreateNode<T>(double x = 0, double y = 0) where T : INode
    {
        var nodeType = typeof(T);
        var node = (T)CreateNode(nodeType, x, y);
        return node;
    }

    public INode CreateNode(Type nodeType, double x = 0, double y = 0)
    {
        if (!typeof(NodeBase).IsAssignableFrom(nodeType))
            throw new NodeValidationException(
                string.Format(ExceptionMessages.GetMessage(ExceptionMessages.NodeMustInheritNodeBase), nodeType.Name));

        // 생성자에 Canvas를 전달하여 노드 생성
        var node = (NodeBase)Activator.CreateInstance(nodeType, this, Guid.NewGuid())!;
        node.X = x;
        node.Y = y;
        node.Initialize();
        
        AddNodeInternal(node);
        return node;
    }

    private void AddNodeInternal(NodeBase node)
    {
        if (node == null)
            throw new NodeValidationException(
                ExceptionMessages.GetMessage(ExceptionMessages.NodeIsNull));

        _nodes.Add(node);
        OnPropertyChanged(nameof(Nodes));
        OnNodeCreated(node);
    }

    public void RemoveNode(INode node)
    {
        if (node == null) 
            throw new NodeValidationException(
                ExceptionMessages.GetMessage(ExceptionMessages.NodeIsNull));
        if (node is not NodeBase nodeBase) 
            throw new NodeValidationException(
                ExceptionMessages.GetMessage(ExceptionMessages.NodeMustInheritNodeBase));
        if (!_nodes.Contains(nodeBase)) 
            throw new NodeValidationException(
                ExceptionMessages.GetMessage(ExceptionMessages.NodeNotFound));

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
    }

    public IConnection Connect(IPort source, IPort target)
    {
        // 기본 유효성 검사
        if (source == null || target == null)
            throw new NodeConnectionException(
                ExceptionMessages.GetMessage(ExceptionMessages.NodeIsNull));

        // 포트 타입 검사
        if (source is not IOutputPort outputPort)
            throw new NodeConnectionException(
                ExceptionMessages.GetMessage(ExceptionMessages.SourceMustBeOutputPort),
                source, target);
        if (target is not IInputPort inputPort)
            throw new NodeConnectionException(
                ExceptionMessages.GetMessage(ExceptionMessages.TargetMustBeInputPort),
                source, target);
        if (source.Node == null || target.Node == null)
            throw new NodeConnectionException(
                ExceptionMessages.GetMessage(ExceptionMessages.PortsMustBeAttachedToNode),
                source, target);

        // 포트 연결 가능 여부 검사
        if (!outputPort.CanConnectTo(inputPort))
        {
            throw new NodeConnectionException(
                ExceptionMessages.GetMessage(ExceptionMessages.PortsCannotBeConnected),
                source, target);
        }

        var sourcePortId = new PortId(
            source.Node.Id,
            false,
            source.GetPortIndex());
            
        var targetPortId = new PortId(
            target.Node.Id,
            true,
            target.GetPortIndex());

        // 중복 연결 체크
        if (_connections.Any(c => c.SourcePortId == sourcePortId && c.TargetPortId == targetPortId))
        {
            throw new NodeConnectionException(
                ExceptionMessages.GetMessage(ExceptionMessages.PortsAlreadyConnected),
                source, target);
        }

        var connection = new Connection(outputPort, inputPort);
        source.AddConnection(connection);
        target.AddConnection(connection);
        _connections.Add(connection);
        OnPropertyChanged(nameof(Connections));
        return connection;
    }

    public void Disconnect(IConnection connection)
    {
        if (connection == null) 
            throw new NodeValidationException(
                ExceptionMessages.GetMessage(ExceptionMessages.ConnectionNotFound));
            
        if (!_connections.Contains(connection)) 
            throw new NodeConnectionException(
                ExceptionMessages.GetMessage(ExceptionMessages.ConnectionNotFound), 
                connection.Id.ToString());
        
        // Canvas의 Connections 컬렉션에서 제거
        _connections.Remove(connection);
        
        // 포트에서 연결 제거
        connection.Source.RemoveConnection(connection);
        connection.Target.RemoveConnection(connection);
        
        OnPropertyChanged(nameof(Connections));
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
                group.Nodes.Add(node);
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

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var executionPlan = ExecutionPlan.Create(_nodes, _connections);
        await executionPlan.ExecuteAsync(cancellationToken);
    }

    internal Connection CreateConnection(IOutputPort source, IInputPort target)
    {
        return new Connection(source, target);
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

    public T CreateNodeWithId<T>(Guid id, double x = 0, double y = 0) where T : INode
    {
        var nodeType = typeof(T);
        var node = (T)CreateNodeWithId(id, nodeType, x, y);
        return node;
    }

    public INode CreateNodeWithId(Guid id, Type nodeType, double x = 0, double y = 0)
    {
        if (!typeof(NodeBase).IsAssignableFrom(nodeType))
            throw new NodeValidationException($"노드 타입은 NodeBase를 상속해야 합니다: {nodeType.Name}");

        // 생성자에 Canvas와 Id를 전달하여 노드 생성
        var node = (NodeBase)Activator.CreateInstance(nodeType, this, id)!;
        node.X = x;
        node.Y = y;
        node.Initialize();
        
        AddNodeInternal(node);
        return node;
    }

    public IConnection ConnectWithId(Guid id, IPort source, IPort target)
    {
        if (source is not IOutputPort outputPort)
            throw new NodeConnectionException("소스는 출력 포트여야 합니다.", source, target);
        if (target is not IInputPort inputPort)
            throw new NodeConnectionException("타겟은 입력 포트여야 합니다.", source, target);
        if (!outputPort.CanConnectTo(inputPort))
            throw new NodeConnectionException("포트를 연결할 수 없습니다.", source, target);
        if (source.Node == null || target.Node == null)
            throw new NodeConnectionException("포트는 노드에 연결되어 있어야 합니다.", source, target);

        var sourcePortId = new PortId(
            source.Node.Id,
            false,
            source.Node.OutputPorts.ToList().IndexOf(outputPort));
            
        var targetPortId = new PortId(
            target.Node.Id,
            true,
            target.Node.InputPorts.ToList().IndexOf(inputPort));

        // 중복 연결 체크
        if (_connections.Any(c => c.SourcePortId == sourcePortId && c.TargetPortId == targetPortId))
        {
            throw new NodeConnectionException("이미 연결되어 있는 포트입니다.", source, target);
        }

        var connection = new Connection(id, outputPort, inputPort);
        source.AddConnection(connection);
        target.AddConnection(connection);
        _connections.Add(connection);
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
                group.Nodes.Add(node);
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

    public DynamicNode CreateDynamicNode(
        string name,
        string category,
        string description,
        double x = 0,
        double y = 0)
    {
        var node = new DynamicNode(this, Guid.NewGuid(), name, category, description);
        node.X = x;
        node.Y = y;
        SerializableNodes.Add(node);
        return node;
    }
}

// NodeExecutionException을 ExecutionPlan.cs로 이동 
