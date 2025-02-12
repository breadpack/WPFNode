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
        
        if (!actualTarget.DataType.IsAssignableFrom(actualSource.DataType))
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
        var context = new ExecutionContext();
        var executionOrder = GetExecutionOrder();
        
        foreach (var node in executionOrder)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                context.SetNodeState(node, NodeExecutionState.Running);
                await node.ProcessAsync();
                context.SetNodeState(node, NodeExecutionState.Completed);
            }
            catch (Exception ex)
            {
                context.SetNodeState(node, NodeExecutionState.Failed);
                throw new NodeExecutionException($"Node '{node.Name}' execution failed", ex);
            }
        }
    }

    private List<NodeBase> GetExecutionOrder()
    {
        var visited = new HashSet<Guid>();
        var executionOrder = new List<NodeBase>();
        var inProcess = new HashSet<Guid>();

        foreach (var node in _nodes)
        {
            if (!visited.Contains(node.Id))
            {
                VisitNode(node, visited, inProcess, executionOrder);
            }
        }

        return executionOrder;
    }

    private void VisitNode(NodeBase node, HashSet<Guid> visited, HashSet<Guid> inProcess, List<NodeBase> executionOrder)
    {
        if (inProcess.Contains(node.Id))
            throw new InvalidOperationException("Circular dependency detected in node graph");

        if (visited.Contains(node.Id))
            return;

        inProcess.Add(node.Id);

        // Get all nodes that provide input to current node's input ports
        var dependencies = _connections
            .Where(c => c.Target.Node == node)
            .Select(c => c.Source.Node as NodeBase)
            .Where(n => n != null)
            .Distinct();

        foreach (var dependency in dependencies)
        {
            VisitNode(dependency!, visited, inProcess, executionOrder);
        }

        visited.Add(node.Id);
        inProcess.Remove(node.Id);
        executionOrder.Add(node);
    }
}

public class NodeExecutionException : Exception
{
    public NodeExecutionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
} 
