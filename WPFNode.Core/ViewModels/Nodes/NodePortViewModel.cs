using WPFNode.Core.Models;
using WPFNode.Core.ViewModels.Base;

namespace WPFNode.Core.ViewModels.Nodes;

public class NodePortViewModel : ViewModelBase
{
    private readonly NodePort _port;
    private string _name;

    public NodePortViewModel(NodePort port)
    {
        _port = port;
        _name = port.Name;

        // Model 속성 변경 감지
        _port.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(NodePort.Name))
            {
                Name = _port.Name;
            }
        };
    }

    public string Id => _port.Id;
    
    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                _port.Name = value;
            }
        }
    }
    
    public bool IsInput => _port.IsInput;
    public Type DataType => _port.DataType;
    public Node Parent => _port.Parent;
    public IEnumerable<Connection> Connections => _port.Connections;

    public NodePort Model => _port;
} 