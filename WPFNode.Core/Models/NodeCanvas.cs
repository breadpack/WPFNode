using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using WPFNode.Core.Commands;
using WPFNode.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using WPFNode.Core.Services;
using WPFNode.Core.Interfaces;
using WPFNode.Core.Models.Serialization;

namespace WPFNode.Core.Models;

public class NodeCanvas : INodeCanvas, INotifyPropertyChanged
{
    private readonly List<NodeBase> _nodes;
    private readonly List<IConnection> _connections;
    private readonly List<NodeGroup> _groups;
    private readonly INodePluginService _pluginService;
    private double _scale = 1.0;
    private double _offsetX;
    private double _offsetY;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    [JsonIgnore]
    public IReadOnlyList<INode> Nodes => _nodes;
    
    [JsonIgnore]
    public IReadOnlyList<IConnection> Connections => _connections;
    
    [JsonIgnore]
    public IReadOnlyList<NodeGroup> Groups => _groups;
    
    public double Scale
    {
        get => _scale;
        set
        {
            if (_scale != value)
            {
                _scale = value;
                OnPropertyChanged();
            }
        }
    }
    
    public double OffsetX
    {
        get => _offsetX;
        set
        {
            if (_offsetX != value)
            {
                _offsetX = value;
                OnPropertyChanged();
            }
        }
    }
    
    public double OffsetY
    {
        get => _offsetY;
        set
        {
            if (_offsetY != value)
            {
                _offsetY = value;
                OnPropertyChanged();
            }
        }
    }
    
    [JsonIgnore]
    public CommandManager CommandManager { get; }

    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = null
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
            throw new ArgumentException($"노드 타입은 NodeBase를 상속해야 합니다: {nodeType.Name}");

        // 생성자에 Canvas를 전달하여 노드 생성
        var node = (NodeBase)Activator.CreateInstance(nodeType, this)!;
        node.X = x;
        node.Y = y;
        node.Initialize();
        
