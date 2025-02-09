using WPFNode.Core.Models;
using WPFNode.Core.ViewModels.Base;

namespace WPFNode.Core.ViewModels.Nodes;

public class ConnectionViewModel : ViewModelBase
{
    private readonly Connection _connection;
    private bool _isSelected;

    public ConnectionViewModel(Connection connection)
    {
        _connection = connection;
    }

    public string Id => _connection.Id;
    public NodePortViewModel Source => new NodePortViewModel(_connection.Source);
    public NodePortViewModel Target => new NodePortViewModel(_connection.Target);

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public bool IsValid => _connection.IsValid;

    public Connection Model => _connection;
} 