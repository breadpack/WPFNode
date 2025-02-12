using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using WPFNode.Core.Commands;
using WPFNode.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace WPFNode.Core.Models;

public class NodeCanvas
{
    private readonly List<NodeBase> _nodes;
    private readonly List<IConnection> _connections;
    private readonly List<NodeGroup> _groups;

    [JsonIgnore]
    public IReadOnlyList<NodeBase> Nodes => _nodes;
    
    [JsonIgnore]
    public IReadOnlyList<IConnection> Connections => _connections;
    
    [JsonIgnore]
    public IReadOnlyList<NodeGroup> Groups => _groups;
    
    [JsonPropertyName("scale")]
    public double Scale { get; set; } = 1.0;
    
    [JsonPropertyName("offsetX")]
    public double OffsetX { get; set; }
    
    [JsonPropertyName("offsetY")]
    public double OffsetY { get; set; }
    
    [JsonIgnore]
    public CommandManager CommandManager { get; }

    [JsonConstructor]
    public NodeCanvas()
    {
        _nodes = new();
        _connections = new();
        _groups = new();
        CommandManager = new CommandManager();
    }

    [JsonPropertyName("nodes")]
    public List<NodeBase> SerializableNodes => _nodes;
    
    [JsonPropertyName("connections")]
    public List<IConnection> SerializableConnections => _connections;
    
    [JsonPropertyName("groups")]
    public List<NodeGroup> SerializableGroups => _groups;

    public void AddNode(NodeBase node)
    {
        if (node == null) throw new ArgumentNullException(nameof(node));
        if (_nodes.Contains(node)) return;
        
        _nodes.Add(node);
    }

    public void RemoveNode(NodeBase node)
    {
        if (node == null) throw new ArgumentNullException(nameof(node));
        if (!_nodes.Contains(node)) return;

        // 노드와 관련된 모든 연결 제거
        var connectionsToRemove = _connections
            .Where(c => node.InputPorts.Contains(c.Target) || 
                       node.OutputPorts.Contains(c.Source))
            .ToList();
        
        foreach (var connection in connectionsToRemove)
        {
            Disconnect(connection);
        }

        _nodes.Remove(node);
    }

    public Connection? Connect(IPort source, IPort target)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (target == null) throw new ArgumentNullException(nameof(target));
        if (source.IsInput == target.IsInput) return null;
        
        IPort actualSource = source.IsInput ? target : source;
        IPort actualTarget = source.IsInput ? source : target;
        
        // 타입 호환성 검사 (암시적 변환 고려)
        if (!IsTypeCompatible(actualSource.DataType, actualTarget.DataType))
            return null;

        // 이미 연결이 존재하는지 확인
        if (_connections.Any(c => c.Source == actualSource && c.Target == actualTarget))
            return null;

        var connection = new Connection(actualSource, actualTarget);
        
        // 포트에 연결 추가
        ((PortBase)actualSource).AddConnection(connection);
        ((PortBase)actualTarget).AddConnection(connection);
        
        // Canvas의 Connections 컬렉션에 추가
        _connections.Add(connection);
        
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
}

// NodeExecutionException을 ExecutionPlan.cs로 이동 