        AddNodeInternal(node);
        return node;
    }

    private void AddNodeInternal(NodeBase node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));

        _nodes.Add(node);
        OnPropertyChanged(nameof(Nodes));
    }

    public void RemoveNode(INode node)
    {
        if (node == null) throw new ArgumentNullException(nameof(node));
        if (node is not NodeBase nodeBase) return;
        if (!_nodes.Contains(nodeBase)) return;

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
        if (source is not IOutputPort outputPort)
            throw new ArgumentException("소스는 출력 포트여야 합니다.", nameof(source));
        if (target is not IInputPort inputPort)
            throw new ArgumentException("타겟은 입력 포트여야 합니다.", nameof(target));
        if (!outputPort.CanConnectTo(inputPort))
            throw new InvalidOperationException("포트를 연결할 수 없습니다.");

        // 중복 연결 체크
        if (_connections.Any(c => c.Source == source && c.Target == target))
            return null;

        var connection = new Connection(outputPort, inputPort);
        ((PortBase)source).AddConnection(connection);
        ((PortBase)target).AddConnection(connection);
        _connections.Add(connection);
        OnPropertyChanged(nameof(Connections));
        return connection;
    }

    public void Disconnect(IConnection connection)
    {
        if (connection == null) throw new ArgumentNullException(nameof(connection));
        if (!_connections.Contains(connection)) return;
        
        // Canvas의 Connections 컬렉션에서 제거
        _connections.Remove(connection);
        
        // 포트에서 연결 제거
        ((PortBase)connection.Source).RemoveConnection(connection);
        ((PortBase)connection.Target).RemoveConnection(connection);
        
        OnPropertyChanged(nameof(Connections));
    }

    private bool IsTypeCompatible(Type sourceType, Type targetType)
    {
        // 동일한 타입이면 호환됨
        if (targetType.IsAssignableFrom(sourceType))
            return true;

        // 숫자 타입 간의 암시적 변환 체크
        if (IsNumericType(sourceType) && IsNumericType(targetType))
        {
            // 암시적 변환이 가능한지 확인
            var method = sourceType.GetMethod("op_Implicit", new[] { sourceType });
            if (method != null && method.ReturnType == targetType)
                return true;

            // 기본 숫자 타입 간의 암시적 변환 규칙 적용
            return IsImplicitNumericConversion(sourceType, targetType);
        }

        return false;
    }

    private bool IsNumericType(Type type)
    {
        if (type == null) return false;

        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Byte:
            case TypeCode.SByte:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.Decimal:
            case TypeCode.Double:
            case TypeCode.Single:
                return true;
            default:
                return false;
        }
    }

    private bool IsImplicitNumericConversion(Type source, Type target)
    {
        // 기본 숫자 타입 간의 암시적 변환 규칙
        var sourceCode = Type.GetTypeCode(source);
        var targetCode = Type.GetTypeCode(target);

        switch (sourceCode)
        {
            case TypeCode.SByte:
                return targetCode == TypeCode.Int16 || targetCode == TypeCode.Int32 || 
                       targetCode == TypeCode.Int64 || targetCode == TypeCode.Single || 
                       targetCode == TypeCode.Double || targetCode == TypeCode.Decimal;
            case TypeCode.Byte:
                return targetCode == TypeCode.Int16 || targetCode == TypeCode.UInt16 || 
                       targetCode == TypeCode.Int32 || targetCode == TypeCode.UInt32 ||
                       targetCode == TypeCode.Int64 || targetCode == TypeCode.UInt64 || 
                       targetCode == TypeCode.Single || targetCode == TypeCode.Double || 
                       targetCode == TypeCode.Decimal;
            case TypeCode.Int16:
                return targetCode == TypeCode.Int32 || targetCode == TypeCode.Int64 || 
                       targetCode == TypeCode.Single || targetCode == TypeCode.Double || 
                       targetCode == TypeCode.Decimal;
            case TypeCode.UInt16:
                return targetCode == TypeCode.Int32 || targetCode == TypeCode.UInt32 ||
                       targetCode == TypeCode.Int64 || targetCode == TypeCode.UInt64 ||
                       targetCode == TypeCode.Single || targetCode == TypeCode.Double ||
                       targetCode == TypeCode.Decimal;
            case TypeCode.Int32:
                return targetCode == TypeCode.Int64 || targetCode == TypeCode.Single ||
                       targetCode == TypeCode.Double || targetCode == TypeCode.Decimal;
            case TypeCode.UInt32:
                return targetCode == TypeCode.Int64 || targetCode == TypeCode.UInt64 ||
                       targetCode == TypeCode.Single || targetCode == TypeCode.Double ||
                       targetCode == TypeCode.Decimal;
            case TypeCode.Int64:
                return targetCode == TypeCode.Single || targetCode == TypeCode.Double ||
                       targetCode == TypeCode.Decimal;
            case TypeCode.UInt64:
                return targetCode == TypeCode.Single || targetCode == TypeCode.Double ||
                       targetCode == TypeCode.Decimal;
            case TypeCode.Single:
                return targetCode == TypeCode.Double;
            default:
                return false;
        }
    }

    public NodeGroup CreateGroup(IEnumerable<NodeBase> nodes, string name = "New Group")
    {
        if (nodes == null) throw new ArgumentNullException(nameof(nodes));
        
        var group = new NodeGroup(Guid.NewGuid().ToString(), name);
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
        if (group == null) throw new ArgumentNullException(nameof(group));
        if (!_groups.Contains(group)) return;
        
        _groups.Remove(group);
    }

    public void AddGroup(NodeGroup group)
    {
        if (group == null) throw new ArgumentNullException(nameof(group));
        if (_groups.Contains(group)) return;
        
        _groups.Add(group);
    }

    public void RemoveGroup(NodeGroup group)
    {
        if (group == null) throw new ArgumentNullException(nameof(group));
        if (!_groups.Contains(group)) return;
        
        _groups.Remove(group);
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
}

// NodeExecutionException을 ExecutionPlan.cs로 이동 
