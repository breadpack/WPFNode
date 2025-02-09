using System.Collections.ObjectModel;
using WPFNode.Core.Commands;

namespace WPFNode.Core.Models;

public class NodeCanvas
{
    public ObservableCollection<Node> Nodes { get; }
    public ObservableCollection<Connection> Connections { get; }
    public ObservableCollection<NodeGroup> Groups { get; }
    public double Scale { get; set; } = 1.0;
    public double OffsetX { get; set; }
    public double OffsetY { get; set; }
    public CommandManager CommandManager { get; }

    public NodeCanvas()
    {
        Nodes = new ObservableCollection<Node>();
        Connections = new ObservableCollection<Connection>();
        Groups = new ObservableCollection<NodeGroup>();
        CommandManager = new CommandManager();
    }

    public Connection? Connect(NodePort source, NodePort target)
    {
        try
        {
            var connection = new Connection(Guid.NewGuid().ToString(), source, target);
            Connections.Add(connection);
            return connection;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    public void Disconnect(Connection connection)
    {
        connection.Disconnect();
        Connections.Remove(connection);
    }

    public NodeGroup CreateGroup(IEnumerable<Node> nodes, string name = "New Group")
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
