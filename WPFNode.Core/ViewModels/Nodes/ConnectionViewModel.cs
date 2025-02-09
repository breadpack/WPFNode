using WPFNode.Core.ViewModels.Base;
using WPFNode.Abstractions;

namespace WPFNode.Core.ViewModels.Nodes;

public class ConnectionViewModel : ViewModelBase
{
    private readonly Connection _model;
    private bool _isSelected;

    public ConnectionViewModel(Connection model)
    {
        _model = model;
        Source = new NodePortViewModel(model.Source);
        Target = new NodePortViewModel(model.Target);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public NodePortViewModel Source { get; }
    public NodePortViewModel Target { get; }
    public Connection Model => _model;
} 