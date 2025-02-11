using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using WPFNode.Core.Commands;
using WPFNode.Plugin.SDK;
using WPFNode.Abstractions;

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
} 
