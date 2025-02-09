using System.Collections.ObjectModel;
using System.Windows.Media;

namespace WPFNode.Core.Models;

public class PortTemplate
{
    public string Name { get; set; }
    public Type DataType { get; set; }
    public bool IsInput { get; set; }

    public PortTemplate(string name, Type dataType, bool isInput)
    {
        Name = name;
        DataType = dataType;
        IsInput = isInput;
    }
}

public class NodeTemplate
{
    public string Name { get; set; }
    public string Category { get; set; }
    public string Description { get; set; }
    public ObservableCollection<PortTemplate> Ports { get; }
    public Brush? HeaderColor { get; set; }

    public NodeTemplate(string name, string category, string description)
    {
        Name = name;
        Category = category;
        Description = description;
        Ports = new ObservableCollection<PortTemplate>();
        HeaderColor = null;
    }

    public Node CreateNode()
    {
        var node = new Node(Guid.NewGuid().ToString(), Name);
        
        foreach (var portTemplate in Ports)
        {
            var port = new NodePort(
                Guid.NewGuid().ToString(),
                portTemplate.Name,
                portTemplate.DataType,
                portTemplate.IsInput,
                node);

            if (portTemplate.IsInput)
                node.InputPorts.Add(port);
            else
                node.OutputPorts.Add(port);
        }

        return node;
    }
} 