using System.Collections.ObjectModel;
using WPFNode.Core.Commands;
using WPFNode.Plugin.SDK;
using WPFNode.Abstractions;

namespace WPFNode.Core.Models;

public class NodeCanvas
{
    public ObservableCollection<NodeBase>    Nodes          { get; }
    public ObservableCollection<IConnection> Connections    { get; }
    public ObservableCollection<NodeGroup>   Groups         { get; }
    public double                            Scale          { get; set; } = 1.0;
    public double                            OffsetX        { get; set; }
    public double                            OffsetY        { get; set; }
    public CommandManager                    CommandManager { get; }

    public NodeCanvas()
    {
        Nodes          = new ObservableCollection<NodeBase>();
        Connections    = new ObservableCollection<IConnection>();
        Groups         = new ObservableCollection<NodeGroup>();
        CommandManager = new CommandManager();
    }

    public Connection? Connect(IPort source, IPort target)
    {
        if (source.IsInput == target.IsInput) return null;
        
        IPort actualSource = source.IsInput ? target : source;
        IPort actualTarget = source.IsInput ? source : target;
        
        if (!actualTarget.DataType.IsAssignableFrom(actualSource.DataType))
            return null;

        var connection = new Connection(actualSource, actualTarget);
        
        // 포트에 연결 추가
        ((PortBase)actualSource).AddConnection(connection);
        ((PortBase)actualTarget).AddConnection(connection);
        
        // Canvas의 Connections 컬렉션에 추가
        Connections.Add(connection);
        
        return connection;
    }

    public void Disconnect(IConnection connection)
    {
        // Canvas의 Connections 컬렉션에서 제거
        Connections.Remove(connection);
        
        // 포트에서 연결 제거
        ((PortBase)connection.Source).RemoveConnection(connection);
        ((PortBase)connection.Target).RemoveConnection(connection);
    }

    public NodeGroup CreateGroup(IEnumerable<NodeBase> nodes, string name = "New Group")
    {
        var group = new NodeGroup(Guid.NewGuid().ToString(), name);
        foreach (var node in nodes)
        {
            group.Nodes.Add(node);
        }
        Groups.Add(group);
        return group;
    }

    public void DeleteGroup(NodeGroup group)
    {
        Groups.Remove(group);
    }
} 
